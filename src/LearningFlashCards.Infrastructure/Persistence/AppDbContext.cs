using LearningFlashCards.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LearningFlashCards.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Deck> Decks => Set<Deck>();
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<UserProfile> Users => Set<UserProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.ModifiedAt = utcNow;
                entry.Entity.RowVersion = Guid.NewGuid().ToByteArray();
            }
            else if (entry.State == EntityState.Added)
            {
                entry.Entity.ModifiedAt = utcNow;
                entry.Entity.RowVersion = Guid.NewGuid().ToByteArray();
            }
        }

        // TODO: Consider moving to a database provider with native rowversion support to drop the manual token updates.
        return base.SaveChangesAsync(cancellationToken);
    }
}
