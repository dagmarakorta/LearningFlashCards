using LearningFlashCards.Core.Domain.Entities;
using LearningFlashCards.Core.Domain.Sync;

namespace LearningFlashCards.Core.Application.Abstractions.Repositories;

public interface ICardRepository
{
    Task<IReadOnlyList<Card>> GetByDeckAsync(Guid deckId, CancellationToken cancellationToken);
    Task<Card?> GetAsync(Guid cardId, CancellationToken cancellationToken);
    Task UpsertAsync(Card card, CancellationToken cancellationToken);
    Task SoftDeleteAsync(Guid cardId, DateTimeOffset deletedAt, CancellationToken cancellationToken);
    Task SoftDeleteByDeckAsync(Guid deckId, DateTimeOffset deletedAt, CancellationToken cancellationToken);
    Task<IReadOnlyList<SyncChange<Card>>> GetChangesSinceAsync(string? syncToken, Guid ownerId, CancellationToken cancellationToken);
    Task<string?> SaveChangesAsync(IEnumerable<SyncChange<Card>> changes, Guid ownerId, CancellationToken cancellationToken);
}
