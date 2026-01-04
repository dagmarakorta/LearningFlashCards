using LearningFlashCards.Core.Application.Abstractions.Repositories;
using LearningFlashCards.Core.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace LearningFlashCards.Api.Controllers;

[Route("api/[controller]")]
public class DecksController : ApiControllerBase
{
    private readonly IDeckRepository _deckRepository;

    public DecksController(IDeckRepository deckRepository)
    {
        _deckRepository = deckRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Deck>>> GetDecks(CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return BadRequest("Missing X-Owner-Id header.");
        }

        var decks = await _deckRepository.GetByOwnerAsync(ownerId, cancellationToken);
        return Ok(decks);
    }

    [HttpGet("{deckId:guid}")]
    public async Task<ActionResult<Deck>> GetDeck(Guid deckId, CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return BadRequest("Missing X-Owner-Id header.");
        }

        var deck = await _deckRepository.GetAsync(deckId, cancellationToken);
        if (deck is null || deck.DeletedAt != null || deck.OwnerId != ownerId)
        {
            return NotFound();
        }

        return Ok(deck);
    }

    [HttpPost]
    public async Task<ActionResult<Deck>> UpsertDeck([FromBody] Deck deck, CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return BadRequest("Missing X-Owner-Id header.");
        }

        if (deck.OwnerId == Guid.Empty)
        {
            deck.OwnerId = ownerId;
        }
        else if (deck.OwnerId != ownerId)
        {
            return Forbid();
        }

        deck.ModifiedAt = DateTimeOffset.UtcNow;
        await _deckRepository.UpsertAsync(deck, cancellationToken);
        return Ok(deck);
    }

    [HttpDelete("{deckId:guid}")]
    public async Task<IActionResult> SoftDeleteDeck(Guid deckId, CancellationToken cancellationToken)
    {
        if (!TryGetOwnerId(out var ownerId))
        {
            return BadRequest("Missing X-Owner-Id header.");
        }

        var existing = await _deckRepository.GetAsync(deckId, cancellationToken);
        if (existing is null || existing.DeletedAt != null || existing.OwnerId != ownerId)
        {
            return NotFound();
        }

        var deletedAt = DateTimeOffset.UtcNow;
        await _deckRepository.SoftDeleteAsync(deckId, deletedAt, cancellationToken);
        return NoContent();
    }
}
