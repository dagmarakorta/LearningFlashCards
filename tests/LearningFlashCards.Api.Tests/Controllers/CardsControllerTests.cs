using LearningFlashCards.Api.Controllers;
using LearningFlashCards.Api.Services;
using LearningFlashCards.Core.Domain.Entities;
using LearningFlashCards.Api.Tests.TestUtilities;
using LearningFlashCards.Infrastructure.Persistence.Repositories;
using LearningFlashCards.Api.Tests.TestUtilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace LearningFlashCards.Api.Tests.Controllers;

public class CardsControllerTests
{
    [Fact]
    public async Task GetCards_ReturnsBadRequest_WhenHeaderMissing()
    {
        var handler = Mock.Of<CardsHandler>();
        var controller = new CardsController(handler)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await controller.GetCards(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetCard_ReturnsNotFound_WhenHandlerNotFound()
    {
        var handlerMock = new Mock<CardsHandler>(MockBehavior.Strict, null!, null!);
        handlerMock.Setup(h => h.GetCardAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(HandlerResult<Card>.NotFound());

        var controller = new CardsController(handlerMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        controller.HttpContext.Request.Headers["X-Owner-Id"] = Guid.NewGuid().ToString();

        var result = await controller.GetCard(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        var notFound = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task UpsertCard_ReturnsOk_WhenOwned()
    {
        using var dbContext = Api.Tests.TestUtilities.TestDbContextFactory.CreateContext();
        var deckRepository = new DeckRepository(dbContext);
        var cardRepository = new CardRepository(dbContext);
        var handler = new CardsHandler(cardRepository, deckRepository);
        var ownerId = Guid.NewGuid();
        var deckId = Guid.NewGuid();
        await deckRepository.UpsertAsync(new Deck
        {
            Id = deckId,
            OwnerId = ownerId,
            Name = "Deck"
        }, CancellationToken.None);

        var controller = new CardsController(handler)
        {
            ControllerContext = ControllerContextFactory.WithOwner(ownerId)
        };

        var card = new Card
        {
            Front = "F",
            Back = "B",
            State = new CardState
            {
                DueAt = DateTimeOffset.UtcNow,
                IntervalDays = 1,
                EaseFactor = 2.5,
                Streak = 0,
                Lapses = 0
            }
        };

        var result = await controller.UpsertCard(deckId, card, CancellationToken.None);

        var ok = Assert.IsType<ObjectResult>(result.Result);
        var saved = Assert.IsType<Card>(ok.Value);
        Assert.Equal(deckId, saved.DeckId);
    }

    [Fact]
    public async Task SoftDeleteCard_ReturnsNotFound_WhenHandlerNotFound()
    {
        var handlerMock = new Mock<CardsHandler>(MockBehavior.Strict, null!, null!);
        handlerMock.Setup(h => h.SoftDeleteCardAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(HandlerResult<string?>.NotFound());

        var controller = new CardsController(handlerMock.Object)
        {
            ControllerContext = ControllerContextFactory.WithOwner(Guid.NewGuid())
        };

        var result = await controller.SoftDeleteCard(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        var notFound = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }
}
