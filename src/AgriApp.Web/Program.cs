using AgriApp.Web.Auth;
using AgriApp.Web.HttpServices;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<AgriApp.Web.App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ── Configuration ──────────────────────────────────────────────────────────
var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
    ?? builder.HostEnvironment.BaseAddress;

// ── Core Services ───────────────────────────────────────────────────────────
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddMudServices();

// ── Auth ────────────────────────────────────────────────────────────────────
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<JwtAuthenticationStateProvider>());
builder.Services.AddAuthorizationCore();

// ── HTTP Handler ─────────────────────────────────────────────────────────────
builder.Services.AddTransient<JwtAuthorizationMessageHandler>();

// ── Named HttpClient (public — login, no Bearer needed) ───────────────────
builder.Services.AddHttpClient("PublicApi", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
});

// ── Named HttpClient (secured — Bearer attached via handler) ────────────────
builder.Services.AddHttpClient("SecuredApi", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
})
.AddHttpMessageHandler<JwtAuthorizationMessageHandler>();

// ── Application Services ─────────────────────────────────────────────────────
// AuthService uses the PUBLIC client (no token needed for login)
builder.Services.AddScoped<IAuthService>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var http = factory.CreateClient("PublicApi");
    var authProvider = sp.GetRequiredService<JwtAuthenticationStateProvider>();
    return new AuthService(http, authProvider);
});

// WorkOrderService and AttendanceService use the SECURED client
builder.Services.AddScoped<IWorkOrderService>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var http = factory.CreateClient("SecuredApi");
    return new WorkOrderService(http);
});

builder.Services.AddScoped<IAttendanceService>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var http = factory.CreateClient("SecuredApi");
    return new AttendanceService(http);
});

await builder.Build().RunAsync();
