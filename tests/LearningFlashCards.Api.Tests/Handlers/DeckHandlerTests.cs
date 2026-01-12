using LearningFlashCards.Api.Services;
using LearningFlashCards.Core.Domain.Entities;
using LearningFlashCards.Infrastructure.Persistence.Repositories;
using LearningFlashCards.Api.Tests.TestUtilities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace LearningFlashCards.Api.Tests.Handlers;

public class DeckHandlerTests
{
    [Fact]
    public async Task UpsertDeck_AssignsOwner_WhenMissing()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new DeckRepository(dbContext);
        var cardRepository = new CardRepository(dbContext);
        var handler = new DeckHandler(repository, cardRepository);
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
        var cardRepository = new CardRepository(dbContext);
        var handler = new DeckHandler(repository, cardRepository);
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
        var cardRepository = new CardRepository(dbContext);
        var handler = new DeckHandler(repository, cardRepository);
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
        var cardRepository = new CardRepository(dbContext);
        var handler = new DeckHandler(repository, cardRepository);
        var ownerId = Guid.NewGuid();

        var deck = new Deck
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Name = "Deletable",
            RowVersion = Guid.NewGuid().ToByteArray()
        };
        dbContext.Decks.Add(deck);
        dbContext.Cards.AddRange(
            new Card { Id = Guid.NewGuid(), DeckId = deck.Id, Front = "Front", Back = "Back", RowVersion = Guid.NewGuid().ToByteArray() },
            new Card { Id = Guid.NewGuid(), DeckId = deck.Id, Front = "Front 2", Back = "Back 2", RowVersion = Guid.NewGuid().ToByteArray() }
        );
        await dbContext.SaveChangesAsync();

        var result = await handler.SoftDeleteDeckAsync(deck.Id, ownerId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(StatusCodes.Status204NoContent, result.StatusCode);

        var stored = await dbContext.Decks.AsNoTracking().FirstOrDefaultAsync(d => d.Id == deck.Id, CancellationToken.None);
        Assert.NotNull(stored);
        Assert.NotNull(stored!.DeletedAt);

        var storedCards = await dbContext.Cards.AsNoTracking()
            .Where(c => c.DeckId == deck.Id)
            .ToListAsync(CancellationToken.None);
        Assert.All(storedCards, card => Assert.NotNull(card.DeletedAt));
    }

    [Fact]
    public async Task SoftDeleteDeck_ReturnsNotFound_WhenNotOwned()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new DeckRepository(dbContext);
        var cardRepository = new CardRepository(dbContext);
        var handler = new DeckHandler(repository, cardRepository);
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
        var cardRepository = new CardRepository(dbContext);
        var handler = new DeckHandler(repository, cardRepository);
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
