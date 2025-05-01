using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using DOT.AGM;
using DOT.Core.Extensions;
using DOT.Logging;
using log4net;
using log4net.Config;
using Tick42;
using Tick42.AppManager;
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
                    .WithFolder("Children")
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

        // register WPF window with a custom factory
        await glue.AppManager.RegisterWPFAppAsync<object, ChartWindow, SymbolState, AppFactoryContext>(app =>
        {
            app.WithName("MainApp")
                .WithFolder("Children")
                .WithTitle("ChartZero")
                .WithAppKey(
                    new object()) // optional appKey, can be anything and is received in the factory - for correlation/cookie purpose
                .WithContext(new AppFactoryContext { AppType = AppType.ChartOne, MainApp = this })
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
        }, async (context, builder, appKey) =>
        {
            // do something async, if required
            // control over the creation of the window
            var wnd = new ChartWindow();
            // init something else in the wnd, if needed

            // return the ready window
            return wnd;
        });


        // fully custom app factory
        // use lambda app factory to create a window with complete freedom
        // in here you're not limited to the WPF app factory
        // you can use any window - if you have its handle
        // e.g. you can launch a new process and use its window handle
        await glue.AppManager.RegisterAppFactoryAsync<object, LambdaApp<string>, string, object>(
            app =>
                app.WithFolder("Children")
                    .WithAllowMultiple(true)
                    .WithName("GenericWindow")
                    .WithContext(new AppFactoryContext { AppType = AppType.ChartOne, MainApp = this })
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
                    }),
            async (context, builder, __) =>
            {
                var anyWindow = new Window
                {
                    Content = new TextBlock
                    {
                        Text = "Specific content on thread " + Thread.CurrentThread.ManagedThreadId,
                        FontSize = 20,
                        Foreground = Brushes.Red,
                        Background = Brushes.Yellow
                    }
                };

                // position it outside the screen so it's not 'seen' initially
                anyWindow.Top = -10000;
                anyWindow.Left = -10000;
                // initially show the window to force WPF initialization
                anyWindow.Show();

                // get the handle
                var handle = new WindowInteropHelper(anyWindow).EnsureHandle();

                // pass it to the lambda app
                // you can create your own LambdaApp implementation that suits you better
                return new LambdaApp<string>(handle)
                {
                    ChannelChanged = (channelContext, channel, prevChannel) => { },
                    ChannelUpdate = (channelContext, channel, updateArgs) => { },
                    // ... Init, GetState, Shutdown
                };
            });

        // same freedom, but also start each window in its own thread
        await glue.AppManager.RegisterAppFactoryAsync<object, LambdaApp<string>, string, object>(
            app =>
                app.WithFolder("Children")
                    .WithAllowMultiple(true)
                    .WithName("GenericWindow_SeparateThread")
                    .WithContext(new AppFactoryContext { AppType = AppType.ChartOne, MainApp = this })
                    .WithAppDefinitionModifier(appDef =>
                    {
                        appDef.AllowWorkspaceDrop = true;
                        if (appDef.CustomProperties == null)
                        {
                            appDef.CustomProperties = new Dictionary<string, Value>();
                        }

                        appDef.CustomProperties["includeInWorkspaces"] = true;
                        return appDef;
                    }),
            (context, builder, __) =>
            {
                var tcs = new TaskCompletionSource<LambdaApp<string>>();
                var dispThread = new Thread(() =>
                {
                    var anyWindow = new Window
                    {
                        Content = new TextBlock
                        {
                            Text = "Specific content on thread " + Thread.CurrentThread.ManagedThreadId,
                            FontSize = 20,
                            Foreground = Brushes.Red,
                            Background = Brushes.Yellow
                        }
                    };

                    // position it outside the screen so it's not 'seen' initially
                    anyWindow.Top = -10000;
                    anyWindow.Left = -10000;
                    // initially show the window to force WPF initialization
                    anyWindow.Show();

                    anyWindow.Closed += (sender, args) => Dispatcher.ExitAllFrames();

                    // get the handle
                    var handle = new WindowInteropHelper(anyWindow).EnsureHandle();

                    // pass it to the lambda app
                    // you can create your own LambdaApp implementation that suits you better
                    var la = new LambdaApp<string>(handle)
                    {
                        ChannelChanged = (channelContext, channel, prevChannel) => { },
                        ChannelUpdate = (channelContext, channel, updateArgs) => { },
                        // ... Init, GetState, Shutdown
                    };

                    tcs.TrySetResult(la);

                    Dispatcher.Run();
                });
                dispThread.SetApartmentState(ApartmentState.STA);
                dispThread.Name = "LambdaAppThread";
                dispThread.Start();
                return tcs.Task;

            });
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