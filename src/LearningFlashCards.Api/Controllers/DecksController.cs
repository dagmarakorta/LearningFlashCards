using LearningFlashCards.Api.Services;
using LearningFlashCards.Core.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace LearningFlashCards.Api.Controllers;

[Route("api/[controller]")]
public class DecksController : ApiControllerBase
{
    private readonly DeckHandler _deckHandler;

    public DecksController(DeckHandler deckHandler)
    {
        _deckHandler = deckHandler;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Deck>>> GetDecks(CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return BadRequest("Missing X-Owner-Id header.");
        }

        var result = await _deckHandler.GetDecksAsync(ownerId, cancellationToken);
        return StatusCode(result.StatusCode, result.Value);
    }

    [HttpGet("{deckId:guid}")]
    public async Task<ActionResult<Deck>> GetDeck(Guid deckId, CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return BadRequest("Missing X-Owner-Id header.");
        }

        var result = await _deckHandler.GetDeckAsync(deckId, ownerId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpPost]
    public async Task<ActionResult<Deck>> UpsertDeck([FromBody] Deck deck, CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return BadRequest("Missing X-Owner-Id header.");
        }

        var result = await _deckHandler.UpsertDeckAsync(deck, ownerId, cancellationToken);
        return ToActionResult(result);
    }

    [HttpDelete("{deckId:guid}")]
    public async Task<IActionResult> SoftDeleteDeck(Guid deckId, CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return BadRequest("Missing X-Owner-Id header.");
        }

        var result = await _deckHandler.SoftDeleteDeckAsync(deckId, ownerId, cancellationToken);
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
