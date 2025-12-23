using EnVogue;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using EnVogue.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Înregistreazã doar serviciile tale de business
builder.Services.AddSingleton<WardrobeServices>();
builder.Services.AddSingleton<UserState>();

await builder.Build().RunAsync();