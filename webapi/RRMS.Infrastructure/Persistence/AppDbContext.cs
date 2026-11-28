using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RRMS.Application.Interfaces.Repositories;
using RRMS.Domain.Entities;
using RRMS.Domain.Exceptions;
using RRMS.Infrastructure.Persistence.Extensions;

namespace RRMS.Infrastructure.Persistence;

public class AppDbContext : DbContext, IUnitOfWork
{
    internal string? CurrentActorId { get; private set; }

    public AppDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<Province> Provinces => Set<Province>();
    public DbSet<Ward> Wards => Set<Ward>();
    public DbSet<AddressLine> AddressLines => Set<AddressLine>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<UserCredential> UserCredentials => Set<UserCredential>();
    public DbSet<UserIdentity> UserIdentities => Set<UserIdentity>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<FileStorage> FileStorages => Set<FileStorage>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public async Task<int> SaveChangesAsync(
        string actorId,
        CancellationToken cancellationToken = default)
    {
        CurrentActorId = actorId;

        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            foreach (var entry in ex.Entries.Where(entry => entry.Entity is User))
            {
                var userName = entry.Property(nameof(User.UserName));
                if (userName.IsModified && userName.OriginalValue == null)
                {
                    var databaseValues = await entry.GetDatabaseValuesAsync(cancellationToken);
                    if (databaseValues?.GetValue<string?>(nameof(User.UserName)) != null)
                    {
                        throw new UnprocessableEntityException("Username can only be set once.");
                    }
                }
            }

            throw new ConflictException("The data was changed by another request. Please retry.");
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            throw ex.ToConflictException(this);
        }
        finally
        {
            CurrentActorId = null;
        }
    }

    public async Task<int> SaveChangesWithoutAuditAsync(CancellationToken cancellationToken = default)
    {
        CurrentActorId = null;

        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            throw ex.ToConflictException(this);
        }
    }

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            throw ex.ToConflictException(this);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        ConfigureBaseEntities(modelBuilder);
        ConfigureAuditableEntities(modelBuilder);
    }

    private static void ConfigureBaseEntities(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var builder = modelBuilder.Entity(entityType.ClrType);

            builder.HasKey(nameof(BaseEntity.Id));

            builder.Property(nameof(BaseEntity.Id))
                .HasColumnName("id")
                .HasColumnType("char(36)")
                .ValueGeneratedNever()
                .IsRequired();
        }
    }

    private static void ConfigureAuditableEntities(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            var builder = modelBuilder.Entity(entityType.ClrType);

            builder.Property(nameof(AuditableEntity.CreatedAt))
                .HasColumnName("created_at")
                .HasColumnType("timestamp(6)")
                .IsRequired();

            builder.Property(nameof(AuditableEntity.CreatedById))
                .HasColumnName("created_by_id")
                .HasColumnType("char(36)")
                .IsRequired(false);

            builder.HasOne(nameof(AuditableEntity.CreatedBy))
                .WithMany()
                .HasForeignKey(nameof(AuditableEntity.CreatedById))
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(nameof(AuditableEntity.UpdatedAt))
                .HasColumnName("updated_at")
                .HasColumnType("timestamp(6)")
                .IsRequired(false);

            builder.Property(nameof(AuditableEntity.UpdatedById))
                .HasColumnName("updated_by_id")
                .HasColumnType("char(36)")
                .IsRequired(false);

            builder.HasOne(nameof(AuditableEntity.UpdatedBy))
                .WithMany()
                .HasForeignKey(nameof(AuditableEntity.UpdatedById))
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(nameof(AuditableEntity.IsDeleted))
                .HasColumnName("is_deleted")
                .IsRequired();

            ApplySoftDeleteFilter(modelBuilder, entityType.ClrType);
        }
    }

    private static void ApplySoftDeleteFilter(ModelBuilder modelBuilder, Type entityType)
    {
        var parameter = Expression.Parameter(entityType, "entity");
        var isDeleted = Expression.Property(parameter, nameof(AuditableEntity.IsDeleted));
        var condition = Expression.Not(isDeleted);
        var filter = Expression.Lambda(condition, parameter);

        modelBuilder.Entity(entityType)
            .HasQueryFilter(filter);
    }
}
