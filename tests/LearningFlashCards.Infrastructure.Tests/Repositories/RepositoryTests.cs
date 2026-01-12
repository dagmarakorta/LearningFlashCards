using LearningFlashCards.Core.Domain.Entities;
using LearningFlashCards.Infrastructure.Persistence.Repositories;
using LearningFlashCards.Infrastructure.Tests.TestUtilities;
using LearningFlashCards.Core.Domain.Sync;
using Microsoft.EntityFrameworkCore;

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
        var firstRowVersion = inserted.RowVersion.ToArray();

        inserted.Name = "Updated";
        await repository.UpsertAsync(inserted, CancellationToken.None);

        var updated = await repository.GetAsync(deckId, CancellationToken.None);
        Assert.Equal("Updated", updated!.Name);
        Assert.NotEqual(firstRowVersion, updated.RowVersion);
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
        var firstRowVersion = inserted.RowVersion.ToArray();

        inserted.Name = "Updated Tag";
        await repository.UpsertAsync(inserted, CancellationToken.None);

        var updated = await repository.GetAsync(tagId, CancellationToken.None);
        Assert.Equal("Updated Tag", updated!.Name);
        Assert.NotEqual(firstRowVersion, updated.RowVersion);
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

        var stored = await dbContext.Cards.AsNoTracking().FirstOrDefaultAsync(c => c.Id == cardId, CancellationToken.None);
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

        var stored = await dbContext.Tags.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tagId, CancellationToken.None);
        Assert.NotNull(stored);
        Assert.NotNull(stored!.DeletedAt);
        Assert.True(stored.DeletedAt >= deletedAt.AddSeconds(-1));
    }

    [Fact]
    public async Task DeckRepository_SoftDelete_SetsDeletedAt()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new DeckRepository(dbContext);
        var deckId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();

        await repository.UpsertAsync(new Deck
        {
            Id = deckId,
            OwnerId = ownerId,
            Name = "Deck"
        }, CancellationToken.None);

        var deletedAt = DateTimeOffset.UtcNow;
        await repository.SoftDeleteAsync(deckId, deletedAt, CancellationToken.None);

        var stored = await dbContext.Decks.AsNoTracking().FirstOrDefaultAsync(d => d.Id == deckId, CancellationToken.None);
        Assert.NotNull(stored);
        Assert.NotNull(stored!.DeletedAt);
        Assert.True(stored.DeletedAt >= deletedAt.AddSeconds(-1));
    }

    [Fact]
    public async Task DeckRepository_GetAsync_ReturnsNull_WhenDeleted()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new DeckRepository(dbContext);
        var deckId = Guid.NewGuid();

        dbContext.Decks.Add(new Deck
        {
            Id = deckId,
            OwnerId = Guid.NewGuid(),
            Name = "Deleted Deck",
            DeletedAt = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var deck = await repository.GetAsync(deckId, CancellationToken.None);

        Assert.Null(deck);
    }

    [Fact]
    public async Task DeckRepository_GetByOwner_FiltersDeleted()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new DeckRepository(dbContext);
        var ownerId = Guid.NewGuid();

        dbContext.Decks.AddRange(
            new Deck
            {
                Id = Guid.NewGuid(),
                OwnerId = ownerId,
                Name = "Active"
            },
            new Deck
            {
                Id = Guid.NewGuid(),
                OwnerId = ownerId,
                Name = "Deleted",
                DeletedAt = DateTimeOffset.UtcNow
            });
        await dbContext.SaveChangesAsync();

        var decks = await repository.GetByOwnerAsync(ownerId, CancellationToken.None);

        Assert.Single(decks);
        Assert.Equal("Active", decks.First().Name);
    }

    [Fact]
    public async Task TagRepository_GetByOwner_FiltersDeleted()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new TagRepository(dbContext);
        var ownerId = Guid.NewGuid();

        dbContext.Tags.AddRange(
            new Tag
            {
                Id = Guid.NewGuid(),
                OwnerId = ownerId,
                Name = "Active"
            },
            new Tag
            {
                Id = Guid.NewGuid(),
                OwnerId = ownerId,
                Name = "Deleted",
                DeletedAt = DateTimeOffset.UtcNow
            });
        await dbContext.SaveChangesAsync();

        var tags = await repository.GetByOwnerAsync(ownerId, CancellationToken.None);

        Assert.Single(tags);
        Assert.Equal("Active", tags.First().Name);
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
    public async Task CardRepository_GetAsync_ReturnsNull_WhenDeleted()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new CardRepository(dbContext);
        var cardId = Guid.NewGuid();

        dbContext.Cards.Add(new Card
        {
            Id = cardId,
            DeckId = Guid.NewGuid(),
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
            DeletedAt = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var card = await repository.GetAsync(cardId, CancellationToken.None);

        Assert.Null(card);
    }

    [Fact]
    public async Task TagRepository_GetAsync_ReturnsNull_WhenDeleted()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new TagRepository(dbContext);
        var tagId = Guid.NewGuid();

        dbContext.Tags.Add(new Tag
        {
            Id = tagId,
            OwnerId = Guid.NewGuid(),
            Name = "Deleted Tag",
            DeletedAt = DateTimeOffset.UtcNow
        });
        await dbContext.SaveChangesAsync();

        var tag = await repository.GetAsync(tagId, CancellationToken.None);

        Assert.Null(tag);
    }

    [Fact]
    public async Task CardRepository_SoftDeleteByDeck_DeletesOnlyMatchingDeck()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new CardRepository(dbContext);
        var deckId = Guid.NewGuid();
        var otherDeckId = Guid.NewGuid();

        dbContext.Cards.AddRange(
            new Card
            {
                Id = Guid.NewGuid(),
                DeckId = deckId,
                Front = "Deck Front",
                Back = "Deck Back",
                State = new CardState
                {
                    DueAt = DateTimeOffset.UtcNow,
                    IntervalDays = 1,
                    EaseFactor = 2.5,
                    Streak = 0,
                    Lapses = 0
                }
            },
            new Card
            {
                Id = Guid.NewGuid(),
                DeckId = otherDeckId,
                Front = "Other Front",
                Back = "Other Back",
                State = new CardState
                {
                    DueAt = DateTimeOffset.UtcNow,
                    IntervalDays = 1,
                    EaseFactor = 2.5,
                    Streak = 0,
                    Lapses = 0
                }
            });
        await dbContext.SaveChangesAsync();

        var deletedAt = DateTimeOffset.UtcNow;
        await repository.SoftDeleteByDeckAsync(deckId, deletedAt, CancellationToken.None);

        var deckCards = await dbContext.Cards.AsNoTracking()
            .Where(c => c.DeckId == deckId)
            .ToListAsync(CancellationToken.None);
        Assert.All(deckCards, card => Assert.NotNull(card.DeletedAt));

        var otherDeckCards = await dbContext.Cards.AsNoTracking()
            .Where(c => c.DeckId == otherDeckId)
            .ToListAsync(CancellationToken.None);
        Assert.All(otherDeckCards, card => Assert.Null(card.DeletedAt));
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
    public async Task DeckRepository_SaveChanges_DeletesExistingDeck()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new DeckRepository(dbContext);
        var ownerId = Guid.NewGuid();
        var deckId = Guid.NewGuid();

        await repository.UpsertAsync(new Deck
        {
            Id = deckId,
            OwnerId = ownerId,
            Name = "To Delete"
        }, CancellationToken.None);

        var deletedAt = DateTimeOffset.UtcNow;
        var changes = new[]
        {
            new SyncChange<Deck>(SyncOperation.Delete, new Deck
            {
                Id = deckId,
                OwnerId = ownerId,
                DeletedAt = deletedAt,
                RowVersion = Guid.NewGuid().ToByteArray()
            }, null)
        };

        await repository.SaveChangesAsync(changes, ownerId, CancellationToken.None);

        var stored = await dbContext.Decks.AsNoTracking().FirstOrDefaultAsync(d => d.Id == deckId, CancellationToken.None);
        Assert.NotNull(stored);
        Assert.NotNull(stored!.DeletedAt);
        Assert.True(stored.DeletedAt >= deletedAt.AddSeconds(-1));
    }

    [Fact]
    public async Task DeckRepository_SaveChanges_InsertsDeletedWhenMissing()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new DeckRepository(dbContext);
        var ownerId = Guid.NewGuid();
        var deckId = Guid.NewGuid();

        var deletedAt = DateTimeOffset.UtcNow;
        var changes = new[]
        {
            new SyncChange<Deck>(SyncOperation.Delete, new Deck
            {
                Id = deckId,
                OwnerId = ownerId,
                Name = "Missing",
                DeletedAt = deletedAt,
                RowVersion = Guid.NewGuid().ToByteArray()
            }, null)
        };

        await repository.SaveChangesAsync(changes, ownerId, CancellationToken.None);

        var stored = await dbContext.Decks.AsNoTracking().FirstOrDefaultAsync(d => d.Id == deckId, CancellationToken.None);
        Assert.NotNull(stored);
        Assert.NotNull(stored!.DeletedAt);
    }

    [Fact]
    public async Task DeckRepository_SaveChanges_UpsertsExistingDeck()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new DeckRepository(dbContext);
        var ownerId = Guid.NewGuid();
        var deckId = Guid.NewGuid();

        await repository.UpsertAsync(new Deck
        {
            Id = deckId,
            OwnerId = ownerId,
            Name = "Before"
        }, CancellationToken.None);
        dbContext.ChangeTracker.Clear();
        var existing = await dbContext.Decks.AsNoTracking()
            .FirstAsync(d => d.Id == deckId, CancellationToken.None);

        var changes = new[]
        {
            new SyncChange<Deck>(SyncOperation.Upsert, new Deck
            {
                Id = deckId,
                OwnerId = ownerId,
                Name = "After",
                RowVersion = existing.RowVersion
            }, null)
        };

        await repository.SaveChangesAsync(changes, ownerId, CancellationToken.None);

        var stored = await repository.GetAsync(deckId, CancellationToken.None);
        Assert.NotNull(stored);
        Assert.Equal("After", stored!.Name);
    }

    [Fact]
    public async Task DeckRepository_SaveChanges_InsertsUpsertWhenMissing()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new DeckRepository(dbContext);
        var ownerId = Guid.NewGuid();
        var deckId = Guid.NewGuid();

        var changes = new[]
        {
            new SyncChange<Deck>(SyncOperation.Upsert, new Deck
            {
                Id = deckId,
                OwnerId = ownerId,
                Name = "New",
                RowVersion = Guid.NewGuid().ToByteArray()
            }, null)
        };

        await repository.SaveChangesAsync(changes, ownerId, CancellationToken.None);

        var stored = await repository.GetAsync(deckId, CancellationToken.None);
        Assert.NotNull(stored);
        Assert.Equal("New", stored!.Name);
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
    public async Task TagRepository_SaveChanges_DeletesExistingTag()
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

        var deletedAt = DateTimeOffset.UtcNow;
        var changes = new[]
        {
            new SyncChange<Tag>(SyncOperation.Delete, new Tag
            {
                Id = tagId,
                OwnerId = ownerId,
                Name = "Tag",
                DeletedAt = deletedAt,
                RowVersion = Guid.NewGuid().ToByteArray()
            }, null)
        };

        await repository.SaveChangesAsync(changes, ownerId, CancellationToken.None);

        var stored = await dbContext.Tags.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tagId, CancellationToken.None);
        Assert.NotNull(stored);
        Assert.NotNull(stored!.DeletedAt);
        Assert.True(stored.DeletedAt >= deletedAt.AddSeconds(-1));
    }

    [Fact]
    public async Task TagRepository_SaveChanges_InsertsDeletedWhenMissing()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new TagRepository(dbContext);
        var ownerId = Guid.NewGuid();
        var tagId = Guid.NewGuid();

        var deletedAt = DateTimeOffset.UtcNow;
        var changes = new[]
        {
            new SyncChange<Tag>(SyncOperation.Delete, new Tag
            {
                Id = tagId,
                OwnerId = ownerId,
                Name = "Missing",
                DeletedAt = deletedAt,
                RowVersion = Guid.NewGuid().ToByteArray()
            }, null)
        };

        await repository.SaveChangesAsync(changes, ownerId, CancellationToken.None);

        var stored = await dbContext.Tags.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tagId, CancellationToken.None);
        Assert.NotNull(stored);
        Assert.NotNull(stored!.DeletedAt);
    }

    [Fact]
    public async Task TagRepository_SaveChanges_UpsertsExistingTag()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new TagRepository(dbContext);
        var ownerId = Guid.NewGuid();
        var tagId = Guid.NewGuid();

        await repository.UpsertAsync(new Tag
        {
            Id = tagId,
            OwnerId = ownerId,
            Name = "Before"
        }, CancellationToken.None);
        dbContext.ChangeTracker.Clear();
        var existing = await dbContext.Tags.AsNoTracking()
            .FirstAsync(t => t.Id == tagId, CancellationToken.None);

        var changes = new[]
        {
            new SyncChange<Tag>(SyncOperation.Upsert, new Tag
            {
                Id = tagId,
                OwnerId = ownerId,
                Name = "After",
                RowVersion = existing.RowVersion
            }, null)
        };

        await repository.SaveChangesAsync(changes, ownerId, CancellationToken.None);

        var stored = await repository.GetAsync(tagId, CancellationToken.None);
        Assert.NotNull(stored);
        Assert.Equal("After", stored!.Name);
    }

    [Fact]
    public async Task TagRepository_SaveChanges_InsertsUpsertWhenMissing()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new TagRepository(dbContext);
        var ownerId = Guid.NewGuid();
        var tagId = Guid.NewGuid();

        var changes = new[]
        {
            new SyncChange<Tag>(SyncOperation.Upsert, new Tag
            {
                Id = tagId,
                OwnerId = ownerId,
                Name = "New",
                RowVersion = Guid.NewGuid().ToByteArray()
            }, null)
        };

        await repository.SaveChangesAsync(changes, ownerId, CancellationToken.None);

        var stored = await repository.GetAsync(tagId, CancellationToken.None);
        Assert.NotNull(stored);
        Assert.Equal("New", stored!.Name);
    }

    [Fact]
    public async Task CardRepository_SaveChanges_UpsertsExistingCard()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var deckRepository = new DeckRepository(dbContext);
        var repository = new CardRepository(dbContext);
        var ownerId = Guid.NewGuid();
        var deckId = Guid.NewGuid();
        var cardId = Guid.NewGuid();

        await deckRepository.UpsertAsync(new Deck
        {
            Id = deckId,
            OwnerId = ownerId,
            Name = "Deck"
        }, CancellationToken.None);

        await repository.UpsertAsync(new Card
        {
            Id = cardId,
            DeckId = deckId,
            Front = "Before",
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
        dbContext.ChangeTracker.Clear();
        var existing = await dbContext.Cards.AsNoTracking()
            .FirstAsync(c => c.Id == cardId, CancellationToken.None);

        var changes = new[]
        {
            new SyncChange<Card>(SyncOperation.Upsert, new Card
            {
                Id = cardId,
                DeckId = deckId,
                Front = "After",
                Back = "Back",
                State = existing.State,
                RowVersion = existing.RowVersion
            }, null)
        };

        await repository.SaveChangesAsync(changes, ownerId, CancellationToken.None);

        var stored = await repository.GetAsync(cardId, CancellationToken.None);
        Assert.NotNull(stored);
        Assert.Equal("After", stored!.Front);
    }

    [Fact]
    public async Task CardRepository_SaveChanges_InsertsUpsertWhenMissing()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var deckRepository = new DeckRepository(dbContext);
        var repository = new CardRepository(dbContext);
        var ownerId = Guid.NewGuid();
        var deckId = Guid.NewGuid();
        var cardId = Guid.NewGuid();

        await deckRepository.UpsertAsync(new Deck
        {
            Id = deckId,
            OwnerId = ownerId,
            Name = "Deck"
        }, CancellationToken.None);

        var changes = new[]
        {
            new SyncChange<Card>(SyncOperation.Upsert, new Card
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
                },
                RowVersion = Guid.NewGuid().ToByteArray()
            }, null)
        };

        await repository.SaveChangesAsync(changes, ownerId, CancellationToken.None);

        var stored = await repository.GetAsync(cardId, CancellationToken.None);
        Assert.NotNull(stored);
        Assert.Equal("Front", stored!.Front);
    }

    [Fact]
    public async Task UserProfileRepository_ExistsAndGets_ByEmailAndDisplayName()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new UserProfileRepository(dbContext);
        var profileId = Guid.NewGuid();

        await repository.CreateAsync(new UserProfile
        {
            Id = profileId,
            Email = "Test@Example.com",
            DisplayName = "TestUser"
        }, CancellationToken.None);

        var existsByEmail = await repository.ExistsByEmailAsync("test@example.com", CancellationToken.None);
        var existsByDisplayName = await repository.ExistsByDisplayNameAsync("testuser", CancellationToken.None);
        var byEmail = await repository.GetByEmailAsync("TEST@example.com", CancellationToken.None);
        var byDisplayName = await repository.GetByDisplayNameAsync("TESTUSER", CancellationToken.None);

        Assert.True(existsByEmail);
        Assert.True(existsByDisplayName);
        Assert.NotNull(byEmail);
        Assert.NotNull(byDisplayName);
    }

    [Fact]
    public async Task UserProfileRepository_SaveChanges_DeletesExistingProfile()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new UserProfileRepository(dbContext);
        var ownerId = Guid.NewGuid();

        await repository.CreateAsync(new UserProfile
        {
            Id = ownerId,
            Email = "owner@example.com",
            DisplayName = "Owner"
        }, CancellationToken.None);
        dbContext.ChangeTracker.Clear();
        var existing = await dbContext.Users.AsNoTracking()
            .FirstAsync(u => u.Id == ownerId, CancellationToken.None);

        var deletedAt = DateTimeOffset.UtcNow;
        var changes = new[]
        {
            new SyncChange<UserProfile>(SyncOperation.Delete, new UserProfile
            {
                Id = ownerId,
                DeletedAt = deletedAt,
                RowVersion = existing.RowVersion
            }, null),
        };

        await repository.SaveChangesAsync(changes, ownerId, CancellationToken.None);

        var stored = await repository.GetAsync(ownerId, CancellationToken.None);
        Assert.Null(stored);
    }

    [Fact]
    public async Task UserProfileRepository_SaveChanges_UpsertsExistingProfile()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new UserProfileRepository(dbContext);
        var ownerId = Guid.NewGuid();

        await repository.CreateAsync(new UserProfile
        {
            Id = ownerId,
            Email = "owner@example.com",
            DisplayName = "Owner"
        }, CancellationToken.None);
        dbContext.ChangeTracker.Clear();
        var existing = await dbContext.Users.AsNoTracking()
            .FirstAsync(u => u.Id == ownerId, CancellationToken.None);

        var changes = new[]
        {
            new SyncChange<UserProfile>(SyncOperation.Upsert, new UserProfile
            {
                Id = ownerId,
                Email = "updated@example.com",
                DisplayName = "Updated",
                RowVersion = existing.RowVersion
            }, null)
        };

        await repository.SaveChangesAsync(changes, ownerId, CancellationToken.None);

        var stored = await repository.GetAsync(ownerId, CancellationToken.None);
        Assert.NotNull(stored);
        Assert.Equal("updated@example.com", stored!.Email);
    }

    [Fact]
    public async Task UserProfileRepository_Upsert_UpdatesTrackedEntity()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new UserProfileRepository(dbContext);
        var profileId = Guid.NewGuid();

        await repository.CreateAsync(new UserProfile
        {
            Id = profileId,
            Email = "owner@example.com",
            DisplayName = "Owner"
        }, CancellationToken.None);

        var tracked = await dbContext.Users.FirstAsync(u => u.Id == profileId, CancellationToken.None);
        var updatedProfile = new UserProfile
        {
            Id = profileId,
            Email = "updated@example.com",
            DisplayName = "Updated"
        };

        await repository.UpsertAsync(updatedProfile, CancellationToken.None);

        Assert.Equal("updated@example.com", tracked.Email);
        Assert.Equal("Updated", tracked.DisplayName);
    }

    [Fact]
    public async Task UserProfileRepository_GetByEmail_ReturnsNull_WhenDeleted()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new UserProfileRepository(dbContext);
        var profileId = Guid.NewGuid();

        await repository.CreateAsync(new UserProfile
        {
            Id = profileId,
            Email = "deleted@example.com",
            DisplayName = "Deleted"
        }, CancellationToken.None);

        var stored = await dbContext.Users.FirstAsync(u => u.Id == profileId, CancellationToken.None);
        stored.DeletedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync();

        var profile = await repository.GetByEmailAsync("deleted@example.com", CancellationToken.None);

        Assert.Null(profile);
    }

    [Fact]
    public async Task UserProfileRepository_GetByDisplayName_ReturnsNull_WhenDeleted()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new UserProfileRepository(dbContext);
        var profileId = Guid.NewGuid();

        await repository.CreateAsync(new UserProfile
        {
            Id = profileId,
            Email = "deleted2@example.com",
            DisplayName = "DeletedTwo"
        }, CancellationToken.None);

        var stored = await dbContext.Users.FirstAsync(u => u.Id == profileId, CancellationToken.None);
        stored.DeletedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync();

        var profile = await repository.GetByDisplayNameAsync("DeletedTwo", CancellationToken.None);

        Assert.Null(profile);
    }

    [Fact]
    public async Task UserProfileRepository_GetAsync_ReturnsNull_WhenDeleted()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new UserProfileRepository(dbContext);
        var profileId = Guid.NewGuid();

        await repository.CreateAsync(new UserProfile
        {
            Id = profileId,
            Email = "gone@example.com",
            DisplayName = "Gone"
        }, CancellationToken.None);

        var stored = await dbContext.Users.FirstAsync(u => u.Id == profileId, CancellationToken.None);
        stored.DeletedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync();

        var profile = await repository.GetAsync(profileId, CancellationToken.None);

        Assert.Null(profile);
    }

    [Fact]
    public async Task UserProfileRepository_Upsert_UpdatesExistingWhenNotTracked()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new UserProfileRepository(dbContext);
        var profileId = Guid.NewGuid();

        await repository.CreateAsync(new UserProfile
        {
            Id = profileId,
            Email = "owner@example.com",
            DisplayName = "Owner"
        }, CancellationToken.None);
        dbContext.ChangeTracker.Clear();

        var existing = await dbContext.Users.AsNoTracking()
            .FirstAsync(u => u.Id == profileId, CancellationToken.None);

        await repository.UpsertAsync(new UserProfile
        {
            Id = profileId,
            Email = "updated@example.com",
            DisplayName = "Updated",
            RowVersion = existing.RowVersion
        }, CancellationToken.None);

        var stored = await repository.GetAsync(profileId, CancellationToken.None);
        Assert.NotNull(stored);
        Assert.Equal("updated@example.com", stored!.Email);
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

    [Fact]
    public async Task DeckRepository_GetChangesSince_FiltersByOwnerAndSince()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new DeckRepository(dbContext);
        var ownerId = Guid.NewGuid();
        var otherOwnerId = Guid.NewGuid();
        var oldDeckId = Guid.NewGuid();
        var deleteDeckId = Guid.NewGuid();

        dbContext.Decks.AddRange(
            new Deck
            {
                Id = oldDeckId,
                OwnerId = ownerId,
                Name = "Old"
            },
            new Deck
            {
                Id = deleteDeckId,
                OwnerId = ownerId,
                Name = "Deleted"
            });
        await dbContext.SaveChangesAsync();

        var since = DateTimeOffset.UtcNow;

        var deleteDeck = await dbContext.Decks.FirstAsync(d => d.Id == deleteDeckId, CancellationToken.None);
        deleteDeck.DeletedAt = DateTimeOffset.UtcNow;

        dbContext.Decks.AddRange(
            new Deck
            {
                Id = Guid.NewGuid(),
                OwnerId = ownerId,
                Name = "Recent"
            },
            new Deck
            {
                Id = Guid.NewGuid(),
                OwnerId = otherOwnerId,
                Name = "Other"
            });
        await dbContext.SaveChangesAsync();

        var changes = await repository.GetChangesSinceAsync(since.ToString("O"), ownerId, CancellationToken.None);

        Assert.Equal(2, changes.Count);
        Assert.Contains(changes, change => change.Operation == SyncOperation.Upsert && change.Entity.Name == "Recent");
        Assert.Contains(changes, change => change.Operation == SyncOperation.Delete && change.Entity.Name == "Deleted");
    }

    [Fact]
    public async Task TagRepository_GetChangesSince_FiltersByOwnerAndSince()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new TagRepository(dbContext);
        var ownerId = Guid.NewGuid();
        var oldTagId = Guid.NewGuid();
        var deleteTagId = Guid.NewGuid();

        dbContext.Tags.AddRange(
            new Tag
            {
                Id = oldTagId,
                OwnerId = ownerId,
                Name = "Old"
            },
            new Tag
            {
                Id = deleteTagId,
                OwnerId = ownerId,
                Name = "Deleted"
            });
        await dbContext.SaveChangesAsync();

        var since = DateTimeOffset.UtcNow;

        var deleteTag = await dbContext.Tags.FirstAsync(t => t.Id == deleteTagId, CancellationToken.None);
        deleteTag.DeletedAt = DateTimeOffset.UtcNow;
        dbContext.Tags.Add(new Tag
        {
            Id = Guid.NewGuid(),
            OwnerId = ownerId,
            Name = "Recent"
        });
        await dbContext.SaveChangesAsync();

        var changes = await repository.GetChangesSinceAsync(since.ToString("O"), ownerId, CancellationToken.None);

        Assert.Equal(2, changes.Count);
        Assert.Contains(changes, change => change.Operation == SyncOperation.Upsert && change.Entity.Name == "Recent");
        Assert.Contains(changes, change => change.Operation == SyncOperation.Delete && change.Entity.Name == "Deleted");
    }

    [Fact]
    public async Task CardRepository_GetChangesSince_FiltersByOwnerAndSince()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var deckRepository = new DeckRepository(dbContext);
        var repository = new CardRepository(dbContext);
        var ownerId = Guid.NewGuid();
        var otherOwnerId = Guid.NewGuid();
        var deckId = Guid.NewGuid();
        var otherDeckId = Guid.NewGuid();

        await deckRepository.UpsertAsync(new Deck
        {
            Id = deckId,
            OwnerId = ownerId,
            Name = "Deck"
        }, CancellationToken.None);
        await deckRepository.UpsertAsync(new Deck
        {
            Id = otherDeckId,
            OwnerId = otherOwnerId,
            Name = "Other"
        }, CancellationToken.None);

        var oldCardId = Guid.NewGuid();
        var deleteCardId = Guid.NewGuid();

        dbContext.Cards.AddRange(
            new Card
            {
                Id = oldCardId,
                DeckId = deckId,
                Front = "Old",
                Back = "Back",
                State = new CardState
                {
                    DueAt = DateTimeOffset.UtcNow,
                    IntervalDays = 1,
                    EaseFactor = 2.5,
                    Streak = 0,
                    Lapses = 0
                }
            },
            new Card
            {
                Id = deleteCardId,
                DeckId = deckId,
                Front = "Deleted",
                Back = "Back",
                State = new CardState
                {
                    DueAt = DateTimeOffset.UtcNow,
                    IntervalDays = 1,
                    EaseFactor = 2.5,
                    Streak = 0,
                    Lapses = 0
                }
            },
            new Card
            {
                Id = Guid.NewGuid(),
                DeckId = otherDeckId,
                Front = "Other",
                Back = "Back",
                State = new CardState
                {
                    DueAt = DateTimeOffset.UtcNow,
                    IntervalDays = 1,
                    EaseFactor = 2.5,
                    Streak = 0,
                    Lapses = 0
                }
            });
        await dbContext.SaveChangesAsync();

        var since = DateTimeOffset.UtcNow;

        var deleteCard = await dbContext.Cards.FirstAsync(c => c.Id == deleteCardId, CancellationToken.None);
        deleteCard.DeletedAt = DateTimeOffset.UtcNow;
        dbContext.Cards.Add(new Card
        {
            Id = Guid.NewGuid(),
            DeckId = deckId,
            Front = "Recent",
            Back = "Back",
            State = new CardState
            {
                DueAt = DateTimeOffset.UtcNow,
                IntervalDays = 1,
                EaseFactor = 2.5,
                Streak = 0,
                Lapses = 0
            }
        });
        await dbContext.SaveChangesAsync();

        var changes = await repository.GetChangesSinceAsync(since.ToString("O"), ownerId, CancellationToken.None);

        Assert.Equal(2, changes.Count);
        Assert.Contains(changes, change => change.Operation == SyncOperation.Upsert && change.Entity.Front == "Recent");
        Assert.Contains(changes, change => change.Operation == SyncOperation.Delete && change.Entity.Front == "Deleted");
    }

    [Fact]
    public async Task UserProfileRepository_GetChangesSince_ReturnsOwnerProfile()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new UserProfileRepository(dbContext);
        var ownerId = Guid.NewGuid();
        dbContext.Users.AddRange(
            new UserProfile
            {
                Id = ownerId,
                Email = "owner@example.com",
                DisplayName = "Owner"
            },
            new UserProfile
            {
                Id = Guid.NewGuid(),
                Email = "other@example.com",
                DisplayName = "Other"
            });
        await dbContext.SaveChangesAsync();

        var since = DateTimeOffset.UtcNow;

        var owner = await dbContext.Users.FirstAsync(u => u.Id == ownerId, CancellationToken.None);
        owner.DisplayName = "Owner Updated";
        await dbContext.SaveChangesAsync();

        var changes = await repository.GetChangesSinceAsync(since.ToString("O"), ownerId, CancellationToken.None);

        Assert.Single(changes);
        Assert.Equal(ownerId, changes[0].Entity.Id);
        Assert.Equal(SyncOperation.Upsert, changes[0].Operation);
    }

    [Fact]
    public async Task CardRepository_GetAsync_IncludesTags()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var deckRepository = new DeckRepository(dbContext);
        var tagRepository = new TagRepository(dbContext);
        var cardRepository = new CardRepository(dbContext);
        var ownerId = Guid.NewGuid();
        var deckId = Guid.NewGuid();
        var cardId = Guid.NewGuid();
        var tagId1 = Guid.NewGuid();
        var tagId2 = Guid.NewGuid();

        await deckRepository.UpsertAsync(new Deck
        {
            Id = deckId,
            OwnerId = ownerId,
            Name = "Deck"
        }, CancellationToken.None);

        await tagRepository.UpsertAsync(new Tag
        {
            Id = tagId1,
            OwnerId = ownerId,
            Name = "Tag1"
        }, CancellationToken.None);

        await tagRepository.UpsertAsync(new Tag
        {
            Id = tagId2,
            OwnerId = ownerId,
            Name = "Tag2"
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

        dbContext.Cards.Add(card);
        await dbContext.SaveChangesAsync();

        var tag1 = await dbContext.Tags.FindAsync(tagId1);
        var tag2 = await dbContext.Tags.FindAsync(tagId2);
        card.Tags.Add(tag1!);
        card.Tags.Add(tag2!);
        await dbContext.SaveChangesAsync();

        dbContext.ChangeTracker.Clear();

        var retrieved = await cardRepository.GetAsync(cardId, CancellationToken.None);

        Assert.NotNull(retrieved);
        Assert.Equal(2, retrieved!.Tags.Count);
        Assert.Contains(retrieved.Tags, t => t.Name == "Tag1");
        Assert.Contains(retrieved.Tags, t => t.Name == "Tag2");
    }

    [Fact]
    public async Task CardRepository_SoftDeleteAsync_ReturnsEarly_WhenCardNotFound()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new CardRepository(dbContext);
        var nonExistentCardId = Guid.NewGuid();

        await repository.SoftDeleteAsync(nonExistentCardId, DateTimeOffset.UtcNow, CancellationToken.None);

        var card = await dbContext.Cards.FirstOrDefaultAsync(c => c.Id == nonExistentCardId, CancellationToken.None);
        Assert.Null(card);
    }

    [Fact]
    public async Task CardRepository_SoftDeleteByDeckAsync_ReturnsEarly_WhenNoCards()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new CardRepository(dbContext);
        var emptyDeckId = Guid.NewGuid();

        await repository.SoftDeleteByDeckAsync(emptyDeckId, DateTimeOffset.UtcNow, CancellationToken.None);

        var cards = await dbContext.Cards.Where(c => c.DeckId == emptyDeckId).ToListAsync(CancellationToken.None);
        Assert.Empty(cards);
    }

    [Fact]
    public async Task CardRepository_SaveChanges_InsertsDeletedWhenMissing()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var deckRepository = new DeckRepository(dbContext);
        var cardRepository = new CardRepository(dbContext);
        var ownerId = Guid.NewGuid();
        var deckId = Guid.NewGuid();
        var cardId = Guid.NewGuid();

        await deckRepository.UpsertAsync(new Deck
        {
            Id = deckId,
            OwnerId = ownerId,
            Name = "Deck"
        }, CancellationToken.None);

        var deletedAt = DateTimeOffset.UtcNow;
        var changes = new[]
        {
            new SyncChange<Card>(SyncOperation.Delete, new Card
            {
                Id = cardId,
                DeckId = deckId,
                Front = "Deleted",
                Back = "Back",
                State = new CardState
                {
                    DueAt = DateTimeOffset.UtcNow,
                    IntervalDays = 1,
                    EaseFactor = 2.5,
                    Streak = 0,
                    Lapses = 0
                },
                DeletedAt = deletedAt,
                RowVersion = Guid.NewGuid().ToByteArray()
            }, null)
        };

        await cardRepository.SaveChangesAsync(changes, ownerId, CancellationToken.None);

        var stored = await dbContext.Cards.AsNoTracking().FirstOrDefaultAsync(c => c.Id == cardId, CancellationToken.None);
        Assert.NotNull(stored);
        Assert.NotNull(stored!.DeletedAt);
    }

    [Fact]
    public async Task UserProfileRepository_ExistsByEmailAsync_ReturnsFalse_WhenNotFound()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new UserProfileRepository(dbContext);

        var exists = await repository.ExistsByEmailAsync("nonexistent@example.com", CancellationToken.None);

        Assert.False(exists);
    }

    [Fact]
    public async Task UserProfileRepository_ExistsByDisplayNameAsync_ReturnsFalse_WhenNotFound()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new UserProfileRepository(dbContext);

        var exists = await repository.ExistsByDisplayNameAsync("NonExistentUser", CancellationToken.None);

        Assert.False(exists);
    }

    [Fact]
    public async Task UserProfileRepository_CreateAsync_InsertsNewProfile()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new UserProfileRepository(dbContext);
        var profileId = Guid.NewGuid();

        var profile = await repository.CreateAsync(new UserProfile
        {
            Id = profileId,
            Email = "new@example.com",
            DisplayName = "NewUser",
            AvatarUrl = "https://example.com/avatar.png"
        }, CancellationToken.None);

        Assert.NotNull(profile);
        Assert.Equal(profileId, profile.Id);
        Assert.Equal("new@example.com", profile.Email);
        Assert.Equal("NewUser", profile.DisplayName);
        Assert.Equal("https://example.com/avatar.png", profile.AvatarUrl);

        var stored = await dbContext.Users.FindAsync(profileId);
        Assert.NotNull(stored);
        Assert.Equal("new@example.com", stored!.Email);
    }

    [Fact]
    public async Task UserProfileRepository_GetByEmailAsync_ReturnsNull_WhenNotFound()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new UserProfileRepository(dbContext);

        var profile = await repository.GetByEmailAsync("missing@example.com", CancellationToken.None);

        Assert.Null(profile);
    }

    [Fact]
    public async Task UserProfileRepository_GetByDisplayNameAsync_ReturnsNull_WhenNotFound()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new UserProfileRepository(dbContext);

        var profile = await repository.GetByDisplayNameAsync("MissingUser", CancellationToken.None);

        Assert.Null(profile);
    }

    [Fact]
    public async Task UserProfileRepository_GetAsync_ReturnsNull_WhenNotFound()
    {
        using var dbContext = TestDbContextFactory.CreateContext();
        var repository = new UserProfileRepository(dbContext);
        var nonExistentId = Guid.NewGuid();

        var profile = await repository.GetAsync(nonExistentId, CancellationToken.None);

        Assert.Null(profile);
    }
}
