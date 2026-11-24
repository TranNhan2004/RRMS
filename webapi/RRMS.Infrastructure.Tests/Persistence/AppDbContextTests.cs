using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using RRMS.Domain.Entities;
using RRMS.Domain.Enums.Users;
using RRMS.Domain.Enums.Uam;
using RRMS.Infrastructure.Persistence;
using Xunit;

namespace RRMS.Infrastructure.Tests.Persistence;

public class AppDbContextTests
{
    [Fact]
    public void Model_ShouldMapEveryEntityIdAsChar36_WhenEntitiesInheritBaseEntity()
    {
        using var context = CreateContext();

        var entities = context.Model.GetEntityTypes()
            .Where(entity => typeof(BaseEntity).IsAssignableFrom(entity.ClrType))
            .ToList();

        entities.Should().NotBeEmpty();
        entities.Should().OnlyContain(entity => entity.FindPrimaryKey() != null);
        foreach (var entity in entities)
        {
            var id = entity.FindProperty(nameof(BaseEntity.Id));
            id.Should().NotBeNull();
            id!.GetColumnName().Should().Be("id");
            id.GetColumnType().Should().Be("char(36)");
            id.ValueGenerated.Should().Be(ValueGenerated.Never);
        }
    }

    [Fact]
    public void Model_ShouldApplySoftDeleteFilterOnlyToAuditableEntities_WhenConfiguringModel()
    {
        using var context = CreateContext();

        var auditable = context.Model.FindEntityType(typeof(User));
        var baseOnly = context.Model.FindEntityType(typeof(Province));
        var auditLog = context.Model.FindEntityType(typeof(AuditLog));

        auditable.Should().NotBeNull();
        auditable!.GetQueryFilter().Should().NotBeNull();
        baseOnly!.GetQueryFilter().Should().BeNull();
        auditLog!.GetQueryFilter().Should().BeNull();
        context.Users.ToQueryString().Should().Contain("is_deleted");
        context.Users.IgnoreQueryFilters().ToQueryString().Should().NotContain("WHERE");
    }

    [Fact]
    public void Model_ShouldMapAuditPropertiesAndRelationships_WhenEntityIsAuditable()
    {
        using var context = CreateContext();

        var entity = context.Model.FindEntityType(typeof(UserProfile));

        entity.Should().NotBeNull();
        entity!.FindProperty(nameof(AuditableEntity.CreatedAt))!.GetColumnType().Should().Be("timestamp(6)");
        entity.FindProperty(nameof(AuditableEntity.CreatedById))!.GetColumnType().Should().Be("char(36)");
        entity.GetForeignKeys().Where(key => key.Properties.Any(property => property.Name == nameof(AuditableEntity.CreatedById) || property.Name == nameof(AuditableEntity.UpdatedById)))
            .Should().HaveCount(2).And.OnlyContain(key => key.DeleteBehavior == DeleteBehavior.Restrict);
    }

    [Fact]
    public void Model_ShouldStoreUppercaseEnumNames_WhenWritingAuthenticationEntities()
    {
        using var context = CreateContext();
        var status = context.Model.FindEntityType(typeof(User))!.FindProperty(nameof(User.Status))!.GetValueConverter()!;
        var provider = context.Model.FindEntityType(typeof(UserIdentity))!.FindProperty(nameof(UserIdentity.Provider))!.GetValueConverter()!;

        var statusValue = status.ConvertToProvider(UserStatus.Active);
        var providerValue = provider.ConvertToProvider(UserIdentityProvider.Google);

        statusValue.Should().Be("ACTIVE");
        providerValue.Should().Be("GOOGLE");
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql("Server=localhost;Database=tests;User=test;Password=test", new MySqlServerVersion(new Version(8, 0, 36)))
            .Options;
        return new AppDbContext(options);
    }
}
