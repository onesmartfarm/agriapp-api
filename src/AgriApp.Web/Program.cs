using System.Globalization;
using AgriApp.Web.Security;
using AgriApp.Web.Services;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<AgriApp.Web.App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ── Configuration ────────────────────────────────────────────────────────────
var apiBase = builder.Configuration["ApiBaseUrl"] ?? builder.HostEnvironment.BaseAddress;

// ── Core services ────────────────────────────────────────────────────────────
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddMudServices();
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.AddAuthorizationCore();

// ── Auth state provider ──────────────────────────────────────────────────────
builder.Services.AddScoped<JwtAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(
    sp => sp.GetRequiredService<JwtAuthenticationStateProvider>());

// ── HTTP message handler (attaches Bearer token) ─────────────────────────────
builder.Services.AddTransient<JwtAuthorizationMessageHandler>();

// ── Named HttpClient for API calls ───────────────────────────────────────────
builder.Services.AddHttpClient("AgriApi", client =>
    client.BaseAddress = new Uri(apiBase))
    .AddHttpMessageHandler<JwtAuthorizationMessageHandler>();

// ── Typed service registrations ──────────────────────────────────────────────
builder.Services.AddScoped<IAuthService>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    var authProvider = sp.GetRequiredService<JwtAuthenticationStateProvider>();
    return new AuthService(factory.CreateClient("AgriApi"), authProvider);
});

builder.Services.AddScoped<IWorkOrderService>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    return new WorkOrderService(factory.CreateClient("AgriApi"));
});

builder.Services.AddScoped<IAttendanceService>(sp =>
{
    var factory = sp.GetRequiredService<IHttpClientFactory>();
    return new AttendanceService(factory.CreateClient("AgriApi"));
});

// ── Localization default culture ─────────────────────────────────────────────
CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

await builder.Build().RunAsync();
