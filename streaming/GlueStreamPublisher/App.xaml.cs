using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Tick42;
using Tick42.StartingContext;

namespace GlueStreamPublisher
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
                ApplicationName = "My Glue Streams Publisher Demo",
                InitializeTimeout = TimeSpan.FromSeconds(5)
            };

            Glue42.InitializeGlue(initializeOptions)
                .ContinueWith((glue) =>
                {
                    //register glue successfuly
                    if (glue.Status == TaskStatus.RanToCompletion)
                    {
                        var glueInstance = glue.Result;
                        GetMainWindow()?.RegisterGlue(glueInstance);
                    }

                }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private MainWindow GetMainWindow()
        {
            return Windows.OfType<MainWindow>().FirstOrDefault();
        }
    }
}
