using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using RRMS.Domain.Entities;
using RRMS.Infrastructure.Persistence;
using RRMS.Infrastructure.Repositories;
using Xunit;

namespace RRMS.Infrastructure.Tests.Repositories;

public class UserRepositoryTests
{
    [Fact]
    public async Task FindUserIdByEmail_ShouldReturnId_WhenEmailMatchesIgnoringCase()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new AppDbContext(options);
        var id = Guid.NewGuid();
        context.Users.Add(new User
        {
            Id = id,
            Email = "User@Example.org"
        });
        await context.SaveChangesAsync();
        var repository = new UserRepository(Mock.Of<Serilog.ILogger>(), context);

        // Act
        var result = repository.FindUserIdByEmail("user@example.org");

        // Assert
        result.Should().Be(id);
    }

    [Fact]
    public async Task FindUserIdByEmail_ShouldReturnNull_WhenUserIsDeleted()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new AppDbContext(options);
        context.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.org",
            IsDeleted = true
        });
        await context.SaveChangesAsync();
        var repository = new UserRepository(Mock.Of<Serilog.ILogger>(), context);

        // Act
        var result = repository.FindUserIdByEmail("user@example.org");

        // Assert
        result.Should().BeNull();
    }
}
