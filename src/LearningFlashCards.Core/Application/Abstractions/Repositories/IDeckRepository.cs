using LearningFlashCards.Core.Domain.Entities;
using LearningFlashCards.Core.Domain.Sync;

namespace LearningFlashCards.Core.Application.Abstractions.Repositories;

public interface IDeckRepository
{
    Task<IReadOnlyList<Deck>> GetByOwnerAsync(Guid ownerId, CancellationToken cancellationToken);
    Task<Deck?> GetAsync(Guid deckId, CancellationToken cancellationToken);
    Task UpsertAsync(Deck deck, CancellationToken cancellationToken);
    Task SoftDeleteAsync(Guid deckId, DateTimeOffset deletedAt, CancellationToken cancellationToken);
    Task<IReadOnlyList<SyncChange<Deck>>> GetChangesSinceAsync(string? syncToken, Guid ownerId, CancellationToken cancellationToken);
    Task<string?> SaveChangesAsync(IEnumerable<SyncChange<Deck>> changes, Guid ownerId, CancellationToken cancellationToken);
}
