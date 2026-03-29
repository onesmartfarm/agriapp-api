using System.Text.Json;
using AgriApp.Core.Entities;
using AgriApp.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AgriApp.Infrastructure.Interceptors;

public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUser? _currentUser;

    public AuditInterceptor(ICurrentUser? currentUser = null)
    {
        _currentUser = currentUser;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ProcessAudits(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ProcessAudits(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ProcessAudits(DbContext? context)
    {
        if (context is null) return;

        var userId = _currentUser?.UserId ?? 0; // Or Guid.Empty if your UserIds are Guids
        var auditsToAdd = new List<AuditLog>();

        // .ToList() is CRITICAL here so we don't modify the collection while iterating
        foreach (var entry in context.ChangeTracker.Entries().ToList())
        {
            if (entry.Entity is AuditLog) continue;
            if (entry.State == EntityState.Detached || entry.State == EntityState.Unchanged) continue;

            var entityName = entry.Entity.GetType().Name;

            // 1. Specific Rule: Commission Realized
            if (entry.Entity is CommissionLedger ledger && entry.State == EntityState.Modified)
            {
                var statusProp = entry.Property("Status");
                if (statusProp.IsModified &&
                    statusProp.CurrentValue?.ToString() == "Realized" &&
                    statusProp.OriginalValue?.ToString() != "Realized")
                {
                    auditsToAdd.Add(new AuditLog
                    {
                        UserId = userId,
                        Timestamp = DateTime.UtcNow,
                        Action = "CommissionRealized",
                        EntityName = "CommissionLedger",
                        EntityId = GetPrimaryKeyValue(entry),
                        OldValue = $"Status={statusProp.OriginalValue}",
                        NewValue = $"Status=Realized;UpiTransactionId={ledger.UpiTransactionId}"
                    });
                    continue; // Skip the generic 'Modified' log for this specific event
                }
            }

            // 2. Generic Rules: Added, Modified, Deleted
            switch (entry.State)
            {
                case EntityState.Added:
                    auditsToAdd.Add(new AuditLog
                    {
                        UserId = userId,
                        Timestamp = DateTime.UtcNow,
                        Action = "Created",
                        EntityName = entityName,
                        EntityId = GetPrimaryKeyValue(entry), // Works if using Guids generated client-side
                        OldValue = null,
                        NewValue = SerializeEntity(entry.CurrentValues)
                    });
                    break;

                case EntityState.Modified:
                    auditsToAdd.Add(new AuditLog
                    {
                        UserId = userId,
                        Timestamp = DateTime.UtcNow,
                        Action = "Updated",
                        EntityName = entityName,
                        EntityId = GetPrimaryKeyValue(entry),
                        OldValue = SerializeEntity(entry.OriginalValues),
                        NewValue = SerializeEntity(entry.CurrentValues)
                    });
                    break;

                case EntityState.Deleted:
                    auditsToAdd.Add(new AuditLog
                    {
                        UserId = userId,
                        Timestamp = DateTime.UtcNow,
                        Action = "Deleted",
                        EntityName = entityName,
                        EntityId = GetPrimaryKeyValue(entry),
                        OldValue = SerializeEntity(entry.OriginalValues),
                        NewValue = null
                    });
                    break;
            }
        }

        // Add all gathered audits to the context so they save in the same transaction
        if (auditsToAdd.Any())
        {
            context.AddRange(auditsToAdd);
        }
    }

    private static string? GetPrimaryKeyValue(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry)
    {
        var keyProperties = entry.Metadata.FindPrimaryKey()?.Properties;
        if (keyProperties == null) return null;

        var values = keyProperties.Select(p => entry.Property(p.Name).CurrentValue?.ToString());
        return string.Join(",", values);
    }

    private static string SerializeEntity(Microsoft.EntityFrameworkCore.ChangeTracking.PropertyValues values)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var prop in values.Properties)
        {
            if (prop.Name.Contains("Password", StringComparison.OrdinalIgnoreCase)) continue;
            dict[prop.Name] = values[prop];
        }
        var options = new JsonSerializerOptions 
        { 
            WriteIndented = false,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };
        return JsonSerializer.Serialize(dict, options);
    }
}