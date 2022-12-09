using System;
using System.Threading;
using System.Threading.Tasks;
using Glue;
using Glue.AppManager;
using Glue.AuthenticationProviders;
using Glue.Channels;
using Glue.GDStarting;
using Glue.Logging;
using Glue.Transport;
using Glue.Windows;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace GlazorWebAssembly6
{
    public class GlueProvider
    {
        private const string DefaultGatewayUri = "ws://127.0.0.1:8385";
        private readonly IGlueLoggerFactory glueLoggerFactory_;

        private readonly IJSRuntime jsRuntime_;
        public IGlueLog Logger { get; }
        private TaskCompletionSource<IGlue42Base> glueTcs_;

        public GlueProvider(IJSRuntime jsRuntime, IGlueLoggerFactory loggerFactory, IServiceProvider serviceProvider)
        {
            jsRuntime_ = jsRuntime;

            glueLoggerFactory_ ??= loggerFactory;
            Logger ??= glueLoggerFactory_.GetLogger(typeof(GlueProvider));

            Logger.Info($"Initialized {nameof(GlueProvider)}");
        }

        public IGlue42Base Glue42 { get; private set; }

        public IGlueWindow MainWindow { get; private set; }

        public async Task<IGlueWindow> GetMainWindow()
        {
            await InitGlue(null).ConfigureAwait(false);

            return MainWindow;
        }

        public async Task<IGlue42Base> InitGlue(string uri)
        {
            if (Interlocked.CompareExchange(ref glueTcs_,
                    new TaskCompletionSource<IGlue42Base>(TaskCreationOptions.RunContinuationsAsynchronously), null) is
                { } tcs)
            {
                //already initialized
                return await tcs.Task.ConfigureAwait(false);
            }

            Logger.Info("Initializing Glue");

            InitializeOptions initOptions;

            string windowId = null;

            try
            {
                // try to get the GD hosted settings if this is started as a GD application (in order to do this u have to configure your own application in %localappdata%\tick42\gluedesktop\config\apps by adding a .json file for it
                initOptions = await Glue42Base.GetHostedGDOptions(
                    async tokenName => await jsRuntime_.InvokeAsync<string>(tokenName).ConfigureAwait(false),
                    async gdInfoPropName =>
                    {
                        var gdHostInfo = await GetJSProp<GDHostInfo>(gdInfoPropName).ConfigureAwait(false);
                        windowId = gdHostInfo.WindowId;
                        return gdHostInfo;
                    }).ConfigureAwait(false);
                Logger.Info("Initializing Glue hosted in GD");
            }
            catch (Exception e)
            {
                Logger.Info("Initializing Glue with username and app name");

                // Something went wrong probably the application is started in the browser
                var username = await GetPromptInput("user name").ConfigureAwait(false);

                var appName = await GetPromptInput("app name").ConfigureAwait(false);

                initOptions = new InitializeOptions
                {
                    AdvancedOptions = new AdvancedOptions
                    {
                        AuthenticationProvider = new GatewaySecretAuthenticationProvider(username, username)
                    },
                    // make sure application name is different for each scoped GlueProvider
                    ApplicationName = appName
                };
            }

            // choose the socket client implementation that is web assembly friendly
            initOptions.AdvancedOptions.SocketFactory = connection =>
                new ClientSocket(new Uri(initOptions.GatewayUri ?? DefaultGatewayUri), new Configuration());

            //initialize the logging factory
            initOptions.LoggerFactory = glueLoggerFactory_;
            initOptions.AppDefinition = new AppDefinition
            {
                ApplicationType = ApplicationType.Window,
                Url = uri ?? "https://missing-url"
            };

            var glue = await Glue42Base.InitializeGlue(initOptions).ConfigureAwait(false);

            if (windowId != null)
            {
                // This will be executed in hosted scenarios where we have information about the window
                MainWindow = await RegisterMainWindow(glue, windowId);

                MainWindow.ChannelContext?.Subscribe(new LambdaGlueChannelEventHandler((
                    (context, channel, updatedArgs) =>
                    {
                        //this is invoked when the channel data is updated
                        Logger.Info($"Channel was updated: {updatedArgs}");
                    }), (context, newChannel) =>
                {
                    //this is invoke when the channel is changed
                    Logger.Info($"Channel is {newChannel.Name}");
                }));
            }

            Glue42 = glue;

            glueTcs_.TrySetResult(glue);

            return glue;
        }

        private async Task<string> GetPromptInput(string promptId)
        {
            var input = string.Empty;

            while (string.IsNullOrEmpty(input) || string.IsNullOrWhiteSpace(input))
            {
                input = await jsRuntime_.InvokeAsync<string>("prompt", $"Enter your GD {promptId}");
            }

            return input;
        }

        private Task<IGlueWindow> RegisterMainWindow(IGlue42Base glue, string windowId)
        {
            // create dispatcher for the hosted window
            IGlueDispatcher dispatcher = CreateGlueDispatcher(Dispatcher.CreateDefault());

            // get a dummy window factory that is for hosted windows
            var glueWindowFactory = glue.GetWindowFactory(new HostedWindowFactoryBridge<object>(dispatcher));

            //obtain the main window
            return glueWindowFactory.RegisterStartupWindow(this, "Glazor Web Assembly",
                builder => builder.WithId(windowId).WithChannelSupport(true));
        }

        private IGlueDispatcher CreateGlueDispatcher(Dispatcher dispatcher)
        {
            return new AspNetDispatcher(dispatcher);
        }

        public async Task<T> GetJSProp<T>(string path)
        {
            //ResolveValue is exposed in wwwroot/js/gd.js
            return await jsRuntime_.InvokeAsync<T>("ResolveValue", path);
        }
    }
}