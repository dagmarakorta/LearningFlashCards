using System.Text;

namespace LearningFlashCards.Api.Services;

public static class TextSanitizer
{
    private static readonly char[] StrictUnsafeChars = { '<', '>', '\\', '/', '`' };

    public static string SanitizePermissive(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            if (IsAllowedCharacter(c))
            {
                sb.Append(c);
            }
        }

        return NormalizeAndTrim(sb.ToString());
    }

    public static string SanitizeStrict(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            if (IsAllowedCharacter(c) && !IsStrictUnsafeCharacter(c))
            {
                sb.Append(c);
            }
        }

        return NormalizeAndTrim(sb.ToString());
    }

    public static bool IsValidStrict(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return true;
        }

        return value.IndexOfAny(StrictUnsafeChars) < 0 && !HasDangerousControlChars(value);
    }

    public static char[] GetInvalidStrictChars(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return [];
        }

        var invalidChars = new HashSet<char>();
        foreach (var c in value)
        {
            if (IsStrictUnsafeCharacter(c) || (char.IsControl(c) && c != '\t' && c != '\n' && c != '\r'))
            {
                invalidChars.Add(c);
            }
        }

        return [.. invalidChars];
    }

    public static bool IsValidPermissive(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return true;
        }

        return !HasDangerousControlChars(value);
    }

    private static bool IsAllowedCharacter(char c)
    {
        if (c == '\t' || c == '\n' || c == '\r')
        {
            return true;
        }

        if (char.IsControl(c))
        {
            return false;
        }

        return true;
    }

    private static bool IsStrictUnsafeCharacter(char c)
    {
        return Array.IndexOf(StrictUnsafeChars, c) >= 0;
    }

    private static bool HasDangerousControlChars(string value)
    {
        foreach (var c in value)
        {
            if (char.IsControl(c) && c != '\t' && c != '\n' && c != '\r')
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizeAndTrim(string value)
    {
        var normalized = value
            .Replace("\r\n", "\n")
            .Replace("\r", "\n");

        return normalized.Trim();
    }
}
