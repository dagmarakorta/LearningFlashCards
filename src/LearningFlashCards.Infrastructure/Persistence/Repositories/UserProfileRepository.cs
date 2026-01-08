using LearningFlashCards.Core.Application.Abstractions.Repositories;
using LearningFlashCards.Core.Domain.Entities;
using LearningFlashCards.Core.Domain.Sync;
using Microsoft.EntityFrameworkCore;

namespace LearningFlashCards.Infrastructure.Persistence.Repositories;

public class UserProfileRepository : IUserProfileRepository
{
    private readonly AppDbContext _db;

    public UserProfileRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalized = email.ToLower();
        return await _db.Users.AnyAsync(u => u.Email.ToLower() == normalized, cancellationToken);
    }

    public async Task<bool> ExistsByDisplayNameAsync(string displayName, CancellationToken cancellationToken)
    {
        var normalized = displayName.ToLower();
        return await _db.Users.AnyAsync(u => u.DisplayName.ToLower() == normalized, cancellationToken);
    }

    public async Task<UserProfile?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var normalized = email.ToLower();
        return await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalized && u.DeletedAt == null, cancellationToken);
    }

    public async Task<UserProfile?> GetByDisplayNameAsync(string displayName, CancellationToken cancellationToken)
    {
        var normalized = displayName.ToLower();
        return await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.DisplayName.ToLower() == normalized && u.DeletedAt == null, cancellationToken);
    }

    public async Task<UserProfile?> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null, cancellationToken);
    }

    public async Task<UserProfile> CreateAsync(UserProfile profile, CancellationToken cancellationToken)
    {
        _db.Users.Add(profile);
        await _db.SaveChangesAsync(cancellationToken);

        return profile;
    }

    public async Task UpsertAsync(UserProfile profile, CancellationToken cancellationToken)
    {
        var tracked = _db.Users.Local.FirstOrDefault(u => u.Id == profile.Id);
        if (tracked is not null && !ReferenceEquals(tracked, profile))
        {
            _db.Entry(tracked).CurrentValues.SetValues(profile);
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        var exists = await _db.Users.AsNoTracking().AnyAsync(u => u.Id == profile.Id, cancellationToken);
        if (exists)
        {
            _db.Users.Update(profile);
        }
        else
        {
            _db.Users.Add(profile);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SyncChange<UserProfile>>> GetChangesSinceAsync(string? syncToken, Guid ownerId, CancellationToken cancellationToken)
    {
        var since = SyncTokenHelper.Parse(syncToken);

        var query = _db.Users.AsNoTracking().Where(u => u.Id == ownerId);
        if (since.HasValue)
        {
            query = query.Where(u =>
                u.ModifiedAt > since.Value ||
                (u.DeletedAt != null && u.DeletedAt > since.Value));
        }

        var users = await query.ToListAsync(cancellationToken);

        return users
            .Select(u => new SyncChange<UserProfile>(
                u.DeletedAt.HasValue ? SyncOperation.Delete : SyncOperation.Upsert,
                u,
                null))
            .ToList();
    }

    public async Task<string?> SaveChangesAsync(IEnumerable<SyncChange<UserProfile>> changes, Guid ownerId, CancellationToken cancellationToken)
    {
        foreach (var change in changes)
        {
            var profile = change.Entity;
            profile.Id = ownerId;

            if (change.Operation == SyncOperation.Delete)
            {
                var existing = await _db.Users.FirstOrDefaultAsync(u => u.Id == profile.Id, cancellationToken);
                if (existing != null)
                {
                    existing.DeletedAt = profile.DeletedAt ?? DateTimeOffset.UtcNow;
                    existing.ModifiedAt = existing.DeletedAt.Value;
                }
                else
                {
                    profile.DeletedAt ??= DateTimeOffset.UtcNow;
                    profile.ModifiedAt = profile.DeletedAt.Value;
                    _db.Users.Add(profile);
                }
            }
            else
            {
                var exists = await _db.Users.AsNoTracking().AnyAsync(u => u.Id == profile.Id, cancellationToken);
                if (exists)
                {
                    _db.Users.Update(profile);
                }
                else
                {
                    _db.Users.Add(profile);
                }
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return SyncTokenHelper.NewToken();
    }
}
