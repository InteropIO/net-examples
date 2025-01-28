using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using Glue;
using Glue.GDStarting;
using Glue.Helpers;
using Glue.Transport;
using Glue.Windows;
using log4net.Config;

namespace MultiWindowWPFDemo
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string ChildWindowAppName = "ColorChildNETCore";

        private Glue42 glue42_;
        private IGlueWindow mainWindow_;

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
                }, Dispatcher.AsGlueDispatcher());

            XmlConfigurator.Configure();

            Glue42.InitializeGlueAndTrack(new InitializeOptions
                {
                    ApplicationName = "MultiWindowDemoNETCore",
                    LoggerFactory = new Log4NetLoggerFactory(),
                    AdvancedOptions = new AdvancedOptions
                    {
                        InstanceValidator = config => config.TryPidAndPort(),
                        GDInstanceSelector = contexts =>
                        {
                            // choose some instance to connect to
                            return contexts.FirstOrDefault().AsCompletedTask();
                        },
                    }
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

                        mainWindow_ = await glue42_.GlueWindows.RegisterStartupWindow(this, "MultiWindowDemoNETCore",
                                builder => builder.WithChannelSupport(true)
                                    .WithPlacement(
                                        new GlueWindowScreenPlacement().WithBounds(
                                            new GlueWindowBounds(50, 50, 600, 600)).WithTabGroupId(TabGroupId)))
                            .ConfigureAwait(false);

                        await glue42_.AppManager
                            .RegisterWPFApp<ClientPortfolioView, ClientPortfolioView.State, MainWindow>(
                                app =>
                                {
                                    app.WithName(ChildWindowAppName)
                                        .WithTitle(ChildWindowAppName)
                                        .WithContext(this)
                                        .WithType(GlueWindowType.Tab).WithFolder("MultiWindowNETCoreChildApps");
                                });

                        await glue42_.AppManager
                            .AwaitApplication(app => app.Name == ChildWindowAppName).ConfigureAwait(false);
                    }
                });
        }

        public string GlueWindowId { get; set; }
        public string TabGroupId { get; set; }

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
                    builder.WithTitle(ChildWindowAppName).WithType(GlueWindowType.Tab)
                        .WithPlacement(
                            new GlueWindowScreenPlacement().WithBounds(
                                new GlueWindowBounds(70, 70, 250, 150)).WithTabGroupId(TabGroupId));
                }).ContinueWith(r =>
            {
                glue42_.AppManager.RegisterInstance(ChildWindowAppName, r.Result.Id, clientPortfolioView,
                        synchronizationContext)
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

        public class AppState
        {
            public int SelectedIndex { get; set; }
        }
    }
}