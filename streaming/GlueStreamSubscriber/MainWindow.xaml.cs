using System;
using System.Windows;
using DOT.AGM;
using DOT.AGM.Client;
using DOT.AGM.Core.Client;
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
        public enum TickMode
        {
            None,
            Slow,
            Normal,
            Fast
        }

        private readonly Glue42 glue_;
        private readonly Random random_ = new Random();

        public MainWindow()
        {
            InitializeComponent();

            // initialize Glue42
            glue_ = new Glue42();

            // give subscriber an application name
            // explicitly turn off Glue Window management, Glue contexts, Glue AppManager, Glue metrics and Glue notifications
            // as they're not used in this example
            glue_.Initialize("GlueStreamsSubscriber", useAppManager: false, useStickyWindows: false,
                useContexts: false, useMetrics: false, useNotifications: false);

            // detect connection status change and log it to the UI
            glue_.Interop.ConnectionStatusChanged += (sender, args) => LogMessage($"Glue is now {args.Status}");

            //glue_.Interop.Subscribe("GlueDemoTickStream",
            //    new ClientEventStreamHandler
            //    {
            //        // this lambda is invoked when the status of the stream has changed
            //        EventStreamStatusChanged = (info, status, cookie) => LogMessage(
            //            $"{info.EventStreamingMethod.Name} to {info.Server} is {status}"),

            //        // this lambda is invoked when there is data published to the stream
            //        EventHandler = (info, data, cookie) =>
            //            DispatchAction(() =>
            //            {
            //                var isOOB = data.IsCallbackStream;
            //                // if isOOB is true the data has been pushed only to this subscriber
            //                Subscriptions.Items.Add($"{data.ResultContext.AsString()}");
            //            })
            //    },
            //    // these are the arguments sent in the subscription request
            //    mib => mib.SetContext(cb => cb.AddValue("reject", false).AddValue("tickMode", (int)TickMode.Normal)),
            //    // additional settings - specify target await timeout
            //    new TargetSettings().WithTargetAwaitTimeout(TimeSpan.FromSeconds(5)).WithTargetType(MethodTargetType.All),
            //    // stream settings, specifying that we accept 'personal' (out-of-band) stream pushes
            //    new ClientEventStreamSettings { AllowCallbacks = true, ReestablishStream = false, AutoBindStreams = true, AutoBindSubscriptionContextAlteration = null});

            // target endpoint discovery - this lambda will be invoked for any target endpoint status change
            // i.e. invocation endpoints and streaming endpoints
            glue_.Interop.EndpointStatusChanged += (sender, args) =>
            {
                // is this an instance of the demo stream?
                IMethod endpoint = args.InteropEndpoint;

                // filter only streaming endpoints called GlueDemoTickStream
                if (endpoint.Definition.Name != "GlueDemoTickStream" ||
                    !endpoint.Definition.Flags.HasFlag(MethodFlags.SupportsStreaming) || !endpoint.IsValid)
                {
                    return;
                }

                // choose a random tick mode
                var rndTickMode = (TickMode) random_.Next(Enum.GetValues(typeof(TickMode)).Length - 1);

                LogMessage(
                    $"{endpoint.Definition.Name} is {args.EndpointStatus} from {endpoint.OriginalServer} - subscribing with {rndTickMode}");

                // reject 1 of every 3
                bool reject = random_.Next(2) == 0;

                // send a subscription request to that streaming endpoint
                // the Subscribe method has multiple overloads, so e.g. you can subscribe to 'Best' target of a streaming endpoint specified by its Name
                glue_.Interop.Subscribe(endpoint,
                    new ClientEventStreamHandler
                    {
                        // this lambda is invoked when the status of the stream has changed
                        EventStreamStatusChanged = (info, status, cookie) => LogMessage(
                            $"{info.EventStreamingMethod.Name} to {info.Server} is {status}"),

                        // this lambda is invoked when there is data published to the stream
                        EventHandler = (info, data, cookie) =>
                            DispatchAction(() =>
                            {
                                var isOOB = data.IsCallbackStream;
                                // if isOOB is true the data has been pushed only to this subscriber
                                Subscriptions.Items.Add($"{data.ResultContext.AsString()}");
                            })
                    },
                    // these are the arguments sent in the subscription request
                    mib => mib.SetContext(cb => cb.AddValue("reject", reject).AddValue("tickMode", (int) rndTickMode)),
                    // additional settings - specify target await timeout
                    new TargetSettings().WithTargetAwaitTimeout(TimeSpan.FromSeconds(5)),
                    // stream settings, specifying that we accept 'personal' (out-of-band) stream pushes
                    new ClientEventStreamSettings {AllowCallbacks = true, ReestablishStream = false});
            };
        }

        private void DispatchAction(Action action)
        {
            Dispatcher?.BeginInvoke(action);
        }

        private void LogMessage(string message)
        {
            DispatchAction(() => Log.Items.Add(message));
        }
    }
}