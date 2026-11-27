using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using RRMS.Domain.Entities;
using RRMS.Domain.Enums.Users;
using RRMS.Domain.Enums.Common;
using RRMS.Domain.Enums.Uam;
using RRMS.Infrastructure.Persistence;
using Xunit;

namespace RRMS.Infrastructure.Tests.Persistence;

public class AppDbContextTests
{
    [Fact]
    public void Model_ShouldMapEveryEntityIdAsChar36_WhenEntitiesInheritBaseEntity()
    {
        // Arrange
        using var context = CreateContext();

        // Act
        var entities = context.Model.GetEntityTypes()
            .Where(entity => typeof(BaseEntity).IsAssignableFrom(entity.ClrType))
            .ToList();

        // Assert
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
        // Arrange
        using var context = CreateContext();

        // Act
        var auditable = context.Model.FindEntityType(typeof(User));
        var baseOnly = context.Model.FindEntityType(typeof(Province));
        var auditLog = context.Model.FindEntityType(typeof(AuditLog));

        // Assert
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
        // Arrange
        using var context = CreateContext();

        // Act
        var entity = context.Model.FindEntityType(typeof(UserProfile));

        // Assert
        entity.Should().NotBeNull();
        entity!.FindProperty(nameof(AuditableEntity.CreatedAt))!.GetColumnType().Should().Be("timestamp(6)");
        entity.FindProperty(nameof(AuditableEntity.CreatedById))!.GetColumnType().Should().Be("char(36)");
        entity.GetForeignKeys().Where(key => key.Properties.Any(property => property.Name == nameof(AuditableEntity.CreatedById) || property.Name == nameof(AuditableEntity.UpdatedById)))
            .Should().HaveCount(2).And.OnlyContain(key => key.DeleteBehavior == DeleteBehavior.Restrict);
    }

    [Fact]
    public void Model_ShouldStorePascalCaseEnumNames_WhenWritingAuthenticationEntities()
    {
        // Arrange
        using var context = CreateContext();
        var status = context.Model.FindEntityType(typeof(User))!.FindProperty(nameof(User.Status))!.GetTypeMapping().Converter!;
        var provider = context.Model.FindEntityType(typeof(UserIdentity))!.FindProperty(nameof(UserIdentity.Provider))!.GetTypeMapping().Converter!;

        // Act
        var statusValue = status.ConvertToProvider(UserStatus.Active);
        var providerValue = provider.ConvertToProvider(UserIdentityProvider.Google);

        // Assert
        statusValue.Should().Be("Active");
        providerValue.Should().Be("Google");
        status.ConvertFromProvider("ACTIVE").Should().Be(UserStatus.Active);
        provider.ConvertFromProvider("GOOGLE").Should().Be(UserIdentityProvider.Google);
    }

    [Theory]
    [InlineData(AuditAction.Create, "Create")]
    [InlineData(AuditAction.Update, "Update")]
    [InlineData(AuditAction.Delete, "Delete")]
    public void Model_ShouldStorePascalCaseAuditActions_WhenConvertingValues(
        AuditAction action, string expected)
    {
        // Arrange
        using var context = CreateContext();
        var converter = context.Model.FindEntityType(typeof(AuditLog))!.FindProperty(nameof(AuditLog.Action))!.GetTypeMapping().Converter!;

        // Act
        var providerValue = converter.ConvertToProvider(action);
        var domainValue = converter.ConvertFromProvider(expected);

        // Assert
        providerValue.Should().Be(expected);
        domainValue.Should().Be(action);
    }

    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql("Server=localhost;Database=tests;User=test;Password=test", new MySqlServerVersion(new Version(8, 0, 36)))
            .Options;
        return new AppDbContext(options);
    }
}
