using LearningFlashCards.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearningFlashCards.Infrastructure.Persistence.Configurations;

public class DeckConfiguration : IEntityTypeConfiguration<Deck>
{
    public void Configure(EntityTypeBuilder<Deck> builder)
    {
        builder.ToTable("Decks");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(d => d.Description)
            .HasMaxLength(1024);

        builder.Property(d => d.OwnerId)
            .IsRequired();

        builder.Property(d => d.CreatedAt)
            .IsRequired();

        builder.Property(d => d.ModifiedAt)
            .IsRequired();

        builder.HasMany(d => d.Cards)
            .WithOne()
            .HasForeignKey(c => c.DeckId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(d => d.Tags)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "DeckTags",
                l => l.HasOne<Tag>().WithMany().HasForeignKey("TagId").OnDelete(DeleteBehavior.Cascade),
                r => r.HasOne<Deck>().WithMany().HasForeignKey("DeckId").OnDelete(DeleteBehavior.Cascade),
                je =>
                {
                    je.ToTable("DeckTags");
                    je.HasKey("DeckId", "TagId");
                });
    }
}
