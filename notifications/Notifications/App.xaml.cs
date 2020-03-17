using DOT.AGM;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Tick42;
using Tick42.StartingContext;

namespace WPFApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            InitializeGlue();
        }

        private void InitializeGlue()
        {
            var initializeOptions = new InitializeOptions()
            {
                ApplicationName = "My Glue Notification Demo",
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

                    //register glue
                    var glueInstance = glue.Result;
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
