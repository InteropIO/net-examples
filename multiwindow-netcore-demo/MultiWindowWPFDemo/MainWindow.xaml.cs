using Glue.Transport;
using GlueForNetCore;
using GlueForNetCore.AppManager;
using GlueForNetCore.GDStarting;
using GlueForNetCore.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Diagnostics;

namespace MultiWindowWPFDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static MainWindow()
        {
            log4net.Config.XmlConfigurator.Configure();

        }

        private Glue42 glue42_;
        private IGlueWindow mainWindow_;
        public class AppState
        {
            public int SelectedIndex { get; set; }
        }

        public string GlueWindowId { get; set; }
        public string TabGroupId { get; set; }

        private readonly string ChildWindowAppName = "ColorChild";

        public MainWindow()
        {
            InitializeComponent();

            Visibility = Visibility.Hidden;

            TabGroupId = Guid.NewGuid().ToString();

            var initializeOptions = new InitializeOptions();

            initializeOptions.SetSaveRestoreStateEndpoint(_ =>
            new AppState
            {
                SelectedIndex = ColorSelector.SelectedIndex
            }.AsCompletedTask()
            ,
                state =>
                {
                    AppState restoreState = state;
                    // restore state
                    ColorSelector.SelectedIndex = restoreState.SelectedIndex;
                }, Dispatcher);


            Glue42.InitializeGlue(new InitializeOptions
            {
                ApplicationName = "MultiWindowDemo"
            })
                .ContinueWith(async r =>
                {
                    if (r.Status != TaskStatus.RanToCompletion)
                    {
                        //failed;
                    }
                    else
                    {
                        glue42_ = r.Result;

                        mainWindow_ = await glue42_.GlueWindows.RegisterWindow(this, builder => builder.WithTitle("MultiWindowDemo").WithChannelSupport(true).WithPlacement(new GlueWindowScreenPlacement().WithTabGroupId(TabGroupId))).ConfigureAwait(false);

                        glue42_.AppManager.RegisterWPFApp<ClientPortfolioView, ClientPortfolioView.State, MainWindow>(
                            app =>
                            {
                                app.WithName(ChildWindowAppName)
                                 .WithTitle(ChildWindowAppName)
                                 .WithContext(this)
                                 .WithType(GlueWindowType.Tab).WithFolder("MultiWindowChildApps");
                            });

                        await glue42_.AppManager
                            .AwaitApplication((app) => app.Name == ChildWindowAppName).ConfigureAwait(false);

                        var gwOptions = glue42_.GetStartupWindowOptions("MultiWindowWPF");
                    }
                });
        }

        private void Portfolio_Click(object sender, RoutedEventArgs e)
        {
            var currColor = "#FFFFFF";

            if (ColorSelector.SelectedItem != null)
            {
                currColor = ((SolidColorBrush)((Rectangle)ColorSelector.SelectedItem).Fill).Color.ToString();
            }

            var clientPortfolioView = new ClientPortfolioView(currColor);
            var synchronizationContext = SynchronizationContext.Current;


            glue42_.GlueWindows.RegisterWindow(clientPortfolioView,
                builder =>
                {
                    builder.WithTitle(ChildWindowAppName).WithType(GlueWindowType.Tab).WithPlacement(new GlueWindowScreenPlacement().WithTabGroupId(TabGroupId));
                }).ContinueWith(r =>
                {
                    glue42_.AppManager.RegisterInstance(ChildWindowAppName, r.Result.Id, clientPortfolioView, synchronizationContext)
                        .ContinueWith(instanceTask =>
                        {
                            if (instanceTask.Status != TaskStatus.RanToCompletion)
                            {
                                // register instance failed
                            }
                            else
                            {
                                var instance = instanceTask.Result;
                            }
                        });
                });
        }
    }
}
