using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using RRMS.Domain.Entities;
using RRMS.Domain.Exceptions;
using RRMS.Infrastructure.Persistence;
using Xunit;

namespace RRMS.Infrastructure.Tests.Persistence;

public class UserNameConcurrencyTests
{
    [Fact]
    public async Task SaveChangesAsync_ShouldRejectSecondUpdate_WhenUserNameWasSetConcurrently()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var userId = Guid.NewGuid();
        await using var firstContext = new AppDbContext(options);
        firstContext.Users.Add(new User
        {
            Id = userId,
            Email = "user@example.org"
        });
        await firstContext.SaveChangesAsync();
        await using var secondContext = new AppDbContext(options);
        var first = await firstContext.Users.SingleAsync(user => user.Id == userId);
        var second = await secondContext.Users.SingleAsync(user => user.Id == userId);
        first.UserName = "first";
        second.UserName = "second";
        await firstContext.SaveChangesAsync(userId.ToString());

        // Act
        var action = () => secondContext.SaveChangesAsync(userId.ToString());

        // Assert
        await action.Should().ThrowAsync<UnprocessableEntityException>();
        await using var verifyContext = new AppDbContext(options);
        (await verifyContext.Users.SingleAsync(user => user.Id == userId)).UserName.Should().Be("first");
    }
}
