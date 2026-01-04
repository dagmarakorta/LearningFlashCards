using LearningFlashCards.Api.Services;
using LearningFlashCards.Core.Domain.Entities;
using LearningFlashCards.Infrastructure.Persistence.Repositories;
using LearningFlashCards.Api.Tests.TestUtilities;
using Microsoft.AspNetCore.Http;

namespace LearningFlashCards.Api.Tests.Handlers;

public class DeckHandlerTests
{
    [Fact]
    public async Task UpsertDeck_AssignsOwner_WhenMissing()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new DeckRepository(dbContext);
        var handler = new DeckHandler(repository);
        var ownerId = Guid.NewGuid();

        var deck = new Deck
        {
            Id = Guid.NewGuid(),
            Name = "New Deck",
            Description = "Description",
            RowVersion = Guid.NewGuid().ToByteArray()
        };

        var result = await handler.UpsertDeckAsync(deck, ownerId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ownerId, result.Value!.OwnerId);
    }

    [Fact]
    public async Task UpsertDeck_ReturnsForbidden_WhenOwnerMismatch()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new DeckRepository(dbContext);
        var handler = new DeckHandler(repository);
        var ownerId = Guid.NewGuid();
        var otherOwnerId = Guid.NewGuid();

        var deck = new Deck
        {
            Id = Guid.NewGuid(),
            OwnerId = otherOwnerId,
            Name = "Existing Deck",
            Description = "Description",
            RowVersion = Guid.NewGuid().ToByteArray()
        };

        var result = await handler.UpsertDeckAsync(deck, ownerId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
    }
}
