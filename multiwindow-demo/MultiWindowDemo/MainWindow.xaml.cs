using DOT.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Tick42;
using Tick42.AppManager;
using Tick42.StartingContext;
using Tick42.Windows;

namespace MultiWindowDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public Glue42 glue42_;

        // The shape of the state that will be saved and restored with the window
        public class AppState
        {
            public int SelectedIndex { get; set; }
        }

        public string TabGroupId { get; set; }

        private readonly string ColorWindowAppName = "ColorChild";
        private readonly string ChartWindowAppName = "ChartChild";

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
            RegisterMainWindow(g).ContinueWith(r =>
            {
                // glue will be initialized with the registration of the main window
                glue42_ = g.Result;
                // Getting the restored state if one is provided (the restored state will be populated when the app is restored from GlueDesktop)
                var appState = glue42_.GetRestoreState<AppState>();
                ColorSelector.SelectedIndex = appState?.SelectedIndex ?? -1;

                RegisterColorApp();
                RegisterChartApp();
            }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void Color_Click(object sender, RoutedEventArgs e)
        {
            var currColor = "#FFFFFF"; // white is the default color

            if (ColorSelector.SelectedItem != null)
            {
                currColor = ((SolidColorBrush)((Rectangle)ColorSelector.SelectedItem).Fill).Color.ToString();
            }

            var colorWindow = new ColorWindow(currColor);
            var synchronizationContext = SynchronizationContext.Current;

            // First the window is registered as a GlueWindow and then an instance of the application which corresponds to the window is registered
            RegisterColorWindow(colorWindow).ContinueWith(r =>
            {
                glue42_.AppManager.RegisterInstance(ColorWindowAppName, r.Result.Id, colorWindow, synchronizationContext);
            });
            // Too see how to do it in one invocation please see Chart_Click
        }

        private void Chart_Click(object sender, RoutedEventArgs e)
        {
            var chartWindow = new ChartWindow();
            var synchronizationContext = SynchronizationContext.Current;
            var placement = new GlueWindowScreenPlacement().WithTabGroupId(TabGroupId); // Adding the tab group id so the chart window appears in the same tab group as the main window

            // With the RegisterAppWindow invocation both the window and an instance of the application are being registered
            glue42_.GlueWindows.RegisterAppWindow(chartWindow, chartWindow, ChartWindowAppName,
                builder => builder
                .WithPlacement(placement)
                .WithType(GlueWindowType.Tab)
                .WithTitle(ChartWindowAppName));
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
                     gwo.Placement = new GlueWindowScreenPlacement().WithTabGroupId(TabGroupId);
                 }
                 else if (gwo.Placement is GlueWindowScreenPlacement screenPlacement && screenPlacement.TabGroupId != null)
                 {
                     TabGroupId = screenPlacement.TabGroupId;
                 }
             });
        }

        private void RegisterColorApp()
        {
            // Registering the WPF window as a Glue application and providing the shape of its state
            glue42_.AppManager.RegisterWPFApp<ColorWindow, ColorWindow.State, MainWindow>(app =>
            {
                app.WithName(ColorWindowAppName)
                 .WithTitle(ColorWindowAppName)
                 .WithContext(this)
                 .WithType(GlueWindowType.Tab);
            });
        }

        private void RegisterChartApp()
        {
            // Registering the WPF window as a Glue application and providing the shape of its state
            glue42_.AppManager.RegisterWPFApp<ChartWindow, ChartWindow.SymbolState, MainWindow>((app) =>
            {
                app.WithName(ChartWindowAppName)
                .WithTitle(ChartWindowAppName)
                .WithContext(this)
                .WithType(GlueWindowType.Tab);
            });
        }

        private Task<IGlueWindow> RegisterColorWindow(Window colorWindow)
        {
            return glue42_.AsCompletedTask().RegisterWindow(colorWindow, gwo =>
            {
                gwo.WithTitle(ColorWindowAppName).WithType(GlueWindowType.Tab);

                // Making sure the TabGroupId is correct, so the windows can be opened in the same tab group when started from the MainWindow
                if (gwo.Placement is GlueWindowScreenPlacement placement && placement.TabGroupId == null)
                {
                    placement.WithTabGroupId(TabGroupId);
                }
                else if (gwo.Placement == null)
                {
                    gwo.Placement = new GlueWindowScreenPlacement().WithTabGroupId(TabGroupId);
                }
            });
        }
    }
}
