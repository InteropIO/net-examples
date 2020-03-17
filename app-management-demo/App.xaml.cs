using DOT.Logging;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Tick42;
using Tick42.StartingContext;

namespace AppManagerDemo
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;

            InitializeGlue();
        }

        private void InitializeGlue()
        {
            var initializeOptions = new InitializeOptions()
            {
                ApplicationName = "AppManager Demo",
                IncludedFeatures = GDFeatures.UseAppManager | GDFeatures.UseGlueWindows,
                LogLibrary = LogLibrary.StaticLog4Net,
                InitializeTimeout = TimeSpan.FromSeconds(5)
            };

            Glue42.InitializeGlue(initializeOptions)
                .ContinueWith((glue) =>
                {
                    //unable to register glue
                    if (glue.Status == TaskStatus.Faulted)
                    {
                        ShowMainWindow();
                        return;
                    }

                    var glueInstance = glue.Result;

                    //window management in not initialized correctly 
                    if (glueInstance.GlueWindows == null)
                    {
                        ShowMainWindow();
                    }

                    //register glue
                    GetMainWindow()?.RegisterGlue(glueInstance);

                }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private MainWindow GetMainWindow()
        {
            return Windows.OfType<MainWindow>().FirstOrDefault();
        }

        private void ShowMainWindow()
        {
            var mainWindow = GetMainWindow();
            if (mainWindow != null)
            {
                mainWindow.Visibility = Visibility.Visible;
            }
        }
    }
}
