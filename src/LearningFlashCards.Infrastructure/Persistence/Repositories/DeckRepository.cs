using LearningFlashCards.Core.Application.Abstractions.Repositories;
using LearningFlashCards.Core.Domain.Entities;
using LearningFlashCards.Core.Domain.Sync;
using Microsoft.EntityFrameworkCore;

namespace LearningFlashCards.Infrastructure.Persistence.Repositories;

public class DeckRepository : IDeckRepository
{
    private readonly AppDbContext _db;

    public DeckRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Deck>> GetByOwnerAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        return await _db.Decks
            .AsNoTracking()
            .Where(d => d.OwnerId == ownerId && d.DeletedAt == null)
            .ToListAsync(cancellationToken);
    }

    public async Task<Deck?> GetAsync(Guid deckId, CancellationToken cancellationToken)
    {
        return await _db.Decks
            .Include(d => d.Cards)
            .Include(d => d.Tags)
            .FirstOrDefaultAsync(d => d.Id == deckId && d.DeletedAt == null, cancellationToken);
    }

    public async Task UpsertAsync(Deck deck, CancellationToken cancellationToken)
    {
        var exists = await _db.Decks.AsNoTracking().AnyAsync(d => d.Id == deck.Id, cancellationToken);
        if (exists)
        {
            _db.Decks.Update(deck);
        }
        else
        {
            _db.Decks.Add(deck);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SoftDeleteAsync(Guid deckId, DateTimeOffset deletedAt, CancellationToken cancellationToken)
    {
        var deck = await _db.Decks.FirstOrDefaultAsync(d => d.Id == deckId, cancellationToken);
        if (deck is null)
        {
            return;
        }

        deck.DeletedAt = deletedAt;
        deck.ModifiedAt = deletedAt;

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SyncChange<Deck>>> GetChangesSinceAsync(string? syncToken, Guid ownerId, CancellationToken cancellationToken)
    {
        var since = SyncTokenHelper.Parse(syncToken);

        var query = _db.Decks.AsNoTracking().Where(d => d.OwnerId == ownerId);
        if (since.HasValue)
        {
            query = query.Where(d =>
                d.ModifiedAt > since.Value ||
                (d.DeletedAt != null && d.DeletedAt > since.Value));
        }

        var decks = await query.ToListAsync(cancellationToken);

        return decks
            .Select(d => new SyncChange<Deck>(
                d.DeletedAt.HasValue ? SyncOperation.Delete : SyncOperation.Upsert,
                d,
                null))
            .ToList();
    }

    public async Task<string?> SaveChangesAsync(IEnumerable<SyncChange<Deck>> changes, Guid ownerId, CancellationToken cancellationToken)
    {
        foreach (var change in changes)
        {
            var deck = change.Entity;
            if (deck.OwnerId != Guid.Empty && deck.OwnerId != ownerId)
            {
                continue;
            }

            deck.OwnerId = ownerId;

            if (change.Operation == SyncOperation.Delete)
            {
                var existing = await _db.Decks.FirstOrDefaultAsync(d => d.Id == deck.Id, cancellationToken);
                if (existing != null)
                {
                    existing.DeletedAt = deck.DeletedAt ?? DateTimeOffset.UtcNow;
                    existing.ModifiedAt = existing.DeletedAt.Value;
                }
                else
                {
                    deck.DeletedAt ??= DateTimeOffset.UtcNow;
                    deck.ModifiedAt = deck.DeletedAt.Value;
                    _db.Decks.Add(deck);
                }
            }
            else
            {
                var exists = await _db.Decks.AsNoTracking().AnyAsync(d => d.Id == deck.Id, cancellationToken);
                if (exists)
                {
                    _db.Decks.Update(deck);
                }
                else
                {
                    _db.Decks.Add(deck);
                }
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return SyncTokenHelper.NewToken();
    }
}
