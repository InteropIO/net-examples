using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Glue.Logging;

namespace GlazorWebAssembly
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

            builder.Services.AddScoped<IGlueLoggerFactory, GlueLoggerFactory>(serviceProvider =>
                new GlueLoggerFactory(serviceProvider.GetService<ILoggerFactory>()));

            builder.Services.AddScoped(typeof(GlueProvider));

            await builder.Build().RunAsync();
        }
    }
}
