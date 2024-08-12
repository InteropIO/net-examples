using System;
using System.Windows;
using System.Windows.Media;
using DOT.Core.Extensions;
using Tick42;
using Tick42.Notifications;
using Tick42.Windows;
using Notification = Tick42.Notifications.Notification;

namespace WPFApp
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotificationHandler
    {
        private Glue42 glue_;

        public MainWindow()
        {
            InitializeComponent();

            DataContext = this;
            Visibility = Visibility.Hidden;
            UpdateUI(false);
        }

        public void AcceptNotification(string customerId, double customerPrice)
        {
            //accept handler with notification object state
            MessageBox.Show(customerId + " price " + customerPrice, "Accepted");
        }

        public void RejectNotification(string customerId, double customerPrice)
        {
            //reject handler with notification object state
            MessageBox.Show(customerId + " price " + customerPrice, "Rejected");
        }

        public void NotificationRoutingDetail(IServiceOptions options = null)
        {
            var ic = options.InvocationContext.InvocationContext;
            // receiving notification object state and other service details in ic.Arguments
            Console.WriteLine(ic.Arguments.AsString());
        }

        public void Dispose()
        {
        }

        internal void RegisterGlue(Glue42 glue)
        {
            glue_ = glue;
            UpdateUI(true);

            //bounds are optional. With them we will just set initial placement of the application
            var defaultBounds = new GlueWindowBounds
            {
                X = (int)((SystemParameters.PrimaryScreenWidth / 2) - (Width / 2)),
                Y = (int)((SystemParameters.PrimaryScreenHeight / 2) - (Height / 2)),
                Width = (int)Width,
                Height = (int)Height
            };
            var gwOptions = glue.GetStartupWindowOptions("Notification Publisher", defaultBounds);
            gwOptions.WithType(GlueWindowType.Tab);

            //register the window
            glue.GlueWindows?.RegisterWindow(this, gwOptions);

            //register notification service
            glue.Interop.RegisterService<INotificationHandler>(this);
        }

        private async void OnSendNotificationClick(object sender, RoutedEventArgs e)
        {
            if (glue_ == null)
            {
                //glue is not initialized
                return;
            }

            await glue_.Notifications.RaiseNotification(new Notification
            {
                Title = Title.Text,
                Severity = Enum.TryParse<Severity>(Severity.Text, true, out var severity)
                    ? severity
                    : Tick42.Notifications.Severity.Low,
                Type = NotificationType.Notification,
                Category = "category",
                Source = "source",
                Description = Description.Text,
                GlueRoutingDetailCallback = new NotificationCallback
                {
                    Name = nameof(INotificationHandler.NotificationRoutingDetail),
                    SetTarget = c => c.WithTargetMatching(s => s.InstanceId, glue_.Identity.InstanceId),
                    Parameters = new object[] { new { customerId = "41234", notification = "$(this)" } },
                },
                Actions = new[]
                {
                    new NotificationActionSettings
                    {
                        Name = nameof(INotificationHandler.AcceptNotification),
                        DisplayName = "Accept",
                        Description = "Accept",
                        Parameters = new object[] { new { customerId = "41234", customerPrice = 3.14 } },
                        SetTarget = c => c.WithTargetMatching(s => s.InstanceId, glue_.Identity.InstanceId)
                    },
                    new NotificationActionSettings
                    {
                        Name = nameof(INotificationHandler.RejectNotification),
                        DisplayName = "Reject",
                        Description = "Reject",
                        Parameters = new object[] { new { customerId = "41234", customerPrice = 3.14 } },
                        SetTarget = c => c.WithTargetMatching(s => s.InstanceId, glue_.Identity.InstanceId)
                    }
                },
            });
        }

        private void UpdateUI(bool isConnected)
        {
            var statusMessage = isConnected ? "Connected" : "Disconnected";
            var statusColor = isConnected ? Colors.LightGreen : Colors.LightPink;

            ConnectionStatusDescription = statusMessage;
            ConnectionStatusColor = new SolidColorBrush(statusColor);

            Title.IsEnabled = isConnected;
            Description.IsEnabled = isConnected;
            Severity.IsEnabled = isConnected;
            ButtonNotify.IsEnabled = isConnected;
        }

        #region Dependency Properties

        public static readonly DependencyProperty ConnectionStatusDescriptionProperty =
            DependencyProperty.Register("ConnectionStatusDescription", typeof(string), typeof(MainWindow));

        public static readonly DependencyProperty ConnectionStatusColorProperty =
            DependencyProperty.Register("ConnectionStatusColor", typeof(Brush), typeof(MainWindow));

        public string ConnectionStatusDescription
        {
            get => GetValue(ConnectionStatusDescriptionProperty).ToString();
            set => SetValue(ConnectionStatusDescriptionProperty, value);
        }

        public Brush ConnectionStatusColor
        {
            get => (Brush)GetValue(ConnectionStatusColorProperty);
            set => SetValue(ConnectionStatusColorProperty, value);
        }

        #endregion
    }
}