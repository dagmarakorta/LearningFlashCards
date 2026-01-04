using LearningFlashCards.Core.Domain.Entities;
using LearningFlashCards.Infrastructure.Tests.TestUtilities;

namespace LearningFlashCards.Infrastructure.Tests.Persistence;

public class AppDbContextTests
{
    [Fact]
    public async Task SaveChanges_SetsRowVersion_OnInsertAndUpdate()
    {
        using var dbContext = TestDbContextFactory.CreateContext();

        var profile = new UserProfile
        {
            Email = "rowversion@example.com",
            DisplayName = "Row Version User"
        };

        dbContext.Users.Add(profile);
        await dbContext.SaveChangesAsync();

        var initialRowVersion = profile.RowVersion;

        profile.DisplayName = "Updated Name";
        await dbContext.SaveChangesAsync();

        var updatedRowVersion = profile.RowVersion;

        Assert.NotNull(initialRowVersion);
        Assert.NotNull(updatedRowVersion);
        Assert.NotEmpty(initialRowVersion);
        Assert.NotEmpty(updatedRowVersion);
        Assert.NotEqual(initialRowVersion, updatedRowVersion);
    }
}
