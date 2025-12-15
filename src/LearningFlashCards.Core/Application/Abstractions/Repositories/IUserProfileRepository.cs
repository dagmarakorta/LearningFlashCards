using LearningFlashCards.Core.Domain.Entities;
using LearningFlashCards.Core.Domain.Sync;

namespace LearningFlashCards.Core.Application.Abstractions.Repositories;

public interface IUserProfileRepository
{
    Task<UserProfile?> GetAsync(Guid userId, CancellationToken cancellationToken);
    Task UpsertAsync(UserProfile profile, CancellationToken cancellationToken);
    Task<IReadOnlyList<SyncChange<UserProfile>>> GetChangesSinceAsync(string? syncToken, Guid ownerId, CancellationToken cancellationToken);
    Task<string?> SaveChangesAsync(IEnumerable<SyncChange<UserProfile>> changes, Guid ownerId, CancellationToken cancellationToken);
}
