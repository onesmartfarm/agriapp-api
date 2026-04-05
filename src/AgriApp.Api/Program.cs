using AgriApp.Api.Middleware;
using AgriApp.Application.Services;
using AgriApp.Core.Entities;
using AgriApp.Core.Enums;
using AgriApp.Core.Interfaces;
using AgriApp.Infrastructure.Data;
using AgriApp.Infrastructure.Interceptors;
using AgriApp.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
DotNetEnv.Env.Load();

var rawConnectionString = Environment.GetEnvironmentVariable("DATABASE_URL")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("DATABASE_URL or ConnectionStrings:DefaultConnection must be set.");

var connectionString = ConvertToNpgsqlConnectionString(rawConnectionString);

static string ConvertToNpgsqlConnectionString(string url)
{
    if (!url.StartsWith("postgres://") && !url.StartsWith("postgresql://"))
        return url;

    var uri = new Uri(url);
    var userInfo = uri.UserInfo.Split(':');
    var host = uri.Host;
    var port = uri.Port > 0 ? uri.Port : 5432;
    var database = uri.AbsolutePath.TrimStart('/');
    if (userInfo.Length > 1)
    {
        string rawPassword = userInfo[1];
        string cleanPassword = Uri.UnescapeDataString(rawPassword);

        var username = userInfo.Length > 0 ? userInfo[0] : "";
        //var password = userInfo.Length > 1 ? userInfo[1] : "";

        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        var sslMode = query["sslmode"] ?? "Prefer";

        return $"Host={host};Port={port};Database={database};Username={username};Password={cleanPassword};SSL Mode={sslMode}";
    }

    return $"Host={host};Port={port};Database={database}";
}

var jwtKey = Environment.GetEnvironmentVariable("SESSION_SECRET")
    ?? builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT key not configured. Set SESSION_SECRET env var or Jwt:Key in config.");

builder.Configuration["Jwt:Key"] = jwtKey;
builder.Configuration["Jwt:Issuer"] ??= "AgriApp";
builder.Configuration["Jwt:Audience"] ??= "AgriApp";

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<AuditInterceptor>();

builder.Services.AddDbContext<AgriDbContext>((serviceProvider, options) =>
{
    var interceptor = serviceProvider.GetRequiredService<AuditInterceptor>();
    options.UseNpgsql(connectionString)
           .AddInterceptors(interceptor);
});

builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<EquipmentRepository>();
builder.Services.AddScoped<InquiryRepository>();
builder.Services.AddScoped<WorkOrderRepository>();
builder.Services.AddScoped<CustomerRepository>();
builder.Services.AddScoped<VendorRepository>();

builder.Services.AddScoped<EquipmentService>();
builder.Services.AddScoped<InquiryService>();
builder.Services.AddScoped<WorkOrderService>();
builder.Services.AddScoped<CommissionRealizationService>();
builder.Services.AddScoped<PayrollService>();
builder.Services.AddScoped<CalendarService>();
builder.Services.AddScoped<InvoiceService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<VendorService>();
builder.Services.AddScoped<ServiceActivityService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AgriApp - Agricultural Equipment Rental & Maintenance API",
        Version = "v1",
        Description = "C# .NET 8 Clean Architecture API for managing agricultural equipment rentals, maintenance work orders, and customer inquiries. Uses EF Core with PostgreSQL, Global Query Filters for CenterId isolation, and audit trail interceptor."
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Enter: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AgriDbContext>();
    await db.Database.MigrateAsync();
}

app.UseCors();
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "AgriApp API v1"));
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await SeedDatabase(app);

app.Run();

static async Task SeedDatabase(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AgriDbContext>();

    // Idempotent: existing DBs that already had centers (e.g. older seed) still get vendors/customers/equipment/users.
    var sangola = await EnsureCenterAsync(db,
        "Sangola Agri-Center",
        "Sangola, Maharashtra, India",
        "₹",
        "India Standard Time");
    var edison = await EnsureCenterAsync(db,
        "Edison NJ Hub",
        "Edison, NJ, USA",
        "$",
        "Eastern Standard Time");
    await db.SaveChangesAsync();

    await EnsureVendorAsync(db, "John Deere Sangola", sangola.Id);
    await EnsureVendorAsync(db, "Mazda Financial Services (Edison)", edison.Id);
    await db.SaveChangesAsync();

    await EnsureCustomerAsync(db, "Bafna Farms (Pune)", sangola.Id);
    await EnsureCustomerAsync(db, "Edison Property Management", edison.Id);
    await db.SaveChangesAsync();

    await EnsureServiceActivityAsync(db, "Rotavation", "Field rotavation service", 800m, sangola.Id);
    await EnsureServiceActivityAsync(db, "Cultivation", "Soil cultivation service", 700m, sangola.Id);
    await EnsureServiceActivityAsync(db, "Sowing", "Sowing service", 900m, sangola.Id);
    await db.SaveChangesAsync();

    var deereVendorId = (await db.Vendors.IgnoreQueryFilters().AsNoTracking()
        .FirstAsync(v => v.Name == "John Deere Sangola" && v.CenterId == sangola.Id)).Id;
    var mazdaVendorId = (await db.Vendors.IgnoreQueryFilters().AsNoTracking()
        .FirstAsync(v => v.Name == "Mazda Financial Services (Edison)" && v.CenterId == edison.Id)).Id;

    await EnsureEquipmentAsync(db, "John Deere 5405 Tractor", EquipmentCategory.Tractor, 1500.00m,
        sangola.Id, deereVendorId, isImplement: false);
    await EnsureEquipmentAsync(db, "Heavy Rotavator (7 ft)", EquipmentCategory.Tractor, 400.00m,
        sangola.Id, deereVendorId, isImplement: true);
    await EnsureEquipmentAsync(db, "2025 Mazda CX-90 PHEV", EquipmentCategory.Vehicle, 85.00m,
        edison.Id, mazdaVendorId, isImplement: false);
    await db.SaveChangesAsync();

    await EnsureDemoUsersAsync(db, sangola.Id, edison.Id);
    await db.SaveChangesAsync();
}

