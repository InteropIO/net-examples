using Glue;
using Glue.AuthenticationProviders;
using Glue.Channels;
using Glue.GDStarting;
using Glue.Transport;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace GlazorDemoNet5.Data
{
    public class InteropProvider
    {
        private readonly IJSRuntime jsRuntime_;

        public InteropProvider(IJSRuntime jsRuntime)
        {
            jsRuntime_ = jsRuntime;
        }

        public static IGlueInterop Interop { get; private set; }

        public static IGlueChannels Channels { get; private set; }

        public async Task<IGlueChannels> InitChannels()
        {
            if (Channels != null)
            {
                return Channels;
            }

            if (Interop == null)
            {
                await InitInterop().ConfigureAwait(false);
            }

            return Channels = new GlueChannels(Interop);
        }

        public async Task<IGlueInterop> InitInterop()
        {
            if (Interop != null)
            {
                return Interop;
            }

            IAuthenticationProvider authenticationProvider = null;
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

            gdInfo = gdInfo ?? new GDHostInfo { GwUrl = "ws://127.0.0.1:8385/", ApplicationName = "Glazor .NET 5"};

            var username = Environment.UserName;

            authenticationProvider = gwToken == null ? new LambdaAuthenticationProvider(_ => new AuthenticationDetails(new Dictionary<string, object>()
            {
                {"login", username},
                {"secret", username},
                {"method", "secret"}
            }, null)) : new GatewayTokenAuthenticationProvider(gwToken);

            var interop = await GlueInterop.InitInterop(gdInfo.GwUrl, gdInfo.ApplicationName, gdInfo.WindowId, authenticationProvider, new GwProtocolSerializer()).ConfigureAwait(false);

            Interop = interop;

            return interop;
        }

        public async Task<T> GetJSProp<T>(string path)
        {
            return await jsRuntime_.InvokeAsync<T>("ResolveValue", path);
        }
    }
}
