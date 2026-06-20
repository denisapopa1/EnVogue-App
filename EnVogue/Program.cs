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

var frontendAddress = new Uri(builder.HostEnvironment.BaseAddress);
var frontendHost = frontendAddress.Host;
var apiHost = string.Equals(frontendHost, "localhost", StringComparison.OrdinalIgnoreCase)
    || string.Equals(frontendHost, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
    || string.Equals(frontendHost, "0.0.0.0", StringComparison.OrdinalIgnoreCase)
        ? "localhost"
        : frontendHost;

ApiEndpoints.BaseAddress = new Uri($"http://{apiHost}:5203/");

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = ApiEndpoints.BaseAddress
});

builder.Services.AddBlazoredLocalStorage();

builder.Services.AddScoped<WardrobeServices>();
builder.Services.AddScoped<DatasetImportService>();
builder.Services.AddScoped<UserState>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<ProfilePhotoService>();
builder.Services.AddScoped<AccountProfileService>();

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, SimpleAuthStateProvider>();
builder.Services.AddCascadingAuthenticationState();

await builder.Build().RunAsync();
