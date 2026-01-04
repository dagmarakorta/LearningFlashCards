using LearningFlashCards.Core.Application.Abstractions.Repositories;
using LearningFlashCards.Core.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace LearningFlashCards.Api.Services;

public class CardsHandler
{
    private readonly ICardRepository _cardRepository;
    private readonly IDeckRepository _deckRepository;

    public CardsHandler(ICardRepository cardRepository, IDeckRepository deckRepository)
    {
        _cardRepository = cardRepository;
        _deckRepository = deckRepository;
    }

    public async Task<HandlerResult<IReadOnlyList<Card>>> GetCardsAsync(Guid deckId, Guid ownerId, CancellationToken cancellationToken)
    {
        var deckResult = await EnsureDeckOwnedAsync(deckId, ownerId, cancellationToken);
        if (!deckResult.IsSuccess)
        {
            return HandlerResult<IReadOnlyList<Card>>.NotFound();
        }

        var cards = await _cardRepository.GetByDeckAsync(deckId, cancellationToken);
        return HandlerResult<IReadOnlyList<Card>>.Success(cards);
    }

    public async Task<HandlerResult<Card>> GetCardAsync(Guid deckId, Guid cardId, Guid ownerId, CancellationToken cancellationToken)
    {
        var deckResult = await EnsureDeckOwnedAsync(deckId, ownerId, cancellationToken);
        if (!deckResult.IsSuccess)
        {
            return HandlerResult<Card>.NotFound();
        }

        var card = await _cardRepository.GetAsync(cardId, cancellationToken);
        if (card is null || card.DeletedAt != null || card.DeckId != deckId)
        {
            return HandlerResult<Card>.NotFound();
        }

        return HandlerResult<Card>.Success(card);
    }

    public async Task<HandlerResult<Card>> UpsertCardAsync(Guid deckId, Card card, Guid ownerId, CancellationToken cancellationToken)
    {
        var deckResult = await EnsureDeckOwnedAsync(deckId, ownerId, cancellationToken);
        if (!deckResult.IsSuccess)
        {
            return HandlerResult<Card>.NotFound();
        }

        card.DeckId = deckId;
        card.ModifiedAt = DateTimeOffset.UtcNow;

        await _cardRepository.UpsertAsync(card, cancellationToken);
        return HandlerResult<Card>.Success(card);
    }

    public async Task<HandlerResult<string?>> SoftDeleteCardAsync(Guid deckId, Guid cardId, Guid ownerId, CancellationToken cancellationToken)
    {
        var deckResult = await EnsureDeckOwnedAsync(deckId, ownerId, cancellationToken);
        if (!deckResult.IsSuccess)
        {
            return HandlerResult<string?>.NotFound();
        }

        var card = await _cardRepository.GetAsync(cardId, cancellationToken);
        if (card is null || card.DeletedAt != null || card.DeckId != deckId)
        {
            return HandlerResult<string?>.NotFound();
        }

        var deletedAt = DateTimeOffset.UtcNow;
        await _cardRepository.SoftDeleteAsync(cardId, deletedAt, cancellationToken);
        return HandlerResult<string?>.Success(null, StatusCodes.Status204NoContent);
    }

    private async Task<HandlerResult<string?>> EnsureDeckOwnedAsync(Guid deckId, Guid ownerId, CancellationToken cancellationToken)
    {
        var deck = await _deckRepository.GetAsync(deckId, cancellationToken);
        if (deck is null || deck.DeletedAt != null || deck.OwnerId != ownerId)
        {
            return HandlerResult<string?>.NotFound();
        }

        return HandlerResult<string?>.Success(null);
    }
}
