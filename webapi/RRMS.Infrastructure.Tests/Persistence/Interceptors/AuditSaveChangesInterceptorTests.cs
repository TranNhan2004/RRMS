using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using RRMS.Domain.Entities;
using RRMS.Infrastructure.Persistence.Interceptors;
using Xunit;

namespace RRMS.Infrastructure.Tests.Persistence.Interceptors;

public class AuditSaveChangesInterceptorTests
{
    [Fact]
    public void SerializeValues_ShouldOmitSensitiveFields_WhenPropertyIsMarkedAuditIgnore()
    {
        var options = new DbContextOptionsBuilder<AuditTestContext>()
            .UseMySql("Server=localhost;Database=tests;User=test;Password=test", new MySqlServerVersion(new Version(8, 0, 36)))
            .Options;
        using var context = new AuditTestContext(options);
        var credential = new AuditTestEntity { Id = Guid.NewGuid(), Name = "visible", PasswordHash = "secret" };
        var entry = context.Entry<BaseEntity>(credential);
        entry.State = EntityState.Added;

        var json = AuditSaveChangesInterceptor.SerializeValues(entry, false);

        json.Should().Contain("visible");
        json.Should().NotContain("PasswordHash").And.NotContain("secret");
    }

    private sealed class AuditTestContext : DbContext
    {
        public AuditTestContext(DbContextOptions<AuditTestContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AuditTestEntity>().HasKey(x => x.Id);
            modelBuilder.Entity<AuditTestEntity>().Property(x => x.PasswordHash)
                .HasAnnotation(AuditSaveChangesInterceptor.AuditIgnoreAnnotation, true);
        }
    }

    private sealed class AuditTestEntity : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
    }
}
