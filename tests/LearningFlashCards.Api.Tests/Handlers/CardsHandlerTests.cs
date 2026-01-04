using LearningFlashCards.Api.Services;
using LearningFlashCards.Core.Domain.Entities;
using LearningFlashCards.Infrastructure.Persistence.Repositories;
using LearningFlashCards.Api.Tests.TestUtilities;
using Microsoft.AspNetCore.Http;

namespace LearningFlashCards.Api.Tests.Handlers;

public class CardsHandlerTests
{
    [Fact]
    public async Task GetCard_ReturnsNotFound_WhenDeckNotOwned()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var deckRepository = new DeckRepository(dbContext);
        var cardRepository = new CardRepository(dbContext);
        var handler = new CardsHandler(cardRepository, deckRepository);

        var deck = new Deck
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Foreign Deck"
        };
        dbContext.Decks.Add(deck);
        await dbContext.SaveChangesAsync();

        var result = await handler.GetCardAsync(deck.Id, Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
    }

    [Fact]
    public async Task UpsertCard_Succeeds_ForOwnedDeck()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var deckRepository = new DeckRepository(dbContext);
        var cardRepository = new CardRepository(dbContext);
        var handler = new CardsHandler(cardRepository, deckRepository);
        var ownerId = Guid.NewGuid();

        var deck = new Deck
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Name = "Owned Deck",
            RowVersion = Guid.NewGuid().ToByteArray()
        };
        dbContext.Decks.Add(deck);
        await dbContext.SaveChangesAsync();

        var card = new Card
        {
            Id = Guid.NewGuid(),
            Front = "Front",
            Back = "Back",
            RowVersion = Guid.NewGuid().ToByteArray(),
            State = new CardState
            {
                DueAt = DateTimeOffset.UtcNow,
                IntervalDays = 1,
                EaseFactor = 2.5,
                Streak = 0,
                Lapses = 0
            }
        };

        var result = await handler.UpsertCardAsync(deck.Id, card, ownerId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(deck.Id, result.Value!.DeckId);
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
    }
}
