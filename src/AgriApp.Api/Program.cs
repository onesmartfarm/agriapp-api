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

    if (await db.Centers.AnyAsync()) return;

    var sangola = new Center
    {
        Name = "Sangola Agri-Center",
        Location = "Sangola, Maharashtra, India",
        CurrencySymbol = "₹",
        TimeZoneId = "India Standard Time",
        CreatedAt = DateTime.UtcNow
    };
    var edison = new Center
    {
        Name = "Edison NJ Hub",
        Location = "Edison, NJ, USA",
        CurrencySymbol = "$",
        TimeZoneId = "Eastern Standard Time",
        CreatedAt = DateTime.UtcNow
    };
    db.Centers.AddRange(sangola, edison);
    await db.SaveChangesAsync();

    var vendorDeere = new Vendor
    {
        Name = "John Deere Sangola",
        CenterId = sangola.Id,
        CreatedAt = DateTime.UtcNow
    };
    var vendorMazda = new Vendor
    {
        Name = "Mazda Financial Services (Edison)",
        CenterId = edison.Id,
        CreatedAt = DateTime.UtcNow
    };
    db.Vendors.AddRange(vendorDeere, vendorMazda);
    await db.SaveChangesAsync();

    var customerBafna = new Customer
    {
        Name = "Bafna Farms (Pune)",
        CenterId = sangola.Id,
        CreatedAt = DateTime.UtcNow
    };
    var customerEdison = new Customer
    {
        Name = "Edison Property Management",
        CenterId = edison.Id,
        CreatedAt = DateTime.UtcNow
    };
    db.Customers.AddRange(customerBafna, customerEdison);
    await db.SaveChangesAsync();

    var equipment = new[]
    {
        new Equipment
        {
            Name = "John Deere 5405 Tractor",
            Category = EquipmentCategory.Tractor,
            HourlyRate = 1500.00m,
            CenterId = sangola.Id,
            VendorId = vendorDeere.Id,
            CreatedAt = DateTime.UtcNow
        },
        new Equipment
        {
            Name = "2025 Mazda CX-90 PHEV",
            Category = EquipmentCategory.Vehicle,
            HourlyRate = 85.00m,
            CenterId = edison.Id,
            VendorId = vendorMazda.Id,
            CreatedAt = DateTime.UtcNow
        }
    };
    db.Equipment.AddRange(equipment);
    await db.SaveChangesAsync();

    var users = new[]
    {
        new User
        {
            Name = "Admin SuperUser",
            Email = "admin@agriapp.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("SuperUser123!"),
            Role = Role.SuperUser,
            CenterId = null,
            CreatedAt = DateTime.UtcNow
        },
        new User
        {
            Name = "Vikram Desai",
            Email = "vikram@sangola.agriapp.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Manager123!"),
            Role = Role.Manager,
            CenterId = sangola.Id,
            CreatedAt = DateTime.UtcNow
        },
        new User
        {
            Name = "Sneha Kulkarni",
            Email = "sneha@sangola.agriapp.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Sales123!"),
            Role = Role.Sales,
            CenterId = sangola.Id,
            CreatedAt = DateTime.UtcNow
        },
        new User
        {
            Name = "Ravi Jadhav",
            Email = "ravi@sangola.agriapp.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Staff123!"),
            Role = Role.Staff,
            CenterId = sangola.Id,
            CreatedAt = DateTime.UtcNow
        },
        new User
        {
            Name = "James Morrison",
            Email = "james@edison.agriapp.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Manager123!"),
            Role = Role.Manager,
            CenterId = edison.Id,
            CreatedAt = DateTime.UtcNow
        },
        new User
        {
            Name = "Maria Gonzalez",
            Email = "maria@edison.agriapp.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Sales123!"),
            Role = Role.Sales,
            CenterId = edison.Id,
            CreatedAt = DateTime.UtcNow
        },
        new User
        {
            Name = "Alex Nguyen",
            Email = "alex@edison.agriapp.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Staff123!"),
            Role = Role.Staff,
            CenterId = edison.Id,
            CreatedAt = DateTime.UtcNow
        }
    };
    db.Users.AddRange(users);
    await db.SaveChangesAsync();
}
