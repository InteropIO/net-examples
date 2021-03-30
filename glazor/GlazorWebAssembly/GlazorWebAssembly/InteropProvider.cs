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
    public class InteropProvider
    {
        private static TaskCompletionSource<IGlueInterop> interopTcs_;
        private static TaskCompletionSource<IGlueChannels> channelsTcs_;

        private static IGlueLoggerFactory glueLoggerFactory_;

        private readonly IJSRuntime jsRuntime_;
        private readonly ILoggerFactory loggerFactory_;


        public InteropProvider(IJSRuntime jsRuntime, ILoggerFactory loggerFactory)
        {
            jsRuntime_ = jsRuntime;
            loggerFactory_ = loggerFactory;
            if (glueLoggerFactory_ == null)
            {
                glueLoggerFactory_ = new GlueLoggerFactory(loggerFactory);
            }

            glueLoggerFactory_.GetLogger(typeof(InteropProvider))
                .Info($"{nameof(Environment.UserName)} is {Environment.UserName}");
            //log4net.Config.XmlConfigurator.Configure(Assembly.GetExecutingAssembly().GetManifestResourceStream("GlazorWebAssembly.log4net.cfg"));
        }


        public static IGlueInterop Interop { get; private set; }
        public static IGlueChannels Channels { get; private set; }

        public async Task<IGlueChannels> InitChannels()
        {
            if (Interlocked.CompareExchange(ref channelsTcs_,
                    new TaskCompletionSource<IGlueChannels>(TaskCreationOptions.RunContinuationsAsynchronously),
                    null) is
                { } tcs)
            {
                return await tcs.Task.ConfigureAwait(false);
            }

            var channels = new GlueChannels(await InitInterop().ConfigureAwait(false));

            channelsTcs_.TrySetResult(channels);

            return channels;
        }

        public async Task<IGlueInterop> InitInterop()
        {
            if (Interlocked.CompareExchange(ref interopTcs_,
                    new TaskCompletionSource<IGlueInterop>(TaskCreationOptions.RunContinuationsAsynchronously), null) is
                { } tcs)
            {
                return await tcs.Task.ConfigureAwait(false);
            }

            GDHostInfo gdInfo = null;
            string gwToken = null;

            try
            {
                gdInfo = await GetJSProp<GDHostInfo>("glue42gd").ConfigureAwait(false);
                gwToken = await jsRuntime_.InvokeAsync<string>("glue42gd.getGWToken").ConfigureAwait(false);
            }
            catch (Exception e)
            {
            }

            //In case of external hosting envrionment (browser), the user should match the username used for GD
            //gdInfo = gdInfo ?? new GDHostInfo { ApplicationName = "Glazor .NET 5", User = Environment.UserName };

            var protocolSerializer =
                new GwProtocolSerializer(glueLoggerFactory_.GetLogger(typeof(GwProtocolSerializer)));

            var identity = protocolSerializer.SerializeRemoteId(new Instance(true, true));

            identity[InstanceSchema.Instance.ApplicationName] = "GlueAssembly";

            identity[InstanceSchema.Instance.InstanceId] = Guid.NewGuid().ToString();

            identity[InstanceSchema.Instance.UserName] = "ttomov";

            var authProvider = new GatewaySecretAuthenticationProvider("ttomov", "ttomov");

            var interop = await GlueInterop.InitInterop(
                new GlueConnectionBase<GwMessage>.BusConfiguration
                {
                    GatewayUri = "ws://127.0.0.1:8385",
                    SocketFactory = c => new ClientSocket(new Uri("ws://127.0.0.1:8385"), new Configuration())
                }, authProvider, protocolSerializer, identity,
                glueLoggerFactory_).ConfigureAwait(false);
            interopTcs_.TrySetResult(interop);
            return interop;
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