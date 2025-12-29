using EnVogue;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using EnVogue.Services;
using Blazored.LocalStorage; // Necesar pentru extensia de mai jos

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// 1. ÎNREGISTREAZÃ PACHETUL DE LOCAL STORAGE (Linia criticã lipsã)
builder.Services.AddBlazoredLocalStorage();

// 2. ÎNREGISTREAZÃ SERVICIILE TALE (Folosim AddScoped pentru toate)
builder.Services.AddScoped<WardrobeServices>();
builder.Services.AddScoped<UserState>();
builder.Services.AddScoped<NotificationService>();

await builder.Build().RunAsync();