using LearningFlashCards.Api.Services;
using LearningFlashCards.Core.Domain.Entities;
using LearningFlashCards.Infrastructure.Persistence.Repositories;
using LearningFlashCards.Api.Tests.TestUtilities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

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

    [Fact]
    public async Task SoftDeleteTag_ReturnsNoContent_WhenOwned()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new TagRepository(dbContext);
        var handler = new TagsHandler(repository);
        var ownerId = Guid.NewGuid();
        var tagId = Guid.NewGuid();

        dbContext.Tags.Add(new Tag
        {
            Id = tagId,
            OwnerId = ownerId,
            Name = "Tag",
            RowVersion = Guid.NewGuid().ToByteArray()
        });
        await dbContext.SaveChangesAsync();

        var result = await handler.SoftDeleteTagAsync(tagId, ownerId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(StatusCodes.Status204NoContent, result.StatusCode);

        var stored = await dbContext.Tags.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tagId, CancellationToken.None);
        Assert.NotNull(stored);
        Assert.NotNull(stored!.DeletedAt);
    }

    [Fact]
    public async Task SoftDeleteTag_ReturnsNotFound_WhenNotOwned()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new TagRepository(dbContext);
        var handler = new TagsHandler(repository);
        var ownerId = Guid.NewGuid();
        var otherOwnerId = Guid.NewGuid();
        var tagId = Guid.NewGuid();

        dbContext.Tags.Add(new Tag
        {
            Id = tagId,
            OwnerId = otherOwnerId,
            Name = "Tag",
            RowVersion = Guid.NewGuid().ToByteArray()
        });
        await dbContext.SaveChangesAsync();

        var result = await handler.SoftDeleteTagAsync(tagId, ownerId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
    }
}