static async Task<Center> EnsureCenterAsync(
    AgriDbContext db,
    string name,
    string location,
    string currencySymbol,
    string timeZoneId)
{
    var existing = await db.Centers.FirstOrDefaultAsync(c => c.Name == name);
    if (existing != null)
        return existing;

    var center = new Center
    {
        Name = name,
        Location = location,
        CurrencySymbol = currencySymbol,
        TimeZoneId = timeZoneId,
        CreatedAt = DateTime.UtcNow
    };
    db.Centers.Add(center);
    return center;
}

static async Task EnsureVendorAsync(AgriDbContext db, string name, int centerId)
{
    if (await db.Vendors.IgnoreQueryFilters().AnyAsync(v => v.Name == name && v.CenterId == centerId))
        return;

    db.Vendors.Add(new Vendor
    {
        Name = name,
        CenterId = centerId,
        CreatedAt = DateTime.UtcNow
    });
}

static async Task EnsureCustomerAsync(AgriDbContext db, string name, int centerId)
{
    if (await db.Customers.IgnoreQueryFilters().AnyAsync(c => c.Name == name && c.CenterId == centerId))
        return;

    db.Customers.Add(new Customer
    {
        Name = name,
        CenterId = centerId,
        CreatedAt = DateTime.UtcNow
    });
}

static async Task EnsureEquipmentAsync(
    AgriDbContext db,
    string name,
    EquipmentCategory category,
    decimal hourlyRate,
    int centerId,
    int vendorId,
    bool isImplement = false)
{
    if (await db.Equipment.IgnoreQueryFilters().AnyAsync(e => e.Name == name && e.CenterId == centerId))
        return;

    db.Equipment.Add(new Equipment
    {
        Name = name,
        Category = category,
        HourlyRate = hourlyRate,
        CenterId = centerId,
        VendorId = vendorId,
        IsImplement = isImplement,
        CreatedAt = DateTime.UtcNow
    });
}

static async Task EnsureServiceActivityAsync(
    AgriDbContext db,
    string name,
    string description,
    decimal baseRatePerHour,
    int centerId)
{
    if (await db.ServiceActivities.IgnoreQueryFilters()
            .AnyAsync(a => a.Name == name && a.CenterId == centerId))
        return;

    db.ServiceActivities.Add(new ServiceActivity
    {
        Name = name,
        Description = description,
        BaseRatePerHour = baseRatePerHour,
        CenterId = centerId,
        CreatedAt = DateTime.UtcNow
    });
}

static async Task EnsureDemoUsersAsync(AgriDbContext db, int sangolaId, int edisonId)
{
    async Task AddIfMissingAsync(string email, string name, Role role, int? centerId, string password)
    {
        if (await db.Users.AnyAsync(u => u.Email == email))
            return;
        db.Users.Add(new User
        {
            Name = name,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = role,
            CenterId = centerId,
            CreatedAt = DateTime.UtcNow
        });
    }

    await AddIfMissingAsync("admin@agriapp.com", "Admin SuperUser", Role.SuperUser, null, "SuperUser123!");
    await AddIfMissingAsync("vikram@sangola.agriapp.com", "Vikram Desai", Role.Manager, sangolaId, "Manager123!");
    await AddIfMissingAsync("sneha@sangola.agriapp.com", "Sneha Kulkarni", Role.Sales, sangolaId, "Sales123!");
    await AddIfMissingAsync("ravi@sangola.agriapp.com", "Ravi Jadhav", Role.Staff, sangolaId, "Staff123!");
    await AddIfMissingAsync("james@edison.agriapp.com", "James Morrison", Role.Manager, edisonId, "Manager123!");
    await AddIfMissingAsync("maria@edison.agriapp.com", "Maria Gonzalez", Role.Sales, edisonId, "Sales123!");
    await AddIfMissingAsync("alex@edison.agriapp.com", "Alex Nguyen", Role.Staff, edisonId, "Staff123!");
}
