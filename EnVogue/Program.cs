using EnVogue;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using EnVogue.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization; // Adăugat pentru AuthenticationStateProvider

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// 1. ÎNREGISTREAZĂ PACHETUL DE LOCAL STORAGE
builder.Services.AddBlazoredLocalStorage();

// 2. ÎNREGISTREAZĂ SERVICIILE TALE
builder.Services.AddScoped<WardrobeServices>();
builder.Services.AddScoped<UserState>();
builder.Services.AddScoped<NotificationService>();

// 3. CONFIGURARE AUTENTIFICARE (Rezolvă eroarea "No service for type...")
builder.Services.AddAuthorizationCore();

// Înregistrăm providerul creat de tine în folderul Services
builder.Services.AddScoped<AuthenticationStateProvider, SimpleAuthStateProvider>();

// Permite componentelor CascadingAuthenticationState să funcționeze
builder.Services.AddCascadingAuthenticationState();

await builder.Build().RunAsync();