using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using PensionPilot;
using PensionPilot.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<IConfigService, ConfigService>();
builder.Services.AddScoped<ITaxService, TaxService>();
builder.Services.AddScoped<ICalculatorService, CalculatorService>();
builder.Services.AddBlazorBootstrap();

await builder.Build().RunAsync();
