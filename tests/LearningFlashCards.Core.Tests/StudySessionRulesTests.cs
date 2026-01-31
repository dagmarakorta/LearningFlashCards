using LearningFlashCards.Core.Domain.Entities;
using LearningFlashCards.Core.Domain.Study;

namespace LearningFlashCards.Core.Tests;

public class StudySessionRulesTests
{
    [Fact]
    public void ShouldRepeatInSession_ReturnsFalse_WhenDisabled()
    {
        var settings = new DeckStudySettings { RepeatInSession = false };

        var result = StudySessionRules.ShouldRepeatInSession(CardReviewRating.Again, settings);

        Assert.False(result);
    }

    [Theory]
    [InlineData(CardReviewRating.Again, true)]
    [InlineData(CardReviewRating.Hard, true)]
    [InlineData(CardReviewRating.Medium, false)]
    [InlineData(CardReviewRating.Easy, false)]
    public void ShouldRepeatInSession_RespectsRating(CardReviewRating rating, bool expected)
    {
        var settings = new DeckStudySettings { RepeatInSession = true };

        var result = StudySessionRules.ShouldRepeatInSession(rating, settings);

        Assert.Equal(expected, result);
    }
}
