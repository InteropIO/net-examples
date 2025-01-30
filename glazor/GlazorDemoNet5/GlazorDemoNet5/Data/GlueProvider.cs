using Glue;
using Glue.AuthenticationProviders;
using Glue.Channels;
using Glue.GDStarting;
using Glue.Transport;
using Microsoft.JSInterop;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Glue.AppManager;
using Glue.Logging;
using Glue.Windows;
using Microsoft.AspNetCore.Components;

namespace GlazorDemoNet5.Data
{
    public class GlueProvider
    {
        private const string DefaultGatewayUri = "ws://127.0.0.1:8385";
        private readonly IGlueLoggerFactory glueLoggerFactory_;

        private readonly IJSRuntime jsRuntime_;
        private readonly IGlueLog logger_;
        private TaskCompletionSource<IGlue42Base> glueTcs_;
        private long isDisposed_ = 0;

        public GlueProvider(IJSRuntime jsRuntime, IGlueLoggerFactory loggerFactory, IServiceProvider serviceProvider)
        {
            jsRuntime_ = jsRuntime;

            glueLoggerFactory_ ??= loggerFactory;
            logger_ ??= glueLoggerFactory_.GetLogger(typeof(GlueProvider));

            logger_.Info($"Initialized {nameof(GlueProvider)}");
        }

        public IGlue42Base Glue42 { get; private set; }

        public IGlueWindow MainWindow { get; private set; }

        public async Task<IGlueWindow> GetMainWindow()
        {
            await InitGlue(null).ConfigureAwait(false);

            return MainWindow;
        }

        public async Task<IGlue42Base> InitGlue(NavigationManager navman)
        {
            if (Interlocked.CompareExchange(ref glueTcs_,
                    new TaskCompletionSource<IGlue42Base>(TaskCreationOptions.RunContinuationsAsynchronously), null) is
                { } tcs)
            {
                //already initialized
                return await tcs.Task.ConfigureAwait(false);
            }

            logger_.Info("Initializing Glue");

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
                logger_.Info("Initializing Glue hosted in GD");
            }
            catch (Exception e)
            {
                logger_.Info("Initializing Glue with username and app name", e);

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
            initOptions.LoggerFactory = DebugLoggerFactory.Instance;
            initOptions.AppDefinition = new AppDefinition
            {
                ApplicationType = ApplicationType.Window,
                Url = navman?.Uri ?? "https://glazor-url-not-setup-correctly"
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
                        logger_.Info($"Channel was updated: {updatedArgs}");
                    }), (context, newChannel) =>
                    {
                        //this is invoked when the channel is changed
                        logger_.Info($"Channel is {newChannel.Name}");
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

        private async Task<IGlueWindow> RegisterMainWindow(IGlue42Base glue, string windowId)
        {
            // create dispatcher for the hosted window
            IGlueDispatcher dispatcher = CreateGlueDispatcher(Dispatcher.CreateDefault());

            // get a dummy window factory that is for hosted windows
            var glueWindowFactory = await glue.GetWindowFactory(new HostedWindowFactoryBridge<object>(dispatcher)).ConfigureAwait(false);

            //obtain the main window
            return await glueWindowFactory.RegisterStartupWindow(this, "Glazor Web Assembly",
                builder => builder.WithId(windowId).WithChannelSupport(true)).ConfigureAwait(false);
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

        protected virtual async Task Dispose(bool dispose)
        {
            if (Interlocked.CompareExchange(ref isDisposed_, 1, 0) != 0 || !dispose)
            {
                return;
            }

            // this service is being disposed
            if (Glue42 != null)
            {
                await Glue42.Interop.Peer.DisposeAsync().ConfigureAwait(false);
            }
        }

        // this gets called when the blazor component is removed from the page e.g when you refresh the page
        public async ValueTask DisposeAsync()
        {
            await Dispose(true).ConfigureAwait(false);
            GC.SuppressFinalize(this);
        }

        public async void Dispose()
        {
            await Dispose(true).ConfigureAwait(false);
            GC.SuppressFinalize(this);
        }

        ~GlueProvider()
        {
        }
    }
}
