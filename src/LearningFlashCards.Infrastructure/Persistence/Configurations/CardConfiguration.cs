using LearningFlashCards.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearningFlashCards.Infrastructure.Persistence.Configurations;

public class CardConfiguration : IEntityTypeConfiguration<Card>
{
    public void Configure(EntityTypeBuilder<Card> builder)
    {
        builder.ToTable("Cards");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Front)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(c => c.Back)
            .IsRequired()
            .HasMaxLength(2048);

        builder.Property(c => c.Notes)
            .HasMaxLength(4096);

        builder.Property(c => c.DeckId)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.ModifiedAt)
            .IsRequired();

        builder.Property(c => c.RowVersion)
            .IsRowVersion();

        builder.HasIndex(c => new { c.DeckId, c.ModifiedAt });
        builder.HasIndex(c => new { c.DeckId, c.DeletedAt });

        builder.OwnsOne(c => c.State, owned =>
        {
            owned.Property(s => s.DueAt).IsRequired();
            owned.Property(s => s.IntervalDays).IsRequired();
            owned.Property(s => s.EaseFactor).IsRequired().HasDefaultValue(2.5);
            owned.Property(s => s.Streak).IsRequired();
            owned.Property(s => s.Lapses).IsRequired();
        });

        builder.HasMany(c => c.Tags)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "CardTags",
                l => l.HasOne<Tag>().WithMany().HasForeignKey("TagId").OnDelete(DeleteBehavior.Cascade),
                r => r.HasOne<Card>().WithMany().HasForeignKey("CardId").OnDelete(DeleteBehavior.Cascade),
                je =>
                {
                    je.ToTable("CardTags");
                    je.HasKey("CardId", "TagId");
                });
    }
}
