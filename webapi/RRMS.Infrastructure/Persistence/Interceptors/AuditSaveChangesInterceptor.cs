using System.Diagnostics;
using System.Collections.Concurrent;
using System.Text.Json;
using RRMS.Application.Interfaces.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using RRMS.Domain.Entities;
using RRMS.Application.Interfaces.Services.Audit;
using RRMS.Domain.Enums.Common;
using RRMS.Domain.Utils;

namespace RRMS.Infrastructure.Persistence.Interceptors;

public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    internal const string AuditIgnoreAnnotation = "RRMS:AuditIgnore";

    private readonly ICurrentRequestContext _currentRequestContext;
    private readonly IAuditLogBackgroundQueue _auditLogBackgroundQueue;
    private readonly ConcurrentDictionary<Guid, IReadOnlyCollection<AuditLog>> _pendingAuditLogs = new();

    public AuditSaveChangesInterceptor(
        ICurrentRequestContext currentRequestContext,
        IAuditLogBackgroundQueue auditLogBackgroundQueue)
    {
        _currentRequestContext = currentRequestContext;
        _auditLogBackgroundQueue = auditLogBackgroundQueue;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not AppDbContext context)
        {
            return base.SavingChangesAsync(
                        eventData,
                        result,
                        cancellationToken);
        }

        var auditLogs = ApplyAudit(context);
        if (auditLogs.Count > 0)
        {
            _pendingAuditLogs[context.ContextId.InstanceId] = auditLogs;
        }
        else
        {
            _pendingAuditLogs.TryRemove(context.ContextId.InstanceId, out _);
        }

        return base.SavingChangesAsync(
            eventData,
            result,
            cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        var savedResult = await base.SavedChangesAsync(eventData, result, cancellationToken);

        if (eventData.Context is AppDbContext context
            && _pendingAuditLogs.TryRemove(context.ContextId.InstanceId, out var auditLogs))
        {
            _auditLogBackgroundQueue.Enqueue(auditLogs);
        }

        return savedResult;
    }

    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is AppDbContext context)
        {
            _pendingAuditLogs.TryRemove(context.ContextId.InstanceId, out _);
        }

        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    public override Task SaveChangesCanceledAsync(
        DbContextEventData eventData,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is AppDbContext context)
        {
            _pendingAuditLogs.TryRemove(context.ContextId.InstanceId, out _);
        }

        return base.SaveChangesCanceledAsync(eventData, cancellationToken);
    }

    private IReadOnlyCollection<AuditLog> ApplyAudit(AppDbContext context)
    {
        var actorId = context.CurrentActorId;
        var parsedActorId = Guid.TryParse(actorId, out var guid) ? guid : Guid.Empty;
        var now = DateTimeOffset.UtcNow;
        var auditEntries = new List<AuditLog>();

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>())
        {
            var state = entry.State;

            if (entry.Entity is AuditLog)
            {
                continue;
            }

            if (entry.Entity is AuditableEntity auditable)
            {
                switch (state)
                {
                    case EntityState.Added:
                        auditable.CreatedAt = now;
                        auditable.CreatedById = parsedActorId == Guid.Empty ? null : parsedActorId;
                        break;
                    case EntityState.Modified:
                        auditable.UpdatedAt = now;
                        auditable.UpdatedById = parsedActorId == Guid.Empty ? null : parsedActorId;
                        entry.Property(nameof(AuditableEntity.CreatedAt)).IsModified = false;
                        entry.Property(nameof(AuditableEntity.CreatedById)).IsModified = false;
                        break;
                }
            }

            if (!string.IsNullOrWhiteSpace(actorId)
                && state is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            {
                auditEntries.Add(CreateAuditLog(entry, actorId, now, _currentRequestContext));
            }
        }

        return auditEntries;
    }

    private static AuditLog CreateAuditLog(
        EntityEntry<BaseEntity> entry,
        string actorId,
        DateTimeOffset now,
        ICurrentRequestContext requestContext)
    {
        var action = entry.State switch
        {
            EntityState.Added => AuditAction.Create,
            EntityState.Deleted => AuditAction.Delete,
            EntityState.Modified when entry.Entity is AuditableEntity { IsDeleted: true } => AuditAction.Delete,
            _ => AuditAction.Update
        };

        return new AuditLog
        {
            Id = Guid.CreateVersion7(),
            Action = action,
            TraceId = Activity.Current?.TraceId.ToString(),
            RequestId = requestContext.TraceId,
            IpAddress = requestContext.RemoteIpAddress,
            ClientActionId = requestContext.ClientActionId,
            ClientRequestId = requestContext.ClientRequestId,
            EntityName = entry.Metadata.ClrType.Name,
            EntityId = entry.Property(nameof(BaseEntity.Id)).CurrentValue?.ToString() ?? string.Empty,
            OldValues = action is AuditAction.Update or AuditAction.Delete
                ? SerializeValues(entry, useOriginalValues: true)
                : null,
            NewValues = action is AuditAction.Create or AuditAction.Update
                ? SerializeValues(entry, useOriginalValues: false)
                : null,
            ActorId = actorId,
            CreatedAt = now
        };
    }

    internal static string? SerializeValues(EntityEntry<BaseEntity> entry, bool useOriginalValues)
    {
        var values = entry.Properties
            .Where(property => property.Metadata.FindAnnotation(AuditIgnoreAnnotation)?.Value is not true)
            .Where(property => property.Metadata.Name is not nameof(AuditableEntity.CreatedAt)
                and not nameof(AuditableEntity.CreatedById)
                and not nameof(AuditableEntity.UpdatedAt)
                and not nameof(AuditableEntity.UpdatedById))
            .Where(property => entry.State != EntityState.Modified
                || entry.Entity is AuditableEntity { IsDeleted: true }
                || property.IsModified)
            .ToDictionary(
                property => property.Metadata.Name,
                property => FormatAuditValue(useOriginalValues ? property.OriginalValue : property.CurrentValue));

        foreach (var complexProperty in entry.ComplexProperties)
        {
            var originalContent = SerializeComplexProperty(complexProperty, useOriginalValues: true);
            var currentContent = SerializeComplexProperty(complexProperty, useOriginalValues: false);
            if (entry.State != EntityState.Modified
                || entry.Entity is AuditableEntity { IsDeleted: true }
                || !string.Equals(originalContent, currentContent, StringComparison.Ordinal))
            {
                values[complexProperty.Metadata.Name] = DeserializeJsonElement(
                    useOriginalValues ? originalContent : currentContent);
            }
        }

        return values.Count == 0 ? null : JsonSerializer.Serialize(values, JsonUtils.WebSerializerOptions);
    }

    private static string SerializeComplexProperty(
        ComplexPropertyEntry complexProperty,
        bool useOriginalValues)
    {
        var values = complexProperty.Properties
            .Where(property => property.Metadata.FindAnnotation(AuditIgnoreAnnotation)?.Value is not true)
            .ToDictionary(
                property => property.Metadata.Name,
                property => FormatAuditValue(useOriginalValues ? property.OriginalValue : property.CurrentValue));

        foreach (var nestedComplexProperty in complexProperty.ComplexProperties)
        {
            values[nestedComplexProperty.Metadata.Name] = DeserializeJsonElement(
                SerializeComplexProperty(nestedComplexProperty, useOriginalValues));
        }

        return JsonSerializer.Serialize(values, JsonUtils.WebSerializerOptions);
    }

    private static object? FormatAuditValue(object? value)
    {
        return value is Enum enumValue ? EnumUtils.ToString(enumValue) : value;
    }

    private static JsonElement DeserializeJsonElement(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }


}
