using LearningFlashCards.Core.Domain.Entities;

namespace LearningFlashCards.Core.Domain.Study;

public static class StudySettingsValidator
{
    public static bool TryValidate(DeckStudySettings settings, out string? error)
    {
        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        if (settings.DailyReviewLimit <= 0)
        {
            error = "Daily review limit must be a positive number.";
            return false;
        }

        if (settings.EasyMinIntervalDays <= 0)
        {
            error = "Easy minimum interval must be a positive number.";
            return false;
        }

        if (settings.MaxIntervalDays <= 0)
        {
            error = "Max interval must be a positive number.";
            return false;
        }

        if (settings.MaxIntervalDays < settings.EasyMinIntervalDays)
        {
            error = "Max interval must be greater than or equal to the easy minimum interval.";
            return false;
        }

        error = null;
        return true;
    }
}
