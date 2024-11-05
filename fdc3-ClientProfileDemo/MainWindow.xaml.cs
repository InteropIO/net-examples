using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DOT.AGM.GwTransport;
using DOT.AGM.Transport;
using FDC3ChannelsClientProfileDemo.AGM;
using FDC3ChannelsClientProfileDemo.POCO;
using IOConnect.FDC3.API;
using IOConnect.FDC3.API.Context;
using IOConnect.FDC3.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tick42;
using Tick42.AppManager;
using Tick42.Channels;
using Tick42.Contexts;
using Tick42.Entities;
using Tick42.Windows;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using FontFamily = System.Windows.Media.FontFamily;
using Image = System.Windows.Controls.Image;

[assembly: DisableDpiAwareness]

namespace FDC3ChannelsClientProfileDemo
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    partial class MainWindow : Window, IGlueChannelEventHandler<T42Contact>, IT42CRMService
    {
        public class SyncFeature
        {
            public string Name { get; set; }
            public bool Listen { get; set; } = true;
            public bool Send { get; set; } = true;
        }

        private readonly SyncFeature syncOnSyncContact_ = new SyncFeature { Name = "SyncContact", Send = false };
        private readonly SyncFeature syncOnChannel_ = new SyncFeature { Name = "Channel" };
        private readonly SyncFeature syncOnFdc3_ = new SyncFeature { Name = "FDC3" };

        public List<SyncFeature> SyncFeatures { get; }

        // portfolio of the selected client (if any) - shown in AppMode.Legacy and AppMode.Sticky
        private readonly PortfolioCollection ClientPortfolio;

        // list of clients shown in all app modes
        private readonly ClientsCollection Clients;

        private string currentClientId_;
        private DesktopAgent da_;

        // theme switching support - the demo supports a dark and light themes
        private bool darkThemeOn_;
        private Glue42 glue_;
        private IGlueWindow glueWindow_;
        private bool isFirstShown_;
        private AppState startupState_;

        // in AppMode.Glue, whenever the client selection changes, the app would make an
        // interop call to broadcast the selection change so that the Portfolio app can
        // show the portfolio of the selected client; we suspend this during restore
        private bool suspendSyncClient_;

        // Lifetime and initialization

        public MainWindow()
        {
            InitializeComponent();

            Clients = (ClientsCollection)FindResource("Clients");
            ClientPortfolio = (PortfolioCollection)FindResource("ClientPortfolio");
            DataContext = this;
            TextBoxFilter.TextChanged += TextBoxFilter_TextChanged;

            ((App)Application.Current).InitializeGlue();

            Visibility = App.StickyWindowsEnabled ? Visibility.Hidden : Visibility.Visible;

            SyncFeatures = new List<SyncFeature> { syncOnSyncContact_, syncOnChannel_, syncOnFdc3_ };
        }

        public string SelectedClientName
        {
            get => (ClientsListView.SelectedItem as ClientData)?.Name;
            set
            {
                ClientsListView.SelectedItem = Clients.FirstOrDefault(cd => cd.Name == value);
                if (ClientsListView.SelectedItem != null)
                {
                    ClientsListView.ScrollIntoView(ClientsListView.SelectedItem);
                }
            }
        }

        private IntPtr HWnd => new WindowInteropHelper(this).Handle;

        public IT42CRMService CRMService { get; private set; }

        void IGlueChannelEventHandler<T42Contact>.HandleUpdate(IGlueChannelContext channelContext, ChannelUpdateInfo updateInfo, T42Contact data)
        {
            if (data == null)
            {
                return;
            }

            if (!syncOnChannel_.Listen)
            {
                return;
            }

            UpdateSelectedContact(data);
        }

        void IGlueChannelEventHandler.HandleChannelChanged(IGlueChannelContext channelContext, IGlueChannel newChannel,
            IGlueChannel prevChannel)
        {
            if (!syncOnChannel_.Listen)
            {
                return;
            }
            if (newChannel != null && ExtractT42Contact(channelContext) is T42Contact contact)
            {
                UpdateSelectedContact(contact);
            }
        }

        void IGlueChannelEventHandler.HandleUpdate(IGlueChannelContext channelContext, IGlueChannel channel,
            ContextUpdatedEventArgs updateArgs)
        {
            if (!syncOnChannel_.Listen)
            {
                return;
            }
            if (ExtractT42Contact(channelContext) is T42Contact contact)
            {
                UpdateSelectedContact(contact);
            }
        }

        public void Dispose()
        {
        }

        void IT42CRMService.SyncContact(T42Contact contact, IServiceOptions options)
        {
            if (!syncOnSyncContact_.Listen)
            {
                return;
            }
            if (contact != null)
            {
                Dispatcher.Invoke(() => UpdateSelectedContact(contact));
            }
        }

        private void UpdateSelectedContact(T42Contact data)
        {
            if (data?.Name != null)
            {
                SelectedClientName = data.Name.FirstName + " " + data.Name.LastName;
            }
        }

        private T42Contact ExtractT42Contact(IGlueChannelContext channelContext)
        {
            T42Contact result = null;
            try
            {
                result = channelContext.GetValue<T42Contact>("contact");
            }
            catch (Exception e)
            {
                MessageBox.Show("Failed to extract contact from channel context: " + e.Message, "Error");
            }

            return result;
        }

        private async Task SetT42ContactInChannel(IGlueChannelContext channelContext, T42Contact contact)
        {
            if (channelContext is null || contact is null)
            {
                return;
            }

            object contactObject = contact;

            await glueWindow_.ChannelContext.SetValue(contactObject, "contact").ConfigureAwait(true);
        }

        private void SetIcon()
        {
            try
            {
                var uri = new Uri($"pack://application:,,,/Resources/wpf{(int)App.Mode}.jpg");
                Stream iconStream = Application.GetResourceStream(uri).Stream;
                Icon = new Bitmap(iconStream).ToImageSource();
            }
            catch
            {
            }
        }

        private void CheckLaunchPortfolioApp()
        {
            if (!isFirstShown_ && !string.IsNullOrWhiteSpace(SelectedClientName))
            {
                isFirstShown_ = true;
                if (Utils.IsGlueEnabled(App.Mode) && Utils.ShouldLaunchPortfolioApp())
                {
                    LaunchPortfolioApp();
                }
            }
        }

        public AppState GetState()
        {
            return new AppState
            {
                SelectedClient = SelectedClientName,
                DarkThemeOn = darkThemeOn_
            };
        }

        public void RestoreState(AppState state)
        {
            startupState_ = state;
            string selectedClient = state.SelectedClient;

            if (selectedClient != null)
            {
                suspendSyncClient_ = true;
                try
                {
                    SelectedClientName = selectedClient;
                }
                finally
                {
                    suspendSyncClient_ = false;
                }
            }

            ToggleDarkTheme(state.DarkThemeOn);
            CheckLaunchPortfolioApp();
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            Task.Run(RefreshClients);

            TextBoxFilter.Text = "Type to filter";
            TextBoxFilter.GotFocus += (_1, _2) =>
            {
                if (TextBoxFilter.Text == "Type to filter")
                {
                    TextBoxFilter.Text = "";
                    TextBoxFilter.CaretBrush = Brushes.White;
                    TextBoxFilter.CaretIndex = 0;
                }
            };
            TextBoxFilter.LostFocus += (_1, _2) =>
            {
                if (string.IsNullOrWhiteSpace(TextBoxFilter.Text))
                    TextBoxFilter.Text = "Type to filter";
            };

            if (App.PortfolioHidden)
            {
                ClientPortfolioListView.Visibility = Visibility.Hidden;
                ClientPortfolioButton.Visibility = Visibility.Visible;
                Width = 550;
                UIGrid.ColumnDefinitions.Last().Width = new GridLength(0);
                TitleLabel.Content = "Client List";
                Title = "Client List";
                if (App.UseAlternateLogo)
                {
                    LogoStackPanel.Children.Clear();
                    var NetStackPanel = new StackPanel { Orientation = Orientation.Vertical };
                    NetStackPanel.Children.Add(
                        new TextBlock
                        {
                            Text = ".NET",
                            Foreground = Brushes.LightGray,
                            FontSize = 19,
                            Margin = new Thickness(6, -2, 0, 0),
                            FontWeight = FontWeight.FromOpenTypeWeight(400),
                        }
                    );
                    NetStackPanel.Children.Add(
                        new TextBlock
                        {
                            Text = "Framework",
                            Foreground = Brushes.LightGray,
                            Margin = new Thickness(10, -2, 0, 0),
                            FontSize = 9,
                        }
                    );
                    LogoStackPanel.Children.Insert(0,
                        new Image
                        {
                            Source = new BitmapImage(new Uri("pack://application:,,,/Resources/dot-net-framework.png")),
                            Height = 32
                        }
                    );
                    //LogoStackPanel.Children.Add( new TextBlock() { Width = 10 } );
                    LogoStackPanel.Children.Add(NetStackPanel);
                }
            }
            else
            {
                ClientPortfolioButton.Visibility = Visibility.Hidden;
                LogoStackPanel.Children.Clear();
                LogoStackPanel.Children.Add(
                    new TextBlock
                    {
                        Text = "WPF " + App.Mode,
                        FontSize = 16,
                        Foreground = Brushes.White,
                        Margin = new Thickness(10, 10, 0, 10),
                        FontFamily = new FontFamily(new Uri("pack://application:,,,/"), "./Resources/#michroma")
                    });

                ConnectivityIndicatorStackPanel.Visibility = Visibility.Hidden;
            }

            ClientsGridViewWidth.ColumnDefinitions.Last().Width = new GridLength(16);
            ClientsGridView.Columns.Last().CellTemplate = new DataTemplate();

            SetIcon();

            if (!App.StickyWindowsEnabled)
            {
                WindowStyle = WindowStyle.ThreeDBorderWindow;
            }

            var grippers = new[] { ClientPortfolioListView, ClientsListView }
                .SelectMany(lv => lv.FindChildren("PART_HeaderGripper").OfType<Thumb>()).ToList();
            foreach (var gripper in grippers)
            {
                gripper.Visibility = Visibility.Collapsed;
            }
        }

        private async Task RefreshClients()
        {
            while (true)
            {
                try
                {
                    var request = (HttpWebRequest)WebRequest.Create(App.ClientsUrl);
                    request.Method = "GET";
                    request.KeepAlive = true;

                    var gwSerializer = new GwProtocolSerializer(ValueGwConverter.Settings.None);

                    var response = (HttpWebResponse)request.GetResponse();

                    using (var sr = new StreamReader(response.GetResponseStream() ??
                                                     throw new InvalidOperationException()))
                    {
                        string responseText = await sr.ReadToEndAsync();
                        JArray joResponse = JArray.Parse(responseText);
                        var clientDataArray = joResponse.Select((dynamic jsonObject) =>
                        {
                            var portfolioItems =
                                JsonConvert.DeserializeObject<List<PortfolioData>>(
                                    (string)(jsonObject["context"]["portfolio"] + ""));
                            double portfolioValue = portfolioItems.Select(pd => pd.Shares * pd.Price).Sum();
                            string message = jsonObject + "";

                            return new ClientData
                            {
                                Name = jsonObject["name"]["firstName"].Value + " " +
                                       jsonObject["name"]["lastName"].Value,
                                Portfolio = portfolioItems,
                                PortfolioValue = portfolioValue,
                                Contact = gwSerializer.DeserializeMessage<T42Contact>(message)
                            };
                        }).ToArray();

                        Application.Current?.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,
                            new Action(delegate
                            {
                                try
                                {
                                    Clients.Clear();
                                    foreach (var clientData in clientDataArray)
                                    {
                                        Clients.Add(clientData);
                                    }

                                    if (startupState_ != null)
                                    {
                                        RestoreState(startupState_);
                                    }
                                }
                                catch
                                {
                                    Task.Run(RefreshClients);
                                }
                            }));
                    }

                    break;
                }
                catch
                {
                    await Task.Delay(1000).ConfigureAwait(false);
                }
            }
        }

        internal async Task RegisterGlue(Glue42 glue)
        {
            glue_ = glue;

            CRMService =
                    glue_.Interop.CreateServiceProxy<IT42CRMService>(optionsAdapter: new CRMServiceOptionsAdapter(),
                        preventSelfCalls: true);

            glue.Interop.CreateServiceOperationSubscription(
                CRMService,
                crm => crm.SyncContact(null, null),
                (sender, method, status) =>
                {
                    if (!status)
                    {
                        return;
                    }

                    Dispatcher.BeginInvoke((Action)(() =>
                    {
                        var contact = (ClientsListView.SelectedItem as ClientData)?.Contact;
                        if (contact != null)
                        {
                            if (syncOnSyncContact_.Send)
                            {
                                CRMService.SyncContact(contact, options: new CRMServiceOptions(method.OriginalServer));
                            }
                        }
                    }));
                });
            
            glue.Interop.RegisterService<IT42CRMService>(this);

            if (App.Mode == AppMode.FDC3)
            {
                da_ = new DesktopAgent(glue, new DesktopAgent.Configuration
                {
                    HearOwnBroadcasts = true
                });
            }

            UsernameLabel.Content = "Username: " + App.GlueUsername;
            URLLabel.Content = "URL: " + glue.GatewayUri;

            glue.Interop.TargetStatusChanged += Interop_OnTargetStatusChanged;

            ServersLabel.Text = "Servers: " +
                                string.Join(", ",
                                    glue.Interop.Servers.Select(serverInfo => serverInfo.ApplicationName));

            var appState = glue.GetRestoreState<AppState>() ?? new AppState
            {
                DarkThemeOn = App.DefaultToDarkTheme
            };

            RestoreState(appState);

            glue.Interop.ConnectionStatusChanged += Interop_OnConnectionStatusChanged;
            SetConnectedState(glue.Interop.ConnectionStatus.State == TransportState.Connected);

            if (App.StickyWindowsEnabled)
            {
                var defaultBounds = new GlueWindowBounds
                {
                    X = (int)((SystemParameters.PrimaryScreenWidth / 2) - (Width / 2)),
                    Y = (int)((SystemParameters.PrimaryScreenHeight / 2) - (Height / 2)),
                    Width = (int)Width,
                    Height = (int)Height
                };
                var gwOptions = glue.GetStartupWindowOptions(Title, defaultBounds)
                    .WithChannelSupport(App.Mode >= AppMode.Channels);

                glueWindow_ = await glue.GlueWindows.RegisterWindow(this, gwOptions);
                _ = da_?.TrackWindowChannelSwitches(glueWindow_);
                _ = da_?.AddContextListener<Contact>(null, (contact, metadata) =>
                {
                    if (!syncOnFdc3_.Listen)
                    {
                        return;
                    }
                    var restId = contact.ID.FdsId;

                    if (Clients.FirstOrDefault(d =>
                            restId == d.Contact.Ids.FirstOrDefault(id => id.SystemName == "rest.id")?.NativeId) is
                        ClientData cl)
                    {
                        Dispatcher.Invoke(() =>
                            SelectedClientName = cl.Contact.Name.FirstName + " " + cl.Contact.Name.LastName);
                    }
                });

                glueWindow_.ChannelContext.Subscribe(this, "contact");
            }
            else
            {
                Visibility = Visibility.Visible;
            }
        }

        private void Interop_OnConnectionStatusChanged(object sender, InteropStatusEventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke((Action)(() => Interop_OnConnectionStatusChanged(sender, e)));
                return;
            }

            SetConnectedState(e.Status.State == TransportState.Connected);
        }

        private void Interop_OnTargetStatusChanged(object sender, InteropTargetStatusChangedEventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke((Action)(() => Interop_OnTargetStatusChanged(sender, e)));
                return;
            }

            ServersLabel.Text =
                "Servers: " + string.Join(", ", e.Servers.Select(serverInfo => serverInfo.ApplicationName));
        }
        // UI

        private void ToggleDarkTheme(bool? value = null)
        {
            darkThemeOn_ = value ?? !darkThemeOn_;
            if (darkThemeOn_)
            {
                ClientPortfolioListView.Background = Brushes.Black;
                ClientsListView.Background = Brushes.Black;
                ClientPortfolioListView.Foreground = Brushes.WhiteSmoke;
                ClientsListView.Foreground = Brushes.WhiteSmoke;
            }
            else
            {
                ClientPortfolioListView.Background = Brushes.WhiteSmoke;
                ClientsListView.Background = Brushes.WhiteSmoke;
                ClientPortfolioListView.Foreground = Brushes.Black;
                ClientsListView.Foreground = Brushes.Black;
            }
        }

        private async void ClientsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selection = e.AddedItems.OfType<ClientData>().FirstOrDefault();

            ClientPortfolio.Clear();
            if (selection == null)
            {
                return;
            }

            if (ClientsListView.SelectedItem is ClientData data)
            {
                T42Contact contact = data.Contact;

                if (syncOnSyncContact_.Send)
                {
                    // notify interop legacy apps
                    CRMService.SyncContact(contact);
                }

                if (App.Mode == AppMode.FDC3)
                {
                    if (syncOnFdc3_.Send)
                    {
                        await da_.Broadcast(new Contact(new ContactID
                        {
                            Email = contact.Emails.FirstOrDefault(),
                            FdsId = contact.Ids.FirstOrDefault(id => id.SystemName == "rest.id")?.NativeId ??
                                    contact.DisplayName,
                        }, data.Name));
                    }
                }
                
                if (syncOnChannel_.Send)
                {
                    var currentChannel = glueWindow_?.ChannelContext.GetCurrentChannel() ?? null;
                    if (currentChannel != null)
                    {
                        var restId = ExtractT42Contact(glueWindow_.ChannelContext)?.Ids?
                            .FirstOrDefault(id => id.SystemName == "rest.id")?.NativeId;

                        if (restId != contact.Ids.FirstOrDefault(id => id.SystemName == "rest.id")?.NativeId)
                        {
                            try
                            {
                                await SetT42ContactInChannel(glueWindow_.ChannelContext, contact)
                                    .ConfigureAwait(true);
                            }
                            catch (Exception exception)
                            {
                                MessageBox.Show(
                                    $"Failed to set contact in channel {currentChannel.Name}: " + exception.Message,
                                    "Error");
                            }
                        }
                    }
                }
            }

            if (App.PortfolioHidden)
            {
                currentClientId_ = selection.Contact?.Ids?.Where(id => id.SystemName == "rest.id")
                    .Select(id => id.NativeId).FirstOrDefault();
                if (!suspendSyncClient_)
                {
                    //TODO: channel update?
                }
            }
            else
            {
                foreach (var instrument in selection.Portfolio)
                {
                    ClientPortfolio.Add(instrument);
                }
            }
        }

        private void LinkButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("https://www.interop.io");
            }
            catch
            {
            }
        }

        private void TextBoxFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                ((CollectionViewSource)FindResource("clientsCvs")).View.Refresh();
            }));
        }

        private void CollectionViewSource_Filter(object sender, FilterEventArgs e)
        {
            e.Accepted =
                TextBoxFilter.Text == "Type to filter" ||
                string.IsNullOrWhiteSpace(TextBoxFilter.Text) ||
                ((ClientData)e.Item).Name.ToLower().Contains(TextBoxFilter.Text.ToLower());
        }

        // Glue

        private void ClientPortfolioButton_Click(object sender, RoutedEventArgs e)
        {
            LaunchPortfolioApp();
        }

        private async void LaunchPortfolioApp()
        {
            try
            {
                var amcContext = AppManagerContext.CreateNew();
                amcContext.Set("clientId", currentClientId_);
                amcContext.Set("sync", true);

                var amcOptions = AppManagerContext.CreateNew();
                amcOptions.Set("relativeTo", glueWindow_.Id);
                amcOptions.Set("relativeDirection", RelativeDirection.Right.ToString().ToLower());
                amcOptions.Set("channelId", glueWindow_.ChannelContext.GetCurrentChannel()?.Name ?? "");

                var amc = AppManagerContext.CreateNew();
                amc.Set("Context", amcContext);
                amc.Set("Options", amcOptions);

                var portfolioApp = await glue_.AppManager.AwaitApplication(a => a.Name == App.PortfolioAppName);
                var instance = await portfolioApp.Start(amc);
            }
            catch // swallow all
            {
            }
        }

        private void SetConnectedState(bool state)
        {
            if (state)
            {
                ConnectivityCircleLabel.Foreground = new SolidColorBrush(Color.FromRgb(0x81, 0xae, 0x5c));
                ConnectivityCircleLabel1.Foreground = new SolidColorBrush(Color.FromRgb(0x81, 0xae, 0x5c));
                ConnectivityTextLabel.Content = "Connected";
                ConnectivityTextLabel1.Content = "Connected";
            }
            else
            {
                ConnectivityCircleLabel.Foreground = new SolidColorBrush(Color.FromRgb(0xae, 0x75, 0x5c));
                ConnectivityCircleLabel1.Foreground = new SolidColorBrush(Color.FromRgb(0xae, 0x75, 0x5c));
                ConnectivityTextLabel.Content = "Disconnected";
                ConnectivityTextLabel1.Content = "Disconnected";
            }
        }

        private void ConnectivityTextLabel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (ConnectivityPopup.Visibility == Visibility.Visible)
            {
                ConnectivityPopup.Visibility = Visibility.Hidden;
                Panel.SetZIndex(ConnectivityPopup, -100);
            }
            else
            {
                ConnectivityPopup.Visibility = Visibility.Visible;
                Panel.SetZIndex(ConnectivityPopup, 100);
            }
        }

        private void XButton_Click(object sender, RoutedEventArgs e)
        {
            ConnectivityPopup.Visibility = Visibility.Hidden;
            Panel.SetZIndex(ConnectivityPopup, -100);
        }
    }
}