using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AgriApp.Infrastructure.Data;

/// <summary>
/// Allows <c>dotnet ef</c> to create <see cref="AgriDbContext"/> when the startup project is not used
/// (e.g. running from the Infrastructure folder). Prefer:
/// <c>dotnet ef database update --project src/AgriApp.Infrastructure --startup-project src/AgriApp.Api</c>
/// </summary>
public sealed class AgriDbContextFactory : IDesignTimeDbContextFactory<AgriDbContext>
{
    public AgriDbContext CreateDbContext(string[] args)
    {
        TryLoadDotEnvFromAncestors();
        var raw = Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? ReadDefaultConnectionFromAppsettings(FindApiProjectDirectory())
            ?? throw new InvalidOperationException(
                "No database connection for EF tools. Set environment variable DATABASE_URL, " +
                "or add ConnectionStrings:DefaultConnection in AgriApp.Api/appsettings*.json, " +
                "or run from repo root: " +
                "dotnet ef database update --project src/AgriApp.Infrastructure --startup-project src/AgriApp.Api");

        var connectionString = ConvertToNpgsqlConnectionString(raw);

        var optionsBuilder = new DbContextOptionsBuilder<AgriDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        return new AgriDbContext(optionsBuilder.Options, currentUser: null);
    }

    private static void TryLoadDotEnvFromAncestors()
    {
        for (var dir = new DirectoryInfo(Directory.GetCurrentDirectory()); dir != null; dir = dir.Parent)
        {
            var envPath = Path.Combine(dir.FullName, ".env");
            if (!File.Exists(envPath)) continue;

            foreach (var line in File.ReadAllLines(envPath))
            {
                var trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith('#')) continue;
                var eq = trimmed.IndexOf('=');
                if (eq <= 0) continue;
                var key = trimmed[..eq].Trim();
                var value = trimmed[(eq + 1)..].Trim().Trim('"');
                if (key.Length > 0)
                    Environment.SetEnvironmentVariable(key, value);
            }

            break;
        }
    }

    private static string? FindApiProjectDirectory()
    {
        for (var dir = new DirectoryInfo(Directory.GetCurrentDirectory()); dir != null; dir = dir.Parent)
        {
            var direct = Path.Combine(dir.FullName, "AgriApp.Api");
            if (File.Exists(Path.Combine(direct, "AgriApp.Api.csproj")))
                return direct;

            var underSrc = Path.Combine(dir.FullName, "src", "AgriApp.Api");
            if (File.Exists(Path.Combine(underSrc, "AgriApp.Api.csproj")))
                return underSrc;
        }

        return null;
    }

    private static string? ReadDefaultConnectionFromAppsettings(string? apiDir)
    {
        if (apiDir == null || !Directory.Exists(apiDir)) return null;

        foreach (var name in new[] { "appsettings.Development.json", "appsettings.json" })
        {
            var path = Path.Combine(apiDir, name);
            if (!File.Exists(path)) continue;

            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                if (doc.RootElement.TryGetProperty("ConnectionStrings", out var cs) &&
                    cs.TryGetProperty("DefaultConnection", out var dc))
                {
                    var s = dc.GetString();
                    if (!string.IsNullOrWhiteSpace(s)) return s;
                }
            }
            catch
            {
                // ignore malformed json
            }
        }

        return null;
    }

    private static string ConvertToNpgsqlConnectionString(string url)
    {
        if (!url.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            return url;

        var uri = new Uri(url);
        var userInfo = uri.UserInfo.Split(':');
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 5432;
        var database = uri.AbsolutePath.TrimStart('/');

        if (userInfo.Length >= 2)
        {
            var username = Uri.UnescapeDataString(userInfo[0]);
            var password = Uri.UnescapeDataString(userInfo[1]);
            var sslMode = ParseQueryParameter(uri.Query, "sslmode") ?? "Prefer";
            return $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode={sslMode}";
        }

        return $"Host={host};Port={port};Database={database}";
    }

    private static string? ParseQueryParameter(string query, string name)
    {
        if (string.IsNullOrEmpty(query)) return null;
        var q = query.StartsWith('?') ? query[1..] : query;
        foreach (var part in q.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = part.Split('=', 2);
            if (kv.Length == 2 && kv[0].Equals(name, StringComparison.OrdinalIgnoreCase))
                return Uri.UnescapeDataString(kv[1]);
        }

        return null;
    }
}
