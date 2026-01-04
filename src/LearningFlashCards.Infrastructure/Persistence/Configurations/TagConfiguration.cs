using LearningFlashCards.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LearningFlashCards.Infrastructure.Persistence.Configurations;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tags");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(t => t.OwnerId)
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .IsRequired();

        builder.Property(t => t.ModifiedAt)
            .IsRequired();

        builder.Property(t => t.RowVersion)
            .IsRowVersion();

        builder.HasIndex(t => new { t.OwnerId, t.ModifiedAt });
        builder.HasIndex(t => new { t.OwnerId, t.DeletedAt });
        builder.HasIndex(t => new { t.OwnerId, t.Name }).IsUnique();
    }
}
