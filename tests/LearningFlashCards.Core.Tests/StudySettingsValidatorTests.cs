using LearningFlashCards.Core.Domain.Entities;
using LearningFlashCards.Core.Domain.Study;

namespace LearningFlashCards.Core.Tests;

public class StudySettingsValidatorTests
{
    [Fact]
    public void TryValidate_ReturnsFalse_WhenDailyLimitInvalid()
    {
        var settings = new DeckStudySettings { DailyReviewLimit = 0 };

        var isValid = StudySettingsValidator.TryValidate(settings, out var error);

        Assert.False(isValid);
        Assert.Contains("Daily review limit", error);
    }

    [Fact]
    public void TryValidate_ReturnsFalse_WhenMaxLowerThanEasyMin()
    {
        var settings = new DeckStudySettings
        {
            DailyReviewLimit = 10,
            EasyMinIntervalDays = 5,
            MaxIntervalDays = 3
        };

        var isValid = StudySettingsValidator.TryValidate(settings, out var error);

        Assert.False(isValid);
        Assert.Contains("Max interval", error);
    }

    [Fact]
    public void TryValidate_ReturnsTrue_WhenValid()
    {
        var settings = new DeckStudySettings
        {
            DailyReviewLimit = 10,
            EasyMinIntervalDays = 3,
            MaxIntervalDays = 30
        };

        var isValid = StudySettingsValidator.TryValidate(settings, out var error);

        Assert.True(isValid);
        Assert.Null(error);
    }
}
