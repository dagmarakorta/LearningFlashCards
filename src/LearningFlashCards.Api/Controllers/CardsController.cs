using LearningFlashCards.Api.Services;
using LearningFlashCards.Core.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace LearningFlashCards.Api.Controllers;

[Route("api/decks/{deckId:guid}/[controller]")]
public class CardsController : ApiControllerBase
{
    private readonly CardsHandler _cardsHandler;

    public CardsController(CardsHandler cardsHandler)
    {
        _cardsHandler = cardsHandler;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Card>>> GetCards(Guid deckId, CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return BadRequest("Missing X-Owner-Id header.");
        }

        var result = await _cardsHandler.GetCardsAsync(deckId, ownerId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpGet("{cardId:guid}")]
    public async Task<ActionResult<Card>> GetCard(Guid deckId, Guid cardId, CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return BadRequest("Missing X-Owner-Id header.");
        }

        var result = await _cardsHandler.GetCardAsync(deckId, cardId, ownerId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<ActionResult<Card>> UpsertCard(Guid deckId, [FromBody] Card card, CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return BadRequest("Missing X-Owner-Id header.");
        }

        var result = await _cardsHandler.UpsertCardAsync(deckId, card, ownerId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpDelete("{cardId:guid}")]
    public async Task<IActionResult> SoftDeleteCard(Guid deckId, Guid cardId, CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return BadRequest("Missing X-Owner-Id header.");
        }

        var result = await _cardsHandler.SoftDeleteCardAsync(deckId, cardId, ownerId, cancellationToken);
        return result.IsSuccess ? StatusCode(result.StatusCode) : StatusCode(result.StatusCode, result.Error);
    }

    private ActionResult ToActionResult<T>(HandlerResult<T> result)
    {
        if (!result.IsSuccess)
        {
            return StatusCode(result.StatusCode, result.Error);
        }

        return StatusCode(result.StatusCode, result.Value);
    }
}
