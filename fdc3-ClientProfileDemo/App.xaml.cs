#region Tick42 namespaces

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using DOT.AGM;
using FDC3ChannelsClientProfileDemo.POCO;
using log4net;
using log4net.Config;
using Tick42;
using Tick42.StartingContext;

#endregion

[assembly: XmlConfigurator(Watch = true)]

namespace FDC3ChannelsClientProfileDemo
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    partial class App : Application
    {
        static volatile bool isDisposed_;
        private ILog logger_;

        public static AppMode Mode { get; private set; }
        public static bool StickyWindowsEnabled => Utils.IsStickyWindowsEnabled(Mode);
        public static bool PortfolioHidden => Utils.IsGlueEnabled(Mode);
        public static string GlueUsername { get; private set; }
        public static bool UseAlternateLogo { get; private set; }

        public static Glue42 Glue { get; private set; }
        public static AppState StartupState { get; }
        public static string PortfolioAppName { get; private set; }
        public static bool DefaultToDarkTheme { get; private set; }
        public static string ClientsUrl { get; private set; }

        public void Dispose()
        {
        }

        // Lifecycle
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // General app logic unrelated to Glue
            ConfigureLogging();

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;

            if (Utils.ForceDebug())
            {
                Debugger.Break();
            }

            Mode = Utils.GetAppMode();
            DefaultToDarkTheme = Utils.ShouldDefaultToDarkTheme();
            PortfolioAppName = Utils.GetArg("portfolio-app-name") ?? "channelsclientportfolio";
            ClientsUrl = Utils.GetArg("clients-url") ?? Utils.GetArg("clients") ?? "http://localhost:22060/clients";
            UseAlternateLogo = Utils.GetArg("altLogo", "false").Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            logger_.Error("Unhandled exception: " + e.Exception);
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            logger_.Error("Unhandled exception: " + e.Exception);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            logger_.Error("Unhandled exception: " + e.ExceptionObject);
        }

        private void ConfigureLogging()
        {
            XmlConfigurator.Configure();
            logger_ = LogManager.GetLogger("ChannelsClientProfileDemo");
            logger_.Info("Starting app in mode " + Mode);
            logger_.Info("Arguments: " + Environment.CommandLine);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Exit();
            base.OnExit(e);
        }

        public new static void Exit()
        {
            if (!isDisposed_)
            {
                isDisposed_ = true;

                Glue?.Shutdown();

                foreach (Window window in Current.Windows)
                {
                    try
                    {
                        window.Close();
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }
        }

        internal void InitializeGlue()
        {
            // initialize Tick42 Interop (AGM) and Metrics components
            GlueUsername = Utils.GetArg("user") ?? Utils.GetArg("username") ?? Environment.UserName;
            string gluePassword = Utils.GetArg("pass") ?? Utils.GetArg("password") ?? "";
            string gatewayUrlOverride = Utils.GetArg("gw") ?? Utils.GetArg("gateway");

            // these envvars are expanded in some configuration files
            Environment.SetEnvironmentVariable("PROCESSID", Process.GetCurrentProcess().Id + "");
            Environment.SetEnvironmentVariable("GW_USERNAME", GlueUsername);
            Environment.SetEnvironmentVariable("DEMO_MODE", Mode + "");

            var initializeOptions = new InitializeOptions
            {
                ApplicationName = "ChannelsClientProfileDemo",
                IncludedFeatures = GDFeatures.UseAppManager | GDFeatures.UseGlueWindows | GDFeatures.UseGlueWindows |
                                   GDFeatures.UseNotifications | GDFeatures.UseContexts,
                Credentials = Tuple.Create(GlueUsername, gluePassword),
                GatewayUri = gatewayUrlOverride
            };
            initializeOptions.SetSaveRestoreStateEndpoint(GetState);

            Glue42.InitializeGlue(initializeOptions)
                .ContinueWith(glue => Dispatcher.BeginInvoke((Action)(() =>
                {
                    if (glue.Status == TaskStatus.Faulted)
                    {
                        // glue init failed - still be visible
                        GetMainWindow().Visibility = Visibility.Visible;
                        return;
                    }

                    Glue = glue.Result;

                    if (Glue.GlueWindows == null)
                    {
                        GetMainWindow().Visibility = Visibility.Visible;
                    }

                    logger_.Info("Initialized Glue Metrics and AGM");

                    Glue.Metrics?.TrackUserJourneyMetrics(this, trackWindows: true, trackClicks: true);

                    logger_.Info("Initialized Glue");

                    //register glue
                    _ = GetMainWindow().RegisterGlue(Glue);
                })));
        }

        public Task<AppState> GetState(Value value)
        {
            var tcs = new TaskCompletionSource<AppState>();

            Dispatcher.BeginInvoke((Action)(() =>
            {
                var appState = GetMainWindow()?.GetState();
                tcs.TrySetResult(appState);
            }));

            return tcs.Task;
        }

        private MainWindow GetMainWindow()
        {
            return Windows.OfType<MainWindow>().FirstOrDefault();
        }
    }
}