using LearningFlashCards.Core.Domain.Entities;
using LearningFlashCards.Core.Domain.Sync;

namespace LearningFlashCards.Core.Application.Abstractions.Repositories;

public interface ITagRepository
{
    Task<IReadOnlyList<Tag>> GetByOwnerAsync(Guid ownerId, CancellationToken cancellationToken);
    Task<Tag?> GetAsync(Guid tagId, CancellationToken cancellationToken);
    Task UpsertAsync(Tag tag, CancellationToken cancellationToken);
    Task SoftDeleteAsync(Guid tagId, DateTimeOffset deletedAt, CancellationToken cancellationToken);
    Task<IReadOnlyList<SyncChange<Tag>>> GetChangesSinceAsync(string? syncToken, Guid ownerId, CancellationToken cancellationToken);
    Task<string?> SaveChangesAsync(IEnumerable<SyncChange<Tag>> changes, Guid ownerId, CancellationToken cancellationToken);
}
