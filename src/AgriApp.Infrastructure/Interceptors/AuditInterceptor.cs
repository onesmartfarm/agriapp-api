using System.Text.Json;
using AgriApp.Core.Entities;
using AgriApp.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AgriApp.Infrastructure.Interceptors;

public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUser? _currentUser;
    private List<PendingAudit>? _pendingAudits;

    public AuditInterceptor(ICurrentUser? currentUser = null)
    {
        _currentUser = currentUser;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        PreparePendingAudits(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        PreparePendingAudits(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(
        SaveChangesCompletedEventData eventData,
        int result)
    {
        FinalizePendingAudits(eventData.Context);
        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        FinalizePendingAudits(eventData.Context);
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private void PreparePendingAudits(DbContext? context)
    {
        if (context is null) return;

        _pendingAudits = new List<PendingAudit>();
        var userId = _currentUser?.UserId ?? 0;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.Entity is AuditLog) continue;
            if (entry.State == EntityState.Detached || entry.State == EntityState.Unchanged) continue;

            var entityName = entry.Entity.GetType().Name;

            switch (entry.State)
            {
                case EntityState.Added:
                    _pendingAudits.Add(new PendingAudit
                    {
                        Entry = entry,
                        AuditLog = new AuditLog
                        {
                            UserId = userId,
                            Timestamp = DateTime.UtcNow,
                            Action = "Created",
                            EntityName = entityName,
                            OldValue = null,
                            NewValue = SerializeEntity(entry.CurrentValues)
                        }
                    });
                    break;

                case EntityState.Modified:
                    _pendingAudits.Add(new PendingAudit
                    {
                        Entry = entry,
                        AuditLog = new AuditLog
                        {
                            UserId = userId,
                            Timestamp = DateTime.UtcNow,
                            Action = "Updated",
                            EntityName = entityName,
                            EntityId = GetPrimaryKeyValue(entry),
                            OldValue = SerializeEntity(entry.OriginalValues),
                            NewValue = SerializeEntity(entry.CurrentValues)
                        }
                    });
                    break;

                case EntityState.Deleted:
                    _pendingAudits.Add(new PendingAudit
                    {
                        Entry = entry,
                        AuditLog = new AuditLog
                        {
                            UserId = userId,
                            Timestamp = DateTime.UtcNow,
                            Action = "Deleted",
                            EntityName = entityName,
                            EntityId = GetPrimaryKeyValue(entry),
                            OldValue = SerializeEntity(entry.OriginalValues),
                            NewValue = null
                        }
                    });
                    break;
            }
        }
    }

    private void FinalizePendingAudits(DbContext? context)
    {
        if (context is null || _pendingAudits is null || _pendingAudits.Count == 0) return;

        foreach (var pending in _pendingAudits)
        {
            if (pending.AuditLog.Action == "Created")
            {
                pending.AuditLog.EntityId = GetPrimaryKeyValue(pending.Entry);
                pending.AuditLog.NewValue = SerializeEntity(pending.Entry.CurrentValues);
            }
            context.Set<AuditLog>().Add(pending.AuditLog);
        }

        _pendingAudits = null;
        context.SaveChanges();
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
            var value = values[prop];
            if (prop.Name.Contains("Password", StringComparison.OrdinalIgnoreCase)) continue;
            dict[prop.Name] = value;
        }
        return JsonSerializer.Serialize(dict);
    }

    private class PendingAudit
    {
        public Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry Entry { get; set; } = null!;
        public AuditLog AuditLog { get; set; } = null!;
    }
}
