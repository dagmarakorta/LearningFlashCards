using LearningFlashCards.Core.Application.Abstractions.Repositories;
using LearningFlashCards.Core.Domain.Entities;
using LearningFlashCards.Core.Domain.Sync;
using Microsoft.EntityFrameworkCore;

namespace LearningFlashCards.Infrastructure.Persistence.Repositories;

public class TagRepository : ITagRepository
{
    private readonly AppDbContext _db;

    public TagRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Tag>> GetByOwnerAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        return await _db.Tags
            .AsNoTracking()
            .Where(t => t.OwnerId == ownerId && t.DeletedAt == null)
            .ToListAsync(cancellationToken);
    }

    public async Task<Tag?> GetAsync(Guid tagId, CancellationToken cancellationToken)
    {
        return await _db.Tags
            .FirstOrDefaultAsync(t => t.Id == tagId && t.DeletedAt == null, cancellationToken);
    }

    public async Task UpsertAsync(Tag tag, CancellationToken cancellationToken)
    {
        _db.Tags.Update(tag);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SoftDeleteAsync(Guid tagId, DateTimeOffset deletedAt, CancellationToken cancellationToken)
    {
        var tag = await _db.Tags.FirstOrDefaultAsync(t => t.Id == tagId, cancellationToken);
        if (tag is null)
        {
            return;
        }

        tag.DeletedAt = deletedAt;
        tag.ModifiedAt = deletedAt;

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SyncChange<Tag>>> GetChangesSinceAsync(string? syncToken, Guid ownerId, CancellationToken cancellationToken)
    {
        var since = SyncTokenHelper.Parse(syncToken);

        var query = _db.Tags.AsNoTracking().Where(t => t.OwnerId == ownerId);
        if (since.HasValue)
        {
            query = query.Where(t =>
                t.ModifiedAt > since.Value ||
                (t.DeletedAt != null && t.DeletedAt > since.Value));
        }

        var tags = await query.ToListAsync(cancellationToken);

        return tags
            .Select(t => new SyncChange<Tag>(
                t.DeletedAt.HasValue ? SyncOperation.Delete : SyncOperation.Upsert,
                t,
                null))
            .ToList();
    }

    public async Task<string?> SaveChangesAsync(IEnumerable<SyncChange<Tag>> changes, Guid ownerId, CancellationToken cancellationToken)
    {
        foreach (var change in changes)
        {
            var tag = change.Entity;
            tag.OwnerId = ownerId;

            if (change.Operation == SyncOperation.Delete)
            {
                var existing = await _db.Tags.FirstOrDefaultAsync(t => t.Id == tag.Id, cancellationToken);
                if (existing != null)
                {
                    existing.DeletedAt = tag.DeletedAt ?? DateTimeOffset.UtcNow;
                    existing.ModifiedAt = existing.DeletedAt.Value;
                }
                else
                {
                    tag.DeletedAt ??= DateTimeOffset.UtcNow;
                    tag.ModifiedAt = tag.DeletedAt.Value;
                    _db.Tags.Add(tag);
                }
            }
            else
            {
                _db.Tags.Update(tag);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return SyncTokenHelper.NewToken();
    }
}
