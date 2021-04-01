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
        private const string defaultGatewayUri = "ws://127.0.0.1:8385";

        private readonly IJSRuntime jsRuntime_;

        public GlueProvider(IJSRuntime jsRuntime, IGlueLoggerFactory loggerFactory)
        {
            jsRuntime_ = jsRuntime;

            glueLoggerFactory_ ??= loggerFactory;
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
                return await tcs.Task.ConfigureAwait(false);
            }

            InitializeOptions initOptions;

            string windowId = null;

            try
            {
                initOptions = await Glue42Base.GetHostedGDOptions(async tokenName => await jsRuntime_.InvokeAsync<string>(tokenName).ConfigureAwait(false), async gdInfoPropName =>
                {
                    var gdHostInfo = await GetJSProp<GDHostInfo>(gdInfoPropName).ConfigureAwait(false);
                    windowId = gdHostInfo.WindowId;
                    return gdHostInfo;
                }).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                var username = await GetUsername().ConfigureAwait(false);

                initOptions = new InitializeOptions() { AdvancedOptions = new AdvancedOptions()
                {
                    AuthenticationProvider = new GatewaySecretAuthenticationProvider(username, username)
                }, ApplicationName = "GlazorWebAssembly"};
            }

            initOptions.AdvancedOptions.SocketFactory = connection =>
                new ClientSocket(new Uri(initOptions.GatewayUri ?? defaultGatewayUri), new Configuration());

            initOptions.LoggerFactory = glueLoggerFactory_;
            initOptions.AppType = "window";

            var glue = await Glue42Base.InitializeGlue(initOptions).ConfigureAwait(false);

            IGlueDispatcher dispatcher = CreateGlueDispatcher(Dispatcher.CreateDefault());

            if (windowId != null)
            {
                MainWindow = await RegisterMainWindow(glue, dispatcher, windowId);

                MainWindow.ChannelContext?.Subscribe(new LambdaGlueChannelEventHandler((
                    (context, channel, updatedArgs) => { }), (context, newChannel) => { }));
            }

            Glue42 = glue;

            glueTcs_.TrySetResult(glue);
           
            return glue;
        }

        private async Task<string> GetUsername()
        {
            string username = string.Empty;

            while (string.IsNullOrEmpty(username) || string.IsNullOrWhiteSpace(username))
            {
                username = await jsRuntime_.InvokeAsync<string>("prompt", "Enter your GD username");
            }

            return username;
        }

        private Task<IGlueWindow> RegisterMainWindow(IGlue42Base glue, IGlueDispatcher dispatcher, string windowId)
        {
            var glueWindowFactory = glue.GetWindowFactory(new HostedWindowFactoryBridge<object>(dispatcher));

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