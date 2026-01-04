using LearningFlashCards.Core.Application.Abstractions.Repositories;
using LearningFlashCards.Core.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace LearningFlashCards.Api.Controllers;

[Route("api/decks/{deckId:guid}/[controller]")]
public class CardsController : ApiControllerBase
{
    private readonly ICardRepository _cardRepository;
    private readonly IDeckRepository _deckRepository;

    public CardsController(ICardRepository cardRepository, IDeckRepository deckRepository)
    {
        _cardRepository = cardRepository;
        _deckRepository = deckRepository;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Card>>> GetCards(Guid deckId, CancellationToken cancellationToken)
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

        var cards = await _cardRepository.GetByDeckAsync(deckId, cancellationToken);
        return Ok(cards);
    }

    [HttpGet("{cardId:guid}")]
    public async Task<ActionResult<Card>> GetCard(Guid deckId, Guid cardId, CancellationToken cancellationToken)
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

        var card = await _cardRepository.GetAsync(cardId, cancellationToken);
        if (card is null || card.DeletedAt != null || card.DeckId != deckId)
        {
            return NotFound();
        }

        return Ok(card);
    }

    [HttpPost]
    public async Task<ActionResult<Card>> UpsertCard(Guid deckId, [FromBody] Card card, CancellationToken cancellationToken)
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

        card.DeckId = deckId;
        card.ModifiedAt = DateTimeOffset.UtcNow;

        await _cardRepository.UpsertAsync(card, cancellationToken);
        return Ok(card);
    }

    [HttpDelete("{cardId:guid}")]
    public async Task<IActionResult> SoftDeleteCard(Guid deckId, Guid cardId, CancellationToken cancellationToken)
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

        var card = await _cardRepository.GetAsync(cardId, cancellationToken);
        if (card is null || card.DeletedAt != null || card.DeckId != deckId)
        {
            return NotFound();
        }

        var deletedAt = DateTimeOffset.UtcNow;
        await _cardRepository.SoftDeleteAsync(cardId, deletedAt, cancellationToken);
        return NoContent();
    }
}
