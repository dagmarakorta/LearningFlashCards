using LearningFlashCards.Core.Domain.Entities;

namespace LearningFlashCards.Core.Domain.Study;

public static class StudySessionRules
{
    public static bool ShouldRepeatInSession(CardReviewRating rating, DeckStudySettings settings)
    {
        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        if (!settings.RepeatInSession)
        {
            return false;
        }

        return rating == CardReviewRating.Again || rating == CardReviewRating.Hard;
    }
}
