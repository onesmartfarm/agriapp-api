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

// ── Helper to get named HttpClient ───────────────────────────────────────────
static HttpClient Api(IServiceProvider sp) =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("AgriApi");

// ── Typed service registrations ──────────────────────────────────────────────
builder.Services.AddScoped<IAuthService>(sp =>
    new AuthService(Api(sp), sp.GetRequiredService<JwtAuthenticationStateProvider>()));

builder.Services.AddScoped<IWorkOrderService>(sp =>
    new WorkOrderService(Api(sp), sp.GetRequiredService<ILogger<WorkOrderService>>()));

builder.Services.AddScoped<IAttendanceService>(sp =>
    new AttendanceService(Api(sp)));

builder.Services.AddScoped<IEquipmentService>(sp =>
    new EquipmentService(Api(sp), sp.GetRequiredService<ILogger<EquipmentService>>()));

builder.Services.AddScoped<IInquiryService>(sp =>
    new InquiryService(Api(sp), sp.GetRequiredService<ILogger<InquiryService>>()));

builder.Services.AddScoped<IInvoiceService>(sp =>
    new InvoiceService(Api(sp), sp.GetRequiredService<ILogger<InvoiceService>>()));

builder.Services.AddScoped<IPaymentService>(sp =>
    new PaymentService(Api(sp), sp.GetRequiredService<ILogger<PaymentService>>()));

builder.Services.AddScoped<IUserService>(sp =>
    new UserService(Api(sp), sp.GetRequiredService<ILogger<UserService>>()));

builder.Services.AddScoped<ICustomerService>(sp =>
    new CustomerService(Api(sp), sp.GetRequiredService<ILogger<CustomerService>>()));

builder.Services.AddScoped<IVendorService>(sp =>
    new VendorService(Api(sp), sp.GetRequiredService<ILogger<VendorService>>()));

builder.Services.AddScoped<ICenterService>(sp =>
    new CenterService(Api(sp), sp.GetRequiredService<ILogger<CenterService>>()));

// ── Localization default culture ─────────────────────────────────────────────
CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("en-US");

await builder.Build().RunAsync();
