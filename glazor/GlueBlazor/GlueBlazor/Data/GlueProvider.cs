using GlueForNetCore;
using GlueForNetCore.AuthenticationProviders;
using GlueForNetCore.GDStarting;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace GlueBlazor.Data
{
    public class GlueProvider
    {
        private readonly IJSRuntime jsRuntime_;
        public GlueProvider(IJSRuntime jsRuntime)
        {
            jsRuntime_ = jsRuntime;
        }

        public static Glue42 Glue { get; private set; }

        public async Task<Glue42> InitGlue()
        {
            if (Glue != null)
            {
                return Glue;
            }

            var options = new InitializeOptions();

            try
            {
                // U can use this sugar method or fill the initialize options yourself with the token and gd info
                options = await Glue42.GetHostedGDOptions(async tokenName => await jsRuntime_.InvokeAsync<string>(tokenName, new object[0]), async gd => await GetJSProp<GDHostInfo>(gd).ConfigureAwait(false));
            }
            catch (Exception e)
            {
            }

            var glue = await Glue42.InitializeGlue(options).ConfigureAwait(false);

            Glue = glue;

            return glue;
        }

        public async Task<T> GetJSProp<T>(string path)
        {
            return await jsRuntime_.InvokeAsync<T>("ResolveValue", path);
        }
    }

}
