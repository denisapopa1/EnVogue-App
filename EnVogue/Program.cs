using EnVogue;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using EnVogue.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Headers;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// --- MODIFICARE AICI ---
// Schimbăm BaseAddress de la adresa locală a browserului la adresa fixă a API-ului tău
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("https://localhost:7081/")
});

// 1. ÎNREGISTREAZĂ PACHETUL DE LOCAL STORAGE
builder.Services.AddBlazoredLocalStorage();

// 2. ÎNREGISTREAZĂ SERVICIILE TALE
builder.Services.AddScoped<WardrobeServices>();
builder.Services.AddScoped<UserState>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<ProfilePhotoService>();
builder.Services.AddScoped<AccountProfileService>();

// 3. CONFIGURARE AUTENTIFICARE
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, SimpleAuthStateProvider>();
builder.Services.AddCascadingAuthenticationState();

await builder.Build().RunAsync();
