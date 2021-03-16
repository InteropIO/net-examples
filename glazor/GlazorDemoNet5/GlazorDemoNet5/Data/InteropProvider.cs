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
using System.Threading;
using System.Threading.Tasks;

namespace GlazorDemoNet5.Data
{
    public class InteropProvider
    {
        private readonly IJSRuntime jsRuntime_;
        private static TaskCompletionSource<IGlueInterop> interopTcs_;
        private static TaskCompletionSource<IGlueChannels> channelsTcs_;


        public InteropProvider(IJSRuntime jsRuntime)
        {
            jsRuntime_ = jsRuntime;
        }


        public static IGlueInterop Interop { get; private set; }
        public static IGlueChannels Channels { get; private set; }

        public async Task<IGlueChannels> InitChannels()
        {
            if (Interlocked.CompareExchange(ref channelsTcs_, new TaskCompletionSource<IGlueChannels>(TaskCreationOptions.RunContinuationsAsynchronously), null) is { } tcs)
            {
                return await tcs.Task.ConfigureAwait(false);
            }
            var channels = new GlueChannels(await InitInterop().ConfigureAwait(false));

            channelsTcs_.TrySetResult(channels);

            return channels;
        }

        public async Task<IGlueInterop> InitInterop()
        {
            if (Interlocked.CompareExchange(ref interopTcs_, new TaskCompletionSource<IGlueInterop>(TaskCreationOptions.RunContinuationsAsynchronously), null) is { } tcs)
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
            gdInfo = gdInfo ?? new GDHostInfo { ApplicationName = "Glazor .NET 5", User = Environment.UserName };

            var interop = await GlueInterop.InitInterop(gdInfo, gwToken).ConfigureAwait(false);
            interopTcs_.TrySetResult(interop);
            return interop;
        }

        public async Task<T> GetJSProp<T>(string path)
        {
            return await jsRuntime_.InvokeAsync<T>("ResolveValue", path);
        }
    }
}
