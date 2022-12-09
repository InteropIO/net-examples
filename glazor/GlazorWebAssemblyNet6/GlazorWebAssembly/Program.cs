using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using GlazorWebAssembly6;
using Glue.Logging;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddScoped<IGlueLoggerFactory, GlueLoggerFactory>(serviceProvider =>
    new GlueLoggerFactory(serviceProvider.GetService<ILoggerFactory>()!));

builder.Services.AddScoped(typeof(GlueProvider));

await builder.Build().RunAsync();
