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

    [Fact]
    public async Task GetDeck_ReturnsNotFound_WhenMissing()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new DeckRepository(dbContext);
        var handler = new DeckHandler(repository);
        var ownerId = Guid.NewGuid();

        var result = await handler.GetDeckAsync(Guid.NewGuid(), ownerId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
    }

    [Fact]
    public async Task SoftDeleteDeck_ReturnsNoContent_WhenOwned()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new DeckRepository(dbContext);
        var handler = new DeckHandler(repository);
        var ownerId = Guid.NewGuid();

        var deck = new Deck
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Name = "Deletable",
            RowVersion = Guid.NewGuid().ToByteArray()
        };
        dbContext.Decks.Add(deck);
        await dbContext.SaveChangesAsync();

        var result = await handler.SoftDeleteDeckAsync(deck.Id, ownerId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(StatusCodes.Status204NoContent, result.StatusCode);

        var stored = await repository.GetAsync(deck.Id, CancellationToken.None);
        Assert.NotNull(stored);
        Assert.NotNull(stored!.DeletedAt);
    }

    [Fact]
    public async Task SoftDeleteDeck_ReturnsNotFound_WhenNotOwned()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new DeckRepository(dbContext);
        var handler = new DeckHandler(repository);
        var ownerId = Guid.NewGuid();
        var otherOwnerId = Guid.NewGuid();
        var deckId = Guid.NewGuid();

        dbContext.Decks.Add(new Deck
        {
            Id = deckId,
            OwnerId = otherOwnerId,
            Name = "Not Mine",
            RowVersion = Guid.NewGuid().ToByteArray()
        });
        await dbContext.SaveChangesAsync();

        var result = await handler.SoftDeleteDeckAsync(deckId, ownerId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
    }

    [Fact]
    public async Task GetDecks_ReturnsOwnedDecks()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new DeckRepository(dbContext);
        var handler = new DeckHandler(repository);
        var ownerId = Guid.NewGuid();
        var otherOwnerId = Guid.NewGuid();

        dbContext.Decks.AddRange(
            new Deck { Id = Guid.NewGuid(), OwnerId = ownerId, Name = "Mine", RowVersion = Guid.NewGuid().ToByteArray() },
            new Deck { Id = Guid.NewGuid(), OwnerId = otherOwnerId, Name = "Theirs", RowVersion = Guid.NewGuid().ToByteArray() }
        );
        await dbContext.SaveChangesAsync();

        var result = await handler.GetDecksAsync(ownerId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal("Mine", result.Value!.First().Name);
    }
}
