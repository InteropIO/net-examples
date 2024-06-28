using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Windows;
using DOT.AGM.GwTransport;
using DOT.Logging;
using log4net;
using log4net.Config;
using Tick42.StartingContext;

namespace MultipleInstancesDemo
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const string MutexName = "MultipleInstancesDemo_SingleInstanceMutex";
        private const string PipeName = "MultipleInstancesDemo_Pipe";

        private int instance_;
        private volatile int alive_;

        private Mutex mutex_;
        private Thread pipeServerThread_;

        private GwProtocolSerializer serializer_;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            AppDomain.CurrentDomain.UnhandledException += (sender, args) => { };
            Debugger.Launch();
            XmlConfigurator.Configure();
            DotLoggingFacade.UseLibrary(LogLibrary.DynamicLog4Net);
            serializer_ = new GwProtocolSerializer(ValueGwConverter.Settings.None);

            // or choose another ShutdownMode if needed
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            mutex_ = new Mutex(true, MutexName, out bool isOwned);

            var rco = GDStartingContextProvider.ExtractRemoteConfigurationOptionsFromCmdLine() ??
                      GDStartingContextProvider.ExtractRemoteConfigurationOptionsFromEnvironment();

            if (isOwned)
            {
                // First instance
                StartPipeServer();
                ProcessArguments(e.Args, rco);
            }
            else
            {
                // Second instance
                SendArgumentsToFirstInstance(e.Args, rco);
                // Terminate the second instance
                Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            mutex_.Dispose();
            pipeServerThread_?.Abort();
            base.OnExit(e);
        }


        private T DeserializeMessage<T>(string message, T _)
        {
            return serializer_.DeserializeMessage<T>(message);
        }

        private void StartPipeServer()
        {
            pipeServerThread_ = new Thread(() =>
            {
                while (true)
                {
                    using (var pipeServer = new NamedPipeServerStream(PipeName, PipeDirection.In))
                    {
                        try
                        {
                            pipeServer.WaitForConnection();

                            using (StreamReader reader = new StreamReader(pipeServer))
                            {
                                string message = reader.ReadToEnd();
                                if (!string.IsNullOrEmpty(message))
                                {
                                    var data = DeserializeMessage(message,
                                        new { args = new string[0], rco = new RemoteConfigurationOptions() });
                                    ProcessArguments(data.args, data.rco);
                                }
                            }

                            pipeServer.Disconnect();
                        }
                        catch
                        {
                            // swallow the exception and continue waiting for the next connection
                        }
                    }
                }
            })
            {
                IsBackground = true
            };

            pipeServerThread_.Start();
        }

        private void SendArgumentsToFirstInstance(string[] args, RemoteConfigurationOptions rco)
        {
            using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
            {
                pipeClient.Connect(1000); // Timeout of 1 second
                using (StreamWriter writer = new StreamWriter(pipeClient))
                {
                    var msg = serializer_.SerializeMessage(new
                    {
                        args,
                        rco
                    });
                    writer.Write(msg);
                    writer.Flush();
                }
            }
        }

        private void ProcessArguments(string[] args, RemoteConfigurationOptions rco)
        {
            // isolate the window in a new AppDomain, so it has its own log file

            var setup = new AppDomainSetup
            {
                ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                PrivateBinPath = "bin" // Ensure the private bin path is set if necessary
            };

            var newDomain = AppDomain.CreateDomain(rco?.InstanceId ?? "Main", null, setup);
            newDomain.Load(typeof(LogManager).Assembly.FullName);

            var runnerType = typeof(Isolator);
            var runner = (Isolator)newDomain.CreateInstanceAndUnwrap(runnerType.Assembly.FullName, runnerType.FullName);

            // Run the window in the new AppDomain inside the dispatcher of the main AppDomain
            // otherwise the app will have to spin a STA thread for the window
            Dispatcher.BeginInvoke((Action)(() =>
            {
                Interlocked.Increment(ref alive_);
                runner.RunIsolated(args, new SerializableRCO(rco), ++instance_);
                // Optionally unload the new AppDomain when done
                AppDomain.Unload(newDomain);
                if (Interlocked.Decrement(ref alive_) == 0)
                {
                    Shutdown();
                }
            }));


            // or handle the arguments as needed in your application
            // and create the window in the same AppDomain - this will share the log file with the main instance
            // Dispatcher.Invoke(() =>
            // {
            //     // Handle the arguments in the UI thread
            //     // For example, pass the arguments to a new instance of the MainWindow
            //     
            //     var window = new MainWindow();
            //
            //     window.ProcessArguments(args, rco);
            // });
        }

        [Serializable]
        public class SerializableRCO
        {
            public SerializableRCO(RemoteConfigurationOptions rco)
            {
                if (rco == null)
                {
                    return;
                }

                AppName = rco.AppName;
                InstanceId = rco.InstanceId;
                Environment = rco.Environment;
                Region = rco.Region;
                GwUrl = rco.GwUrl;
                GwToken = rco.GwToken;
                Username = rco.Username;
            }

            public string Environment { get; set; }
            public string Region { get; set; }
            public string GwUrl { get; set; }
            public string GwToken { get; set; }
            public string AppName { get; set; }
            public string InstanceId { get; set; }
            public string Username { get; set; }

            public RemoteConfigurationOptions ToRCO()
            {
                return new RemoteConfigurationOptions
                {
                    AppName = AppName,
                    InstanceId = InstanceId,
                    Environment = Environment,
                    Region = Region,
                    GwUrl = GwUrl,
                    GwToken = GwToken,
                    Username = Username
                };
            }
        }

        public class Isolator : MarshalByRefObject
        {
            public void RunIsolated(string[] args, SerializableRCO rco, int instance)
            {
                ConfigureLog4Net(rco?.InstanceId ?? "main");
                DotLoggingFacade.UseLibrary(LogLibrary.DynamicLog4Net);
                var app = new Application();
                var window = new MainWindow();
                window.ProcessArguments(args, rco?.ToRCO(), instance);
                app.Run(window);
            }

            private void ConfigureLog4Net(string instance)
            {
                // Load log4net configuration from a file or create programmatically
                Environment.SetEnvironmentVariable("instance_name", instance);
                //var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
                string logConfigFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log4net.config");
                XmlConfigurator.Configure(new FileInfo(logConfigFilePath));

                // Set a different log file for each AppDomain
                // foreach (var appender in log4net.LogManager.GetRepository().GetAppenders())
                // {
                //     if (appender.Name == "RollingFileAppender" && appender is RollingFileAppender fileAppender)
                //     {
                //         string logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{instance}.log");
                //         fileAppender.File = logFile;
                //         fileAppender.ActivateOptions(); // Refresh the appender configuration
                //     }
                // }
            }
        }
    }
}