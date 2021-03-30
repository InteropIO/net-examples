using System;
using System.Threading;
using System.Threading.Tasks;
using Glue;
using Glue.AuthenticationProviders;
using Glue.Channels;
using Glue.GDStarting;
using Glue.Logging;
using Glue.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace GlazorWebAssembly
{
    public class GlueProvider
    {
        private static TaskCompletionSource<IGlue42Base> glueTcs_;
        private static IGlueLoggerFactory glueLoggerFactory_;

        private readonly IJSRuntime jsRuntime_;
        private readonly ILoggerFactory loggerFactory_;


        public GlueProvider(IJSRuntime jsRuntime, ILoggerFactory loggerFactory)
        {
            jsRuntime_ = jsRuntime;
            loggerFactory_ = loggerFactory;

            glueLoggerFactory_ ??= new GlueLoggerFactory(loggerFactory);
        }

        public IGlue42Base Glue42 { get; private set; }

        public async Task<IGlue42Base> InitGlue()
        {
            if (Interlocked.CompareExchange(ref glueTcs_, new TaskCompletionSource<IGlue42Base>(TaskCreationOptions.RunContinuationsAsynchronously), null) is { } tcs)
            {
                return await tcs.Task.ConfigureAwait(false);
            }

            InitializeOptions initOptions;

            try
            {
                initOptions = await Glue42Base.GetHostedGDOptions(async tokenName => await jsRuntime_.InvokeAsync<string>(tokenName).ConfigureAwait(false), async gdInfoPropName => await GetJSProp<GDHostInfo>(gdInfoPropName).ConfigureAwait(false)).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                //This username should match the GD username
                var username = "runningGDInstanceUsername";
                initOptions = new InitializeOptions() { AdvancedOptions = new AdvancedOptions()
                {
                    AuthenticationProvider = new GatewaySecretAuthenticationProvider(username, username)
                }, ApplicationName = "GlazorWebAssembly"};
            }

            initOptions.AdvancedOptions.SocketFactory = connection =>
                new ClientSocket(new Uri("ws://127.0.0.1:8385"), new Configuration());

            initOptions.LoggerFactory = glueLoggerFactory_;

            var glue = await Glue42Base.InitializeGlue(initOptions).ConfigureAwait(false);

            Glue42 = glue;

            return glue;
        }

        public async Task<T> GetJSProp<T>(string path)
        {
            //ResolveValue is exposed in wwwroot/js/gd.js
            return await jsRuntime_.InvokeAsync<T>("ResolveValue", path);
        }

        class GlueLoggerFactory : IGlueLoggerFactory
        {
            private readonly ILoggerFactory loggerFactory_;

            public GlueLoggerFactory(ILoggerFactory loggerFactory)
            {
                loggerFactory_ = loggerFactory;
            }

            public IGlueLog GetLogger(string name)
            {
                return new GlueLogger(loggerFactory_.CreateLogger(name));
            }

            public IGlueLog GetLogger(Type type)
            {
                return new GlueLogger(loggerFactory_.CreateLogger(type));
            }
        }
    }

    internal class GlueLogger : IGlueLog
    {
        private readonly ILogger logger_;

        public GlueLogger(ILogger logger)
        {
            logger_ = logger;
        }

        public void Info(string message, Exception e = null)
        {
            Log(GlueLogLevel.Info, message, e);
        }

        public void Error(string message, Exception e = null)
        {
            Log(GlueLogLevel.Info, message, e);
        }

        public void Debug(string message, Exception e = null)
        {
            Log(GlueLogLevel.Info, message, e);
        }

        public void Debug(Func<string> message, Exception e = null)
        {
            Log(GlueLogLevel.Info, message(), e);
        }

        public void Trace(Func<string> message, Exception e = null)
        {
            Log(GlueLogLevel.Info, message(), e);
        }

        public bool IsEnabledFor(GlueLogLevel level)
        {
            return logger_.IsEnabled(GetLogLevel(level));
        }

        public void Warn(string message, Exception e = null)
        {
            Log(GlueLogLevel.Info, message, e);
        }

        public void Log(GlueLogLevel level, string message, Exception exception)
        {
            logger_.Log(GetLogLevel(level), exception, message);
        }

        private LogLevel GetLogLevel(GlueLogLevel level)
        {
            return level switch
            {
                GlueLogLevel.Trace => LogLevel.Trace,
                GlueLogLevel.Debug => LogLevel.Debug,
                GlueLogLevel.Info => LogLevel.Information,
                GlueLogLevel.Warn => LogLevel.Warning,
                GlueLogLevel.Error => LogLevel.Error,
                GlueLogLevel.Fatal => LogLevel.Critical,
                GlueLogLevel.Off => LogLevel.None,
                _ => LogLevel.Information
            };
        }
    }
}