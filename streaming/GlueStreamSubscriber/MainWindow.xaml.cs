using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DOT.AGM;
using DOT.AGM.Client;
using DOT.AGM.Core.Client;
using DOT.AGM.Transport;
using DOT.Core.Extensions;
using Tick42;

namespace GlueStreamSubscriber
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    ///     This demonstrates the subscription to a Glue42 stream.
    /// </summary>
    public partial class MainWindow : Window
    {
        private IEventStream currentTicksStream_;

        private Glue42 glue_;
        private IMethod ticksEndpoint_;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            UpdateUI(false);

            AvailableSymbols = new List<string> {"EURUSD", "GOLD", "GOOG", "MSFT"};
        }

        public List<string> AvailableSymbols { get; set; }

        internal void RegisterGlue(Glue42 glue)
        {
            glue_ = glue;
            UpdateUI(true);

            glue_.Interop.ConnectionStatusChanged += InteropConnectionStatusChanged;
            glue_.Interop.EndpointStatusChanged += InteropEndpointStatusChanged;
        }

        private void InteropEndpointStatusChanged(object sender, InteropEndpointStatusChangedEventArgs e)
        {
            // is this an instance of the demo stream?
            var endpoint = e.InteropEndpoint;

            // filter only streaming endpoints called GlueDemoTickStream
            if (endpoint.Definition.Name != "GlueDemoTickStream" ||
                !endpoint.Definition.Flags.HasFlag(MethodFlags.SupportsStreaming) || !endpoint.IsValid)
            {
                return;
            }

            ticksEndpoint_ = endpoint;
            var symbol = ComboBoxSymbols.SelectedItem.ToString();
            SubscribeStream(symbol);
        }

        private void SubscribeStream(string symbol)
        {
            //if there is already subscribed stream - unsubscribe it and subscribe for new symbol
            currentTicksStream_?.Close();

            glue_.Interop.Subscribe(ticksEndpoint_,
                    new ClientEventStreamHandler
                    {
                        // this lambda is invoked when the status of the stream has changed
                        EventStreamStatusChanged = (info, status, cookie) =>
                            LogMessage($"{info.EventStreamingMethod.Name} to {info.Server} is {status}"),

                        // this lambda is invoked when there is data published to the stream
                        EventHandler = (info, data, cookie) =>
                            DispatchAction(() =>
                            {
                                var isOOB = data.IsCallbackStream;
                                // if isOOB is true the data has been pushed only to this subscriber

                                var subscriptionItem = $"{data.ResultContext.AsString()}";
                                ListViewSubscriptions.Items.Add(subscriptionItem);
                                ListViewSubscriptions.ScrollIntoView(subscriptionItem);
                            })
                    },
                    // these are the arguments sent in the subscription request
                    mib => mib.SetContext(cb => cb.AddValue("reject", false).AddValue("symbol", symbol)),
                    // additional settings - specify target await timeout
                    new TargetSettings().WithTargetAwaitTimeout(TimeSpan.FromSeconds(5)),
                    // stream settings, specifying that we accept 'personal' (out-of-band) stream pushes
                    new ClientEventStreamSettings {AllowCallbacks = true, ReestablishStream = false}
                )
                .ContinueWith(eventStream =>
                {
                    currentTicksStream_ =
                        eventStream.Status == TaskStatus.RanToCompletion ? eventStream.Result : null;
                });
        }

        private void InteropConnectionStatusChanged(object sender, InteropStatusEventArgs e)
        {
            LogMessage($"Glue is now {e.Status.State}. StatusMessage: {e.Status.StatusMessage}");

            var isGlueConnected = e.Status.State == TransportState.Connected ? true : false;
            UpdateUI(isGlueConnected);
        }

        private void SymbolsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (glue_ != null)
            {
                var selectedSymbol = ((ComboBox) sender).SelectedItem.ToString();
                SubscribeStream(selectedSymbol);
            }
        }

        private void DispatchAction(Action action)
        {
            Dispatcher?.BeginInvoke(action);
        }

        private void LogMessage(string message)
        {
            DispatchAction(() => ListViewLogs.Items.Add(message));
        }

        private void UpdateUI(bool isConnected)
        {
            var statusMessage = isConnected ? "Connected" : "Disconnected";
            var statusColor = isConnected ? Colors.LightGreen : Colors.LightPink;

            ConnectionStatusDescription = statusMessage;
            ConnectionStatusColor = new SolidColorBrush(statusColor);

            ListViewSubscriptions.IsEnabled = isConnected;
            ListViewLogs.IsEnabled = isConnected;
            ComboBoxSymbols.IsEnabled = isConnected;
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
            get => (Brush) GetValue(ConnectionStatusColorProperty);
            set => SetValue(ConnectionStatusColorProperty, value);
        }

        #endregion
    }
}