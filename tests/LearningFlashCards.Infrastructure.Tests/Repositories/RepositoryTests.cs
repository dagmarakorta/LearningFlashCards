using LearningFlashCards.Core.Domain.Entities;
using LearningFlashCards.Infrastructure.Persistence.Repositories;
using LearningFlashCards.Infrastructure.Tests.TestUtilities;
using LearningFlashCards.Core.Domain.Sync;

namespace LearningFlashCards.Infrastructure.Tests.Repositories;

public class RepositoryTests
{
    [Fact]
    public async Task DeckRepository_Upsert_InsertsAndUpdates()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new DeckRepository(dbContext);
        var deckId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        var deck = new Deck
        {
            Id = deckId,
            OwnerId = ownerId,
            Name = "Initial",
            Description = "Desc"
        };

        await repository.UpsertAsync(deck, CancellationToken.None);

        var inserted = await repository.GetAsync(deckId, CancellationToken.None);
        Assert.NotNull(inserted);
        Assert.Equal("Initial", inserted!.Name);
        Assert.NotEmpty(inserted.RowVersion);

        inserted.Name = "Updated";
        await repository.UpsertAsync(inserted, CancellationToken.None);

        var updated = await repository.GetAsync(deckId, CancellationToken.None);
        Assert.Equal("Updated", updated!.Name);
        Assert.NotEqual(inserted.RowVersion, updated.RowVersion);
    }

    [Fact]
    public async Task TagRepository_Upsert_InsertsAndUpdates()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new TagRepository(dbContext);
        var tagId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        var tag = new Tag
        {
            Id = tagId,
            OwnerId = ownerId,
            Name = "Tag"
        };

        await repository.UpsertAsync(tag, CancellationToken.None);

        var inserted = await repository.GetAsync(tagId, CancellationToken.None);
        Assert.NotNull(inserted);
        Assert.NotEmpty(inserted!.RowVersion);

        inserted.Name = "Updated Tag";
        await repository.UpsertAsync(inserted, CancellationToken.None);

        var updated = await repository.GetAsync(tagId, CancellationToken.None);
        Assert.Equal("Updated Tag", updated!.Name);
        Assert.NotEqual(inserted.RowVersion, updated.RowVersion);
    }

    [Fact]
    public async Task CardRepository_SoftDelete_SetsDeletedAt()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var deckRepository = new DeckRepository(dbContext);
        var cardRepository = new CardRepository(dbContext);
        var deckId = Guid.NewGuid();
        var cardId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        await deckRepository.UpsertAsync(new Deck
        {
            Id = deckId,
            OwnerId = ownerId,
            Name = "Deck"
        }, CancellationToken.None);

        var card = new Card
        {
            Id = cardId,
            DeckId = deckId,
            Front = "Front",
            Back = "Back",
            State = new CardState
            {
                DueAt = DateTimeOffset.UtcNow,
                IntervalDays = 1,
                EaseFactor = 2.5,
                Streak = 0,
                Lapses = 0
            }
        };

        await cardRepository.UpsertAsync(card, CancellationToken.None);

        var deletedAt = DateTimeOffset.UtcNow;
        await cardRepository.SoftDeleteAsync(cardId, deletedAt, CancellationToken.None);

        var stored = await cardRepository.GetAsync(cardId, CancellationToken.None);
        Assert.NotNull(stored);
        Assert.NotNull(stored!.DeletedAt);
        Assert.True(stored.DeletedAt >= deletedAt.AddSeconds(-1));
    }

    [Fact]
    public async Task TagRepository_SoftDelete_SetsDeletedAt()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new TagRepository(dbContext);
        var tagId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        await repository.UpsertAsync(new Tag
        {
            Id = tagId,
            OwnerId = ownerId,
            Name = "Tag"
        }, CancellationToken.None);

        var deletedAt = DateTimeOffset.UtcNow;
        await repository.SoftDeleteAsync(tagId, deletedAt, CancellationToken.None);

        var stored = await repository.GetAsync(tagId, CancellationToken.None);
        Assert.NotNull(stored);
        Assert.NotNull(stored!.DeletedAt);
        Assert.True(stored.DeletedAt >= deletedAt.AddSeconds(-1));
    }

    [Fact]
    public async Task CardRepository_GetByDeck_FiltersDeleted()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var deckId = Guid.NewGuid();
        var repository = new CardRepository(dbContext);

        dbContext.Cards.AddRange(
            new Card
            {
                Id = Guid.NewGuid(),
                DeckId = deckId,
                Front = "Front",
                Back = "Back",
                State = new CardState
                {
                    DueAt = DateTimeOffset.UtcNow,
                    IntervalDays = 1,
                    EaseFactor = 2.5,
                    Streak = 0,
                    Lapses = 0
                },
                DeletedAt = null
            },
            new Card
            {
                Id = Guid.NewGuid(),
                DeckId = deckId,
                Front = "Deleted",
                Back = "Deleted",
                State = new CardState
                {
                    DueAt = DateTimeOffset.UtcNow,
                    IntervalDays = 1,
                    EaseFactor = 2.5,
                    Streak = 0,
                    Lapses = 0
                },
                DeletedAt = DateTimeOffset.UtcNow
            });
        await dbContext.SaveChangesAsync();

        var cards = await repository.GetByDeckAsync(deckId, CancellationToken.None);

        Assert.Single(cards);
        Assert.Equal("Front", cards.First().Front);
    }

    [Fact]
    public async Task DeckRepository_SaveChanges_SkipsForeignOwners()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new DeckRepository(dbContext);
        var ownerId = Guid.NewGuid();
        var otherOwnerId = Guid.NewGuid();

        var changes = new[]
        {
            new SyncChange<Deck>(SyncOperation.Upsert, new Deck
            {
                Id = Guid.NewGuid(),
                OwnerId = otherOwnerId,
                Name = "Other Owner",
                RowVersion = Guid.NewGuid().ToByteArray()
            }, null),
            new SyncChange<Deck>(SyncOperation.Upsert, new Deck
            {
                Id = Guid.NewGuid(),
                OwnerId = ownerId,
                Name = "My Deck",
                RowVersion = Guid.NewGuid().ToByteArray()
            }, null)
        };

        await repository.SaveChangesAsync(changes, ownerId, CancellationToken.None);

        var allDecks = await repository.GetByOwnerAsync(ownerId, CancellationToken.None);
        Assert.Single(allDecks);
        Assert.Equal("My Deck", allDecks.First().Name);
    }

    [Fact]
    public async Task TagRepository_SaveChanges_DeletesAndUpserts()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new TagRepository(dbContext);
        var ownerId = Guid.NewGuid();
        var tagId = Guid.NewGuid();

        await repository.UpsertAsync(new Tag
        {
            Id = tagId,
            OwnerId = ownerId,
            Name = "Tag"
        }, CancellationToken.None);

        var changes = new[]
        {
            new SyncChange<Tag>(SyncOperation.Delete, new Tag
            {
                Id = tagId,
                OwnerId = ownerId,
                Name = "Tag",
                DeletedAt = DateTimeOffset.UtcNow,
                RowVersion = Guid.NewGuid().ToByteArray()
            }, null),
            new SyncChange<Tag>(SyncOperation.Upsert, new Tag
            {
                Id = Guid.NewGuid(),
                OwnerId = ownerId,
                Name = "New Tag",
                RowVersion = Guid.NewGuid().ToByteArray()
            }, null)
        };

        await repository.SaveChangesAsync(changes, ownerId, CancellationToken.None);

        var allTags = await repository.GetByOwnerAsync(ownerId, CancellationToken.None);
        Assert.Single(allTags);
        Assert.Equal("New Tag", allTags.First().Name);
    }

    [Fact]
    public async Task CardRepository_SaveChanges_SkipsForeignDecksAndDeletes()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var deckRepository = new DeckRepository(dbContext);
        var cardRepository = new CardRepository(dbContext);
        var ownerId = Guid.NewGuid();
        var foreignOwnerId = Guid.NewGuid();
        var deckId = Guid.NewGuid();
        var foreignDeckId = Guid.NewGuid();
        var cardId = Guid.NewGuid();

        await deckRepository.UpsertAsync(new Deck
        {
            Id = deckId,
            OwnerId = ownerId,
            Name = "Mine"
        }, CancellationToken.None);
        await deckRepository.UpsertAsync(new Deck
        {
            Id = foreignDeckId,
            OwnerId = foreignOwnerId,
            Name = "Foreign"
        }, CancellationToken.None);
        await cardRepository.UpsertAsync(new Card
        {
            Id = cardId,
            DeckId = deckId,
            Front = "Front",
            Back = "Back",
            State = new CardState
            {
                DueAt = DateTimeOffset.UtcNow,
                IntervalDays = 1,
                EaseFactor = 2.5,
                Streak = 0,
                Lapses = 0
            }
        }, CancellationToken.None);

        var changes = new[]
        {
            new SyncChange<Card>(SyncOperation.Upsert, new Card
            {
                Id = Guid.NewGuid(),
                DeckId = foreignDeckId,
                Front = "Should Skip",
                Back = "Skip",
                State = new CardState
                {
                    DueAt = DateTimeOffset.UtcNow,
                    IntervalDays = 1,
                    EaseFactor = 2.5,
                    Streak = 0,
                    Lapses = 0
                },
                RowVersion = Guid.NewGuid().ToByteArray()
            }, null),
            new SyncChange<Card>(SyncOperation.Delete, new Card
            {
                Id = cardId,
                DeckId = deckId,
                DeletedAt = DateTimeOffset.UtcNow,
                State = new CardState
                {
                    DueAt = DateTimeOffset.UtcNow,
                    IntervalDays = 1,
                    EaseFactor = 2.5,
                    Streak = 0,
                    Lapses = 0
                },
                RowVersion = Guid.NewGuid().ToByteArray()
            }, null)
        };

        await cardRepository.SaveChangesAsync(changes, ownerId, CancellationToken.None);

        var ownedCards = await cardRepository.GetByDeckAsync(deckId, CancellationToken.None);
        Assert.Empty(ownedCards);

        var foreignCards = await cardRepository.GetByDeckAsync(foreignDeckId, CancellationToken.None);
        Assert.Empty(foreignCards);
    }
}
