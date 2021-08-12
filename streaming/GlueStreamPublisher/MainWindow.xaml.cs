using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using DOT.AGM.Core.Server;
using DOT.AGM.Extensions;
using DOT.AGM.Server;
using DOT.AGM.Transport;
using DOT.Core.Extensions;
using Tick42;

namespace GlueStreamPublisher
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml.
    ///     This demonstrates the creation of a Glue42 stream with branches and publishing images and data.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        ///     Used for Glue42 streaming branches
        /// </summary>
        private readonly List<string> _availableSymbols = new List<string> {"EURUSD", "GOLD", "GOOG", "MSFT"};

        private readonly Dictionary<string, Timer> timer_ = new Dictionary<string, Timer>();

        private Glue42 _glue;
        private IServerEventStream stream_;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            UpdateUI(false);

            // create a timer for each of the symbol (branches)
            // each timer will push to the relevant branch
            foreach (var symbol in _availableSymbols)
            {
                timer_.Add(symbol, new Timer(OnTimerTick, symbol, 0, 1000));
            }
        }

        internal void RegisterGlue(Glue42 glue)
        {
            _glue = glue;
            UpdateUI(true);

            _glue.Interop.ConnectionStatusChanged += InteropConnectionStatusChanged;

            // register a streaming endpoint (Glue stream) called GlueDemoTickStream
            stream_ = _glue.Interop.RegisterStreamingEndpoint(smb => smb.SetMethodName("GlueDemoTickStream")
                    .SetParameterSignature("bool reject, string symbol"),
                new ServerEventStreamHandler(false)
                {
                    // in each of the stream handler events we have the optional cookie parameter
                    // that is optionally supplied while registering the stream

                    // this lambda is invoked when a new subscription request is received
                    SubscriptionRequestHandler = SubscriptionRequestHandler,

                    // this lambda is invoked when new subscriber has been connected to a branch
                    SubscriberHandler = SubscriberHandler,

                    //this lambda is invoked when a subscriber cancels its subscription or has been kicked (banned) by the publisher
                    UnsubscribeHandler = UnsubscribeHandler
                });
        }

        private void InteropConnectionStatusChanged(object sender, InteropStatusEventArgs e)
        {
            LogMessage($"Glue is now {e.Status.State}. StatusMessage: {e.Status.StatusMessage}");

            var isGlueConnected = e.Status.State == TransportState.Connected ? true : false;
            UpdateUI(isGlueConnected);
        }

        private IEventStreamBranch SubscriptionRequestHandler(IServerEventStream stream,
            IEventStreamSubscriptionRequest request, object cookie)
        {
            // validate the request
            // for demo purposes we have a reject argument sent by the subscriber
            bool shouldReject = request.SubscriptionContext.Arguments.GetValueByName("reject", v => v.AsBool);
            string symbol = request.SubscriptionContext.Arguments.GetValueByName("symbol", v => v.AsString());

            LogMessage(
                $"Subscription request from {request.Caller} for symbol {symbol} - {request.SubscriptionContext.Arguments.AsString()}");

            if (shouldReject || string.IsNullOrEmpty(symbol))
            {
                string rejectionReason = shouldReject ? "rejected as requested" : "rejected because symbol is empty";
                string rejectionMsg = $"Subscription request from {request.Caller} {rejectionReason}";
                LogMessage(rejectionMsg);

                // if we cannot validate the subscription request we can reject it
                request.Reject(smrb =>
                    smrb.SetMessage(rejectionMsg)
                        .SetContext(cb =>
                            cb.AddValue("RejectionArgument", new[] {5, 3, 2, 1, 2})
                                .AddValue("MoreRejectionArguments", "sdfgsdfg")).Build());

                return null;
            }

            // when we validate the subscription request, we can assign the subscriber to a branch
            // in this demo we will use the tickMode as a branch (https://docs.glue42.com/glue42-concepts/data-sharing-between-apps/interop/net/index.html#streaming)

            // branches are an optional grouping of multiple subscribers to ease data category segregation - the branches are keyed by
            // 'any' object supplied by the publisher

            // if we don't need a branching mechanism, we then use the 'main' branch by *not* supplying value for branch key.
            return request.Accept(smrb => smrb.Build(), branchKey: symbol);
        }

        private void SubscriberHandler(IServerEventStream stream, IEventStreamSubscriber subscriber,
            IEventStreamBranch branch, object cookie)
        {
            IEventStreamSubscriptionRequest request = subscriber.Subscription;
            // log the subscriber
            LogMessage($"New subscriber {request.Caller} {request.SubscriptionContext.Arguments.AsString()}");

            // push an 'image' to that subscriber which is received *only* by it (last image pattern)

            // this will be received as an OOB data - see the demo subscriber code
            subscriber.Push(cb =>
                cb.AddValue("SubscribersAsImage", _glue.AGMObjectSerializer.Serialize(stream.GetBranches()
                    .SelectMany(b =>
                        b.GetSubscribers().Select(sb => sb.Subscription.Caller)))));

            // e.g. keep the subscriptions in a list
            var subscriptionItem =
                $"{request.Caller.ApplicationName} {request.SubscriptionContext.Arguments.AsString()}";
            DispatchAction(() =>
            {
                ListViewSubscriptions.Items.Add(subscriptionItem);
                ListViewSubscriptions.ScrollIntoView(subscriptionItem);
            });
        }

        private void UnsubscribeHandler(
            IServerEventStream stream,
            IEventStreamSubscriber subscriber,
            EventStreamSubscriberRemovedContext context,
            IEventStreamBranch branch,
            object cookie)
        {
            IEventStreamSubscriptionRequest request = subscriber.Subscription;

            // log the subscription cancellation
            LogMessage($"Removed subscriber {request.Caller} {request.SubscriptionContext.Arguments.AsString()}");

            // remove subscriptions when cancelled
            var subscriptionItem =
                $"{request.Caller.ApplicationName} {request.SubscriptionContext.Arguments.AsString()}";
            DispatchAction(() => ListViewSubscriptions.Items.Remove(subscriptionItem));
        }

        private void DispatchAction(Action action)
        {
            Dispatcher?.BeginInvoke(action);
        }

        private void LogMessage(string message)
        {
            DispatchAction(() => ListViewLogs.Items.Add(message));
        }

        private void OnTimerTick(object state)
        {
            // in here the state plays the role of 'updated branch'
            if (!(state is string symbol) || stream_ == null ||
                !stream_.TryGetBranch(out IEventStreamBranch branch, symbol))
            {
                return;
            }

            // branch.GetSubscribers() - a snapshot of the current subscribers to that branch

            // push data to that branch only
            branch.Push(cb => cb.AddValue("Symbol", symbol).AddValue("Time", DateTime.UtcNow.ToString()));
        }

        private void UpdateUI(bool isConnected)
        {
            var statusMessage = isConnected ? "Connected" : "Disconnected";
            var statusColor = isConnected ? Colors.LightGreen : Colors.LightPink;

            ConnectionStatusDescription = statusMessage;
            ConnectionStatusColor = new SolidColorBrush(statusColor);

            ListViewSubscriptions.IsEnabled = isConnected;
            ListViewLogs.IsEnabled = isConnected;
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