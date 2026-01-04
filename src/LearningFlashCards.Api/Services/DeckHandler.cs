using LearningFlashCards.Core.Application.Abstractions.Repositories;
using LearningFlashCards.Core.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace LearningFlashCards.Api.Services;

public class DeckHandler
{
    private readonly IDeckRepository _deckRepository;

    public DeckHandler(IDeckRepository deckRepository)
    {
        _deckRepository = deckRepository;
    }

    public async Task<HandlerResult<IReadOnlyList<Deck>>> GetDecksAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        var decks = await _deckRepository.GetByOwnerAsync(ownerId, cancellationToken);
        return HandlerResult<IReadOnlyList<Deck>>.Success(decks);
    }

    public async Task<HandlerResult<Deck>> GetDeckAsync(Guid deckId, Guid ownerId, CancellationToken cancellationToken)
    {
        var deck = await _deckRepository.GetAsync(deckId, cancellationToken);
        if (deck is null || deck.DeletedAt != null || deck.OwnerId != ownerId)
        {
            return HandlerResult<Deck>.NotFound();
        }

        return HandlerResult<Deck>.Success(deck);
    }

    public async Task<HandlerResult<Deck>> UpsertDeckAsync(Deck deck, Guid ownerId, CancellationToken cancellationToken)
    {
        if (deck.OwnerId == Guid.Empty)
        {
            deck.OwnerId = ownerId;
        }
        else if (deck.OwnerId != ownerId)
        {
            return HandlerResult<Deck>.Forbidden();
        }

        deck.ModifiedAt = DateTimeOffset.UtcNow;
        await _deckRepository.UpsertAsync(deck, cancellationToken);
        return HandlerResult<Deck>.Success(deck);
    }

    public async Task<HandlerResult<string?>> SoftDeleteDeckAsync(Guid deckId, Guid ownerId, CancellationToken cancellationToken)
    {
        var existing = await _deckRepository.GetAsync(deckId, cancellationToken);
        if (existing is null || existing.DeletedAt != null || existing.OwnerId != ownerId)
        {
            return HandlerResult<string?>.NotFound();
        }

        var deletedAt = DateTimeOffset.UtcNow;
        await _deckRepository.SoftDeleteAsync(deckId, deletedAt, cancellationToken);
        return HandlerResult<string?>.Success(null, StatusCodes.Status204NoContent);
    }
}
