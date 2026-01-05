using LearningFlashCards.Core.Application.Abstractions.Repositories;
using LearningFlashCards.Core.Domain.Entities;
using LearningFlashCards.Core.Domain.Sync;
using Microsoft.EntityFrameworkCore;

namespace LearningFlashCards.Infrastructure.Persistence.Repositories;

public class CardRepository : ICardRepository
{
    private readonly AppDbContext _db;

    public CardRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Card>> GetByDeckAsync(Guid deckId, CancellationToken cancellationToken)
    {
        return await _db.Cards
            .AsNoTracking()
            .Where(c => c.DeckId == deckId && c.DeletedAt == null)
            .ToListAsync(cancellationToken);
    }

    public async Task<Card?> GetAsync(Guid cardId, CancellationToken cancellationToken)
    {
        return await _db.Cards
            .Include(c => c.Tags)
            .FirstOrDefaultAsync(c => c.Id == cardId && c.DeletedAt == null, cancellationToken);
    }

    public async Task UpsertAsync(Card card, CancellationToken cancellationToken)
    {
        var exists = await _db.Cards.AsNoTracking().AnyAsync(c => c.Id == card.Id, cancellationToken);
        if (exists)
        {
            _db.Cards.Update(card);
        }
        else
        {
            _db.Cards.Add(card);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SoftDeleteAsync(Guid cardId, DateTimeOffset deletedAt, CancellationToken cancellationToken)
    {
        var card = await _db.Cards.FirstOrDefaultAsync(c => c.Id == cardId, cancellationToken);
        if (card is null)
        {
            return;
        }

        card.DeletedAt = deletedAt;
        card.ModifiedAt = deletedAt;

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SyncChange<Card>>> GetChangesSinceAsync(string? syncToken, Guid ownerId, CancellationToken cancellationToken)
    {
        var since = SyncTokenHelper.Parse(syncToken);

        var query =
            from card in _db.Cards.AsNoTracking()
            join deck in _db.Decks.AsNoTracking() on card.DeckId equals deck.Id
            where deck.OwnerId == ownerId
            select card;

        if (since.HasValue)
        {
            query = query.Where(c =>
                c.ModifiedAt > since.Value ||
                (c.DeletedAt != null && c.DeletedAt > since.Value));
        }

        var cards = await query.ToListAsync(cancellationToken);

        return cards
            .Select(c => new SyncChange<Card>(
                c.DeletedAt.HasValue ? SyncOperation.Delete : SyncOperation.Upsert,
                c,
                null))
            .ToList();
    }

    public async Task<string?> SaveChangesAsync(IEnumerable<SyncChange<Card>> changes, Guid ownerId, CancellationToken cancellationToken)
    {
        foreach (var change in changes)
        {
            var card = change.Entity;

            var ownsDeck = await _db.Decks.AnyAsync(d => d.Id == card.DeckId && d.OwnerId == ownerId, cancellationToken);
            if (!ownsDeck)
            {
                continue;
            }

            if (change.Operation == SyncOperation.Delete)
            {
                var existing = await _db.Cards.FirstOrDefaultAsync(c => c.Id == card.Id, cancellationToken);
                if (existing != null)
                {
                    existing.DeletedAt = card.DeletedAt ?? DateTimeOffset.UtcNow;
                    existing.ModifiedAt = existing.DeletedAt.Value;
                }
                else
                {
                    card.DeletedAt ??= DateTimeOffset.UtcNow;
                    card.ModifiedAt = card.DeletedAt.Value;
                    _db.Cards.Add(card);
                }
            }
            else
            {
                var exists = await _db.Cards.AsNoTracking().AnyAsync(c => c.Id == card.Id, cancellationToken);
                if (exists)
                {
                    _db.Cards.Update(card);
                }
                else
                {
                    _db.Cards.Add(card);
                }
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return SyncTokenHelper.NewToken();
    }
}
