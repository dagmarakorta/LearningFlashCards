using LearningFlashCards.Api.Controllers;
using LearningFlashCards.Api.Controllers.Requests;
using LearningFlashCards.Api.Services;
using LearningFlashCards.Core.Domain.Entities;
using LearningFlashCards.Api.Tests.TestUtilities;
using LearningFlashCards.Infrastructure.Persistence.Repositories;
using LearningFlashCards.Core.Application.Abstractions.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace LearningFlashCards.Api.Tests.Controllers;

public class DecksControllerTests
{
    [Fact]
    public async Task GetDecks_ReturnsBadRequest_WhenHeaderMissing()
    {
        var handler = new DeckHandler(Mock.Of<IDeckRepository>(), Mock.Of<ICardRepository>());
        var controller = new DecksController(handler)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        var result = await controller.GetDecks(CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetDeck_ReturnsNotFound_WhenHandlerNotFound()
    {
        var repoMock = new Mock<IDeckRepository>();
        repoMock.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Deck?)null);
        var handler = new DeckHandler(repoMock.Object, Mock.Of<ICardRepository>());

        var controller = new DecksController(handler)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
        controller.HttpContext.Request.Headers["X-Owner-Id"] = Guid.NewGuid().ToString();

        var result = await controller.GetDeck(Guid.NewGuid(), CancellationToken.None);

        var notFound = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task GetDecks_ReturnsOwnedDecks_FromHandler()
    {
        using var dbContext = Api.Tests.TestUtilities.TestDbContextFactory.CreateContext();
        var repository = new DeckRepository(dbContext);
        var cardRepository = new CardRepository(dbContext);
        var handler = new DeckHandler(repository, cardRepository);
        var ownerId = Guid.NewGuid();
        var controller = new DecksController(handler)
        {
            ControllerContext = ControllerContextFactory.WithOwner(ownerId)
        };

        await repository.UpsertAsync(new Deck
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Name = "Mine"
        }, CancellationToken.None);
        await repository.UpsertAsync(new Deck
        {
            Id = Guid.NewGuid(),
            OwnerId = Guid.NewGuid(),
            Name = "Not Mine"
        }, CancellationToken.None);

        var result = await controller.GetDecks(CancellationToken.None);

        var ok = Assert.IsType<ObjectResult>(result.Result);
        var decks = Assert.IsAssignableFrom<IReadOnlyList<Deck>>(ok.Value);
        Assert.Single(decks);
        Assert.Equal("Mine", decks.First().Name);
    }

    [Fact]
    public async Task SoftDeleteDeck_ReturnsNotFound_WhenHandlerNotFound()
    {
        var repoMock = new Mock<IDeckRepository>();
        repoMock.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Deck?)null);
        var handler = new DeckHandler(repoMock.Object, Mock.Of<ICardRepository>());

        var controller = new DecksController(handler)
        {
            ControllerContext = ControllerContextFactory.WithOwner(Guid.NewGuid())
        };

        var result = await controller.SoftDeleteDeck(Guid.NewGuid(), CancellationToken.None);

        var notFound = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task GetDeck_ReturnsBadRequest_WhenHeaderMissing()
    {
        var handler = new DeckHandler(Mock.Of<IDeckRepository>(), Mock.Of<ICardRepository>());
        var controller = new DecksController(handler)
        {
            ControllerContext = ControllerContextFactory.WithoutOwner()
        };

        var result = await controller.GetDeck(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetDeck_ReturnsDeck_WhenFound()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new DeckRepository(dbContext);
        var cardRepository = new CardRepository(dbContext);
        var handler = new DeckHandler(repository, cardRepository);
        var ownerId = Guid.NewGuid();
        var deckId = Guid.NewGuid();
        var controller = new DecksController(handler)
        {
            ControllerContext = ControllerContextFactory.WithOwner(ownerId)
        };

        await repository.UpsertAsync(new Deck
        {
            Id = deckId,
            OwnerId = ownerId,
            Name = "Test Deck"
        }, CancellationToken.None);

        var result = await controller.GetDeck(deckId, CancellationToken.None);

        var ok = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
        var deck = Assert.IsType<Deck>(ok.Value);
        Assert.Equal("Test Deck", deck.Name);
    }

    [Fact]
    public async Task UpsertDeck_ReturnsBadRequest_WhenHeaderMissing()
    {
        var handler = new DeckHandler(Mock.Of<IDeckRepository>(), Mock.Of<ICardRepository>());
        var controller = new DecksController(handler)
        {
            ControllerContext = ControllerContextFactory.WithoutOwner()
        };

        var result = await controller.UpsertDeck(new UpsertDeckRequest { Name = "Test" }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpsertDeck_CreatesDeck_WhenNew()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new DeckRepository(dbContext);
        var cardRepository = new CardRepository(dbContext);
        var handler = new DeckHandler(repository, cardRepository);
        var ownerId = Guid.NewGuid();
        var controller = new DecksController(handler)
        {
            ControllerContext = ControllerContextFactory.WithOwner(ownerId)
        };

        var request = new UpsertDeckRequest
        {
            Id = Guid.NewGuid(),
            Name = "New Deck"
        };

        var result = await controller.UpsertDeck(request, CancellationToken.None);

        var ok = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status200OK, ok.StatusCode);
        var returnedDeck = Assert.IsType<Deck>(ok.Value);
        Assert.Equal("New Deck", returnedDeck.Name);
    }

    [Fact]
    public async Task SoftDeleteDeck_ReturnsBadRequest_WhenHeaderMissing()
    {
        var handler = new DeckHandler(Mock.Of<IDeckRepository>(), Mock.Of<ICardRepository>());
        var controller = new DecksController(handler)
        {
            ControllerContext = ControllerContextFactory.WithoutOwner()
        };

        var result = await controller.SoftDeleteDeck(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task SoftDeleteDeck_ReturnsNoContent_WhenSuccessful()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new DeckRepository(dbContext);
        var cardRepository = new CardRepository(dbContext);
        var handler = new DeckHandler(repository, cardRepository);
        var ownerId = Guid.NewGuid();
        var deckId = Guid.NewGuid();
        var controller = new DecksController(handler)
        {
            ControllerContext = ControllerContextFactory.WithOwner(ownerId)
        };

        await repository.UpsertAsync(new Deck
        {
            Id = deckId,
            OwnerId = ownerId,
            Name = "To Delete"
        }, CancellationToken.None);

        var result = await controller.SoftDeleteDeck(deckId, CancellationToken.None);

        var statusResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status204NoContent, statusResult.StatusCode);
    }
}
