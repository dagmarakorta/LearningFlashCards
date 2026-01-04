using LearningFlashCards.Api.Controllers;
using LearningFlashCards.Api.Services;
using LearningFlashCards.Infrastructure.Persistence.Repositories;
using LearningFlashCards.Api.Tests.TestUtilities;
using Microsoft.AspNetCore.Http;

namespace LearningFlashCards.Api.Tests.Handlers;

public class CreateUserProfileHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsConflict_WhenEmailExists()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new UserProfileRepository(dbContext);
        var handler = new CreateUserProfileHandler(repository);

        var existing = new LearningFlashCards.Core.Domain.Entities.UserProfile
        {
            Email = "test@example.com",
            DisplayName = "Tester"
        };
        await repository.CreateAsync(existing, CancellationToken.None);

        var request = new CreateUserRequest
        {
            Email = "test@example.com",
            DisplayName = "Another Tester"
        };

        var result = await handler.HandleAsync(request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_ReturnsCreatedProfile_WithSanitizedFields()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new UserProfileRepository(dbContext);
        var handler = new CreateUserProfileHandler(repository);

        var request = new CreateUserRequest
        {
            Email = "  MixedCase@Example.com  ",
            DisplayName = "  Jane Doe  ",
            AvatarUrl = "   "
        };

        var result = await handler.HandleAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Profile);
        Assert.Equal("mixedcase@example.com", result.Profile.Email);
        Assert.Equal("Jane Doe", result.Profile.DisplayName);
        Assert.Null(result.Profile.AvatarUrl);
        Assert.NotEqual(Guid.Empty, result.Profile.Id);
        Assert.NotNull(result.Profile.RowVersion);
        Assert.NotEmpty(result.Profile.RowVersion);
    }

    [Fact]
    public async Task HandleAsync_ReturnsBadRequest_WhenUnsafeCharactersPresent()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new UserProfileRepository(dbContext);
        var handler = new CreateUserProfileHandler(repository);

        var request = new CreateUserRequest
        {
            Email = "bad/<email@example.com>",
            DisplayName = "Bad/User"
        };

        var result = await handler.HandleAsync(request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_ReturnsBadRequest_WhenAvatarUrlInvalid()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new UserProfileRepository(dbContext);
        var handler = new CreateUserProfileHandler(repository);

        var request = new CreateUserRequest
        {
            Email = "good@example.com",
            DisplayName = "Good User",
            AvatarUrl = "not-a-url"
        };

        var result = await handler.HandleAsync(request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
    }
}
