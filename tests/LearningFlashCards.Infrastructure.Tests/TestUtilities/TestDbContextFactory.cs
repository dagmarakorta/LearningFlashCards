using LearningFlashCards.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LearningFlashCards.Infrastructure.Tests.TestUtilities;

internal static class TestDbContextFactory
{
    public static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;

        return new AppDbContext(options);
    }
}
