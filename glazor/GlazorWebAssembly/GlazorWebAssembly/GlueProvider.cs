using System;
using System.Threading;
using System.Threading.Tasks;
using Glue;
using Glue.AppManager;
using Glue.AuthenticationProviders;
using Glue.Channels;
using Glue.GDStarting;
using Glue.InteropContract;
using Glue.Logging;
using Glue.Transport;
using Glue.Windows;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace GlazorWebAssembly
{
    public class GlueProvider
    {
        private static TaskCompletionSource<IGlue42Base> glueTcs_;
        private static IGlueLoggerFactory glueLoggerFactory_;
        private static IGlueLog logger_;
        private const string defaultGatewayUri = "ws://127.0.0.1:8385";

        private readonly IJSRuntime jsRuntime_;

        public GlueProvider(IJSRuntime jsRuntime, IGlueLoggerFactory loggerFactory)
        {
            jsRuntime_ = jsRuntime;

            glueLoggerFactory_ ??= loggerFactory;
            logger_ ??= glueLoggerFactory_.GetLogger(typeof(GlueProvider));
        }
        public IGlue42Base Glue42 { get; private set; }

        public static IGlueWindow MainWindow { get; private set; }

        public async Task<IGlueWindow> GetMainWindow()
        {
            await InitGlue().ConfigureAwait(false);

            return MainWindow;
        }

        public async Task<IGlue42Base> InitGlue()
        {
            if (Interlocked.CompareExchange(ref glueTcs_, new TaskCompletionSource<IGlue42Base>(TaskCreationOptions.RunContinuationsAsynchronously), null) is { } tcs)
            {
                //already initialized
                return await tcs.Task.ConfigureAwait(false);
            }

            InitializeOptions initOptions;

            string windowId = null;

            try
            {
                // try to get the GD hosted settings if this is started as a GD application (in order to do this u have to configure your own application in %localappdata%\tick42\gluedesktop\config\apps by adding a .json file for it
                initOptions = await Glue42Base.GetHostedGDOptions(async tokenName => await jsRuntime_.InvokeAsync<string>(tokenName).ConfigureAwait(false), async gdInfoPropName =>
                {
                    var gdHostInfo = await GetJSProp<GDHostInfo>(gdInfoPropName).ConfigureAwait(false);
                    windowId = gdHostInfo.WindowId;
                    return gdHostInfo;
                }).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                // Something went wrong probably the application is started in the browser
                var username = await GetUsername().ConfigureAwait(false);

                initOptions = new InitializeOptions() { AdvancedOptions = new AdvancedOptions()
                {
                    AuthenticationProvider = new GatewaySecretAuthenticationProvider(username, username)
                }, ApplicationName = "GlazorWebAssembly"};
            }

            // choose the socket client implementation that is web assembly friendly
            initOptions.AdvancedOptions.SocketFactory = connection =>
                new ClientSocket(new Uri(initOptions.GatewayUri ?? defaultGatewayUri), new Configuration());

            //initialize the logging factory
            initOptions.LoggerFactory = glueLoggerFactory_;
            initOptions.AppType = "window";

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
                        //this is invoke when the channel is changed
                        logger_.Info($"Channel is {newChannel.Name}");
                    }));
            }

            Glue42 = glue;

            glueTcs_.TrySetResult(glue);
           
            return glue;
        }

        private async Task<string> GetUsername()
        {
            var username = string.Empty;

            while (string.IsNullOrEmpty(username) || string.IsNullOrWhiteSpace(username))
            {
                username = await jsRuntime_.InvokeAsync<string>("prompt", "Enter your GD username");
            }

            return username;
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