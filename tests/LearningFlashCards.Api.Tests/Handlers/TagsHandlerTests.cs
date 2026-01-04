using LearningFlashCards.Api.Services;
using LearningFlashCards.Core.Domain.Entities;
using LearningFlashCards.Infrastructure.Persistence.Repositories;
using LearningFlashCards.Api.Tests.TestUtilities;
using Microsoft.AspNetCore.Http;

namespace LearningFlashCards.Api.Tests.Handlers;

public class TagsHandlerTests
{
    [Fact]
    public async Task UpsertTag_AssignsOwner_WhenMissing()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new TagRepository(dbContext);
        var handler = new TagsHandler(repository);
        var ownerId = Guid.NewGuid();

        var tag = new Tag
        {
            Id = Guid.NewGuid(),
            Name = "Tag",
            RowVersion = Guid.NewGuid().ToByteArray()
        };

        var result = await handler.UpsertTagAsync(tag, ownerId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ownerId, result.Value!.OwnerId);
    }

    [Fact]
    public async Task UpsertTag_ReturnsForbidden_WhenOwnerMismatch()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new TagRepository(dbContext);
        var handler = new TagsHandler(repository);
        var ownerId = Guid.NewGuid();
        var otherOwnerId = Guid.NewGuid();

        var tag = new Tag
        {
            Id = Guid.NewGuid(),
            OwnerId = otherOwnerId,
            Name = "Tag",
            RowVersion = Guid.NewGuid().ToByteArray()
        };

        var result = await handler.UpsertTagAsync(tag, ownerId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
    }
}
