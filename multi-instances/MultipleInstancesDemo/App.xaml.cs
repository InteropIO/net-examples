using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Windows;
using DOT.AGM.GwTransport;
using DOT.Logging;
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

        private Mutex mutex_;
        private Thread pipeServerThread_;

        private GwProtocolSerializer serializer_;


        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
            };
            Debugger.Launch();
            XmlConfigurator.Configure();
            DotLoggingFacade.UseLibrary(LogLibrary.DynamicLog4Net);
            serializer_ = new GwProtocolSerializer(ValueGwConverter.Settings.None);

            // or choose another ShutdownMode if needed
            ShutdownMode = ShutdownMode.OnLastWindowClose;

            mutex_ = new Mutex(true, MutexName, out bool isOwned);

            if (isOwned)
            {
                // First instance
                StartPipeServer();
                ProcessArguments(e.Args, null);
            }
            else
            {
                // Second instance

                var rco = GDStartingContextProvider.ExtractRemoteConfigurationOptionsFromCmdLine() ??
                          GDStartingContextProvider.ExtractRemoteConfigurationOptionsFromEnvironment();

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
            // Handle the arguments as needed in your application
            Dispatcher.Invoke(() =>
            {
                // Handle the arguments in the UI thread
                // For example, pass the arguments to a new instance of the MainWindow
                var window = new MainWindow();
                window.ProcessArguments(args, rco);
            });
        }
    }
}