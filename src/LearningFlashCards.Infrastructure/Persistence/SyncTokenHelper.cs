namespace LearningFlashCards.Infrastructure.Persistence;

internal static class SyncTokenHelper
{
    public static DateTimeOffset? Parse(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        return DateTimeOffset.TryParse(token, out var value) ? value : null;
    }

    public static string NewToken() => DateTimeOffset.UtcNow.ToString("O");
}
