using System;
using System.Threading.Tasks;
using System.Windows;
using DOT.Core.Extensions;
using Tick42;
using Tick42.AppManager;
using Tick42.StartingContext;
using Tick42.Windows;
using static MultiWindowDemo.ChartWindow;

namespace MultiWindowDemo
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string ChartWindowAppName = "ChartChild";
        private const string ColorWindowAppName = "ColorChild";

        private Glue42 glue42_;

        public MainWindow()
        {
            InitializeComponent();

            var initializeOptions = new InitializeOptions();
            initializeOptions.SetSaveRestoreStateEndpoint(_ =>
                // Returning the state that has to be saved when the applicaiton is saved in a layout
                // In this case the app saves the selected color from the dropdown
                new AppState
                {
                    SelectedIndex = ColorSelector.SelectedIndex
                }.AsCompletedTask(), null, Dispatcher);

            Task<Glue42> g = Glue42.InitializeGlue(initializeOptions);

            // Registering the main window and then continuing with the registration of the additional applications
            RegisterMainWindow(g).ContinueWith(async r =>
            {
                // glue will be initialized with the registration of the main window
                glue42_ = g.Result;
                // Getting the restored state if one is provided (the restored state will be populated when the app is restored from GlueDesktop)
                var appState = glue42_.GetRestoreState<AppState>();
                ColorSelector.SelectedIndex = appState?.SelectedIndex ?? -1;

                await RegisterChartApp().ConfigureAwait(false);
                await RegisterColorApp().ConfigureAwait(false);
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        public string TabGroupId { get; set; }

        private async void Color_Click(object sender, RoutedEventArgs e)
        {
            var cxt = AppManagerContext.CreateNew();
            cxt.SetObject(new ColorWindow.State
            {
                RectangleColor = "#AAAAAA"
            }, glue42_.AGMObjectSerializer);
            await (await glue42_.AppManager.AwaitApplication(app => app.Name == ColorWindowAppName)).Start(cxt);
        }

        private void AssociateWindowToAppInstance()
        {
            var chartWindow = new ChartWindow();
            var placement =
                new GlueWindowScreenPlacement()
                    .WithTabGroupId(
                        TabGroupId); // Adding the tab group id so the chart window appears in the same tab group as the main window

            // With the RegisterAppWindow invocation both the window and an instance of the application are being registered
            glue42_.GlueWindows.RegisterAppWindow(chartWindow, chartWindow, ChartWindowAppName,
                builder => builder
                    .WithPlacement(placement)
                    .WithType(GlueWindowType.Tab)
                    .WithTitle(ChartWindowAppName));

            // alternatively if you have a GlueWindow you can use glue42_.AppManager.RegisterInstance to bind it as an application instance
        }

        private async void Chart_Click(object sender, RoutedEventArgs e)
        {
            var cxt = AppManagerContext.CreateNew();
            cxt.SetObject(new SymbolState
            {
                ActiveSymbol = "FIRST.L"
            }, glue42_.AGMObjectSerializer);
            await (await glue42_.AppManager.AwaitApplication(app => app.Name == ChartWindowAppName)).Start(
                cxt);
        }

        private Task<IGlueWindow> RegisterMainWindow(Task<Glue42> initGlueTask)
        {
            TabGroupId = Guid.NewGuid().ToString();
            return initGlueTask.RegisterWindow(this, gwo =>
            {
                gwo.WithChannelSupport(true).WithTitle("MultiWindowWPF").WithType(GlueWindowType.Tab);

                // Making sure that the TabGroupId is correct, so the windows can be opened in the same tab group when started from the MainWindow
                if (gwo.Placement is GlueWindowScreenPlacement placement && placement.TabGroupId == null)
                {
                    placement.WithTabGroupId(TabGroupId);
                }
                else if (gwo.Placement == null)
                {
                    gwo.Placement = new GlueWindowScreenPlacement().WithBounds(new GlueWindowBounds(0, 0, 600, 600))
                        .WithTabGroupId(TabGroupId);
                }
                else if (gwo.Placement is GlueWindowScreenPlacement screenPlacement &&
                         screenPlacement.TabGroupId != null)
                {
                    TabGroupId = screenPlacement.TabGroupId;
                }
            });
        }

        private async Task RegisterColorApp()
        {
            var baseApp = await glue42_.AppManager
                .AwaitApplication(app => app.Name == ChartWindowAppName).ConfigureAwait(false);

            var baseDefinition = await baseApp.GetFullConfig().ConfigureAwait(false);
            try
            {
                await glue42_.AppManager
                    .RegisterWPFAppAsync<ColorWindow, ColorWindow.State, MainWindow>(
                        app =>
                        {
                            app.WithAppDefinitionModifier(appDef =>
                            {
                                baseDefinition.Title = ColorWindowAppName;
                                baseDefinition.Name = ColorWindowAppName;
                                baseDefinition.Details.Owner = glue42_.InitializeOptions.ApplicationName;
                                baseDefinition.Details.WindowStyle = null;
                                baseDefinition.Type = ApplicationType.ChildWindow;
                                return baseDefinition;
                            });
                        }).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private Task RegisterChartApp()
        {
            // Registering the WPF window as a Glue application and providing the shape of its state
            return glue42_.AppManager.RegisterWPFAppAsync<ChartWindow, SymbolState, MainWindow>(app =>
            {
                app.WithName(ChartWindowAppName)
                    .WithTitle(ChartWindowAppName)
                    .WithContext(this)
                    .WithType(GlueWindowType.Tab);
            });
        }

        // The shape of the state that will be saved and restored with the window
        public class AppState
        {
            public int SelectedIndex { get; set; }
        }
    }
}