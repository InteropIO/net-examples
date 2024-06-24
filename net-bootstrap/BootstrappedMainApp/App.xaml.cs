using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using DOT.AGM;
using Tick42;
using Tick42.AppManager;
using Tick42.StartingContext;

namespace BootstrappedMainApp
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const string LauncherApp = "MainAppBootstrapper";

        private void App_Startup(object sender, StartupEventArgs e)
        {
            // for debugging purposes
            Debugger.Launch();

            // since there's no main window, this can terminated only by killing the process
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            if (e.Args.Length == 0)
            {
                MessageBox.Show("Launched without credentials! Use the bootstrapper!");
                Current.Shutdown(-1);
            }

            var initializeOptions = new InitializeOptions
            {
                // must match with the name as defined in the app def json
                ApplicationName = "BootstrappedMainApp",

                AppDefinition = new AppDefinition
                {
                    Title = "BootstrappedMainApp_" + string.Join(",", e.Args),
                    TerminateOnShutdown = false,
                    // must match the definition of the bootstrapper in the Glue app json
                    LauncherApp = LauncherApp,
                    StartupArguments = "",
                    IgnoreFromLayouts = true,
                    AllowMultiple = false,
                    StartingContextMode = StartingContextMode.None,
                    TrackingType = TrackingType.AGM
                },
            };

            Glue42.InitializeGlue(initializeOptions)
                .ContinueWith(async glueTask =>
                {
                    Glue42 glue = glueTask.Result;

                    // registered by json definition
                    // uncomment if you want to register the bootstrapper from code
                    // await RegisterBootstrapper(glue).ConfigureAwait(false);

                    await glue.AppManager.RegisterWPFAppAsync<BootstrappedWindow1, DummyState, DummyContext>(builder =>
                    {
                    });
                    await glue.AppManager.RegisterWPFAppAsync<BootstrappedWindow2, DummyState, DummyContext>(builder =>
                    {
                    });
                });
        }

        private static async Task RegisterBootstrapper(Glue42 glue)
        {
            var bootstrapperFolder = Path.GetFullPath(Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "..", "..", "..",
                "Bootstrapper", "bin", "debug"));

            var tcs = new TaskCompletionSource<(MethodInvocationStatus, string)>(TaskCreationOptions
                .RunContinuationsAsynchronously);

            glue.GetService<IApplicationRegistryService>().RegisterHostApplication(new AppConfig
            {
                Name = LauncherApp,
                Type = ApplicationType.Exe,
                Details = new ApplicationDefinitionDetails
                {
                    StartingContextMode = StartingContextMode.None,
                    TrackingType = TrackingType.Process,
                    Command = "MainAppBootstrapper.exe",
                    Path = bootstrapperFolder,
                    TerminateOnShutdown = false,
                },
                Hidden = true,
                AllowMultiple = false,
                AutoStart = false,
                IgnoreFromLayouts = true,
                IgnoreSaveOnClose = true,
                IgnoreSavedLayout = true,
            }, AppDefinitionLifetime.Default, r => tcs.TrySetResult((r.Status, r.ResultMessage)));

            if (await tcs.Task.ConfigureAwait(false) is var status && status.Item1 != MethodInvocationStatus.Succeeded)
            {
                throw new Exception(status.Item2);
            }
        }

        public class DummyState
        {
        }

        public class DummyContext
        {
        }
    }
}