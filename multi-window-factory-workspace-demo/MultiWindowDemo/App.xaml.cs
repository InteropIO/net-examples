using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using DOT.AGM;
using DOT.Core.Extensions;
using DOT.Logging;
using log4net;
using log4net.Config;
using Tick42;
using Tick42.StartingContext;
using static MultiWindowFactoryDemo.ChartWindow;

namespace MultiWindowFactoryDemo;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public enum AppType
    {
        ChartOne,
        ChartTwo,
        ChartThree,
        ChartFour,
        ChartFive
    }

    private ILog logger_;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        XmlConfigurator.Configure();
        DotLoggingFacade.Instance = new Log4NetFacade();

        logger_ = LogManager.GetLogger(typeof(App));

        RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            logger_.Error($"Unobserved exception sent by {sender}", args.Exception);
        };

        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            logger_.Error($"Unhandled exception sent by {sender}", args.ExceptionObject as Exception);
        };

        var initializeOptions = new InitializeOptions
        {
            LogLibrary = LogLibrary.UseCustomFacade,
            // do not save the main app in layouts - just the children
            AppDefinition = new AppDefinition { IgnoreFromLayouts = true }
        };


        // if the app is not saved/restored - this is not needed
        initializeOptions.SetSaveRestoreStateEndpoint(_ =>
            // Returning the state that has to be saved when the applicaiton is saved in a layout
            // In this case the app saves the selected color from the dropdown
            new AppState().AsCompletedTask(), null, Dispatcher);

        var glue = await Glue42.InitializeGlue(initializeOptions);

        foreach (var appType in Enum.GetValues(typeof(AppType)).Cast<AppType>())
        {
            await glue.AppManager.RegisterWPFAppAsync<ChartWindow, SymbolState, AppFactoryContext>(app =>
            {
                app.WithName($"ChildApp_{appType}")
                    .WithTitle(appType.ToString())
                    .WithContext(new AppFactoryContext { AppType = appType, MainApp = this })
                    .WithDispatcher(Dispatcher.CurrentDispatcher)
                    .WithAppDefinitionModifier(appDef =>
                    {
                        appDef.AllowWorkspaceDrop = true;
                        if (appDef.CustomProperties == null)
                        {
                            appDef.CustomProperties = new Dictionary<string, Value>();
                        }

                        appDef.CustomProperties["includeInWorkspaces"] = true;
                        return appDef;
                    });
            });
        }
    }

    public class AppFactoryContext
    {
        public AppType AppType { get; set; }
        public App MainApp { get; set; }
    }

    public class AppState
    {
        public string FactoryState { get; set; }
    }
}