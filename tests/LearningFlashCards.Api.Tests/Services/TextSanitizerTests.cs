using LearningFlashCards.Api.Services;

namespace LearningFlashCards.Api.Tests.Services;

public class TextSanitizerTests
{
    #region SanitizePermissive Tests

    [Fact]
    public void SanitizePermissive_ReturnsEmpty_WhenInputNull()
    {
        var result = TextSanitizer.SanitizePermissive(null!);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void SanitizePermissive_ReturnsEmpty_WhenInputEmpty()
    {
        var result = TextSanitizer.SanitizePermissive(string.Empty);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void SanitizePermissive_TrimsWhitespace()
    {
        var result = TextSanitizer.SanitizePermissive("  hello world  ");
        Assert.Equal("hello world", result);
    }

    [Fact]
    public void SanitizePermissive_PreservesCodeCharacters()
    {
        var input = "<div>Hello</div> & foo > bar";
        var result = TextSanitizer.SanitizePermissive(input);
        Assert.Equal(input, result);
    }

    [Fact]
    public void SanitizePermissive_PreservesNewlinesAndTabs()
    {
        var input = "line1\n\tline2\nline3";
        var result = TextSanitizer.SanitizePermissive(input);
        Assert.Equal(input, result);
    }

    [Fact]
    public void SanitizePermissive_RemovesNullCharacter()
    {
        var input = "hello\0world";
        var result = TextSanitizer.SanitizePermissive(input);
        Assert.Equal("helloworld", result);
    }

    [Fact]
    public void SanitizePermissive_RemovesBellCharacter()
    {
        var input = "hello\aworld";
        var result = TextSanitizer.SanitizePermissive(input);
        Assert.Equal("helloworld", result);
    }

    [Fact]
    public void SanitizePermissive_RemovesBackspaceCharacter()
    {
        var input = "hello\bworld";
        var result = TextSanitizer.SanitizePermissive(input);
        Assert.Equal("helloworld", result);
    }

    [Fact]
    public void SanitizePermissive_RemovesDeleteCharacter()
    {
        var input = "hello\x7Fworld";
        var result = TextSanitizer.SanitizePermissive(input);
        Assert.Equal("helloworld", result);
    }

    [Fact]
    public void SanitizePermissive_NormalizesCarriageReturnLineFeed()
    {
        var input = "line1\r\nline2";
        var result = TextSanitizer.SanitizePermissive(input);
        Assert.Equal("line1\nline2", result);
    }

    [Fact]
    public void SanitizePermissive_NormalizesCarriageReturn()
    {
        var input = "line1\rline2";
        var result = TextSanitizer.SanitizePermissive(input);
        Assert.Equal("line1\nline2", result);
    }

    [Fact]
    public void SanitizePermissive_PreservesEmojis()
    {
        var input = "Hello \U0001F44B World!";
        var result = TextSanitizer.SanitizePermissive(input);
        Assert.Equal(input, result);
    }

    [Fact]
    public void SanitizePermissive_PreservesUnicodeCharacters()
    {
        var input = "Caf\u00E9 with \u2615";
        var result = TextSanitizer.SanitizePermissive(input);
        Assert.Equal(input, result);
    }

    [Fact]
    public void SanitizePermissive_PreservesCodeBlock()
    {
        var input = @"```csharp
public class Foo<T> where T : class
{
    public T Value { get; set; }
}
```";
        var result = TextSanitizer.SanitizePermissive(input);
        Assert.Contains("<T>", result);
        Assert.Contains("T : class", result);
    }

    #endregion

    #region SanitizeStrict Tests

    [Fact]
    public void SanitizeStrict_ReturnsEmpty_WhenInputNull()
    {
        var result = TextSanitizer.SanitizeStrict(null!);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void SanitizeStrict_ReturnsEmpty_WhenInputEmpty()
    {
        var result = TextSanitizer.SanitizeStrict(string.Empty);
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void SanitizeStrict_TrimsWhitespace()
    {
        var result = TextSanitizer.SanitizeStrict("  hello world  ");
        Assert.Equal("hello world", result);
    }

    [Fact]
    public void SanitizeStrict_RemovesLessThan()
    {
        var result = TextSanitizer.SanitizeStrict("foo<bar");
        Assert.Equal("foobar", result);
    }

    [Fact]
    public void SanitizeStrict_RemovesGreaterThan()
    {
        var result = TextSanitizer.SanitizeStrict("foo>bar");
        Assert.Equal("foobar", result);
    }

    [Fact]
    public void SanitizeStrict_RemovesBackslash()
    {
        var result = TextSanitizer.SanitizeStrict("foo\\bar");
        Assert.Equal("foobar", result);
    }

    [Fact]
    public void SanitizeStrict_RemovesForwardSlash()
    {
        var result = TextSanitizer.SanitizeStrict("foo/bar");
        Assert.Equal("foobar", result);
    }

    [Fact]
    public void SanitizeStrict_RemovesBacktick()
    {
        var result = TextSanitizer.SanitizeStrict("foo`bar");
        Assert.Equal("foobar", result);
    }

    [Fact]
    public void SanitizeStrict_RemovesAllUnsafeCharacters()
    {
        var result = TextSanitizer.SanitizeStrict("<script>`alert`</script>");
        Assert.Equal("scriptalertscript", result);
    }

    [Fact]
    public void SanitizeStrict_RemovesControlCharacters()
    {
        var result = TextSanitizer.SanitizeStrict("hello\0\a\bworld");
        Assert.Equal("helloworld", result);
    }

    [Fact]
    public void SanitizeStrict_PreservesAmpersand()
    {
        var result = TextSanitizer.SanitizeStrict("foo & bar");
        Assert.Equal("foo & bar", result);
    }

    [Fact]
    public void SanitizeStrict_PreservesEmojis()
    {
        var result = TextSanitizer.SanitizeStrict("My \U0001F4DA Deck");
        Assert.Equal("My \U0001F4DA Deck", result);
    }

    #endregion

    #region IsValidStrict Tests

    [Fact]
    public void IsValidStrict_ReturnsTrue_WhenInputNull()
    {
        var result = TextSanitizer.IsValidStrict(null!);
        Assert.True(result);
    }

    [Fact]
    public void IsValidStrict_ReturnsTrue_WhenInputEmpty()
    {
        var result = TextSanitizer.IsValidStrict(string.Empty);
        Assert.True(result);
    }

    [Fact]
    public void IsValidStrict_ReturnsTrue_WhenCleanInput()
    {
        var result = TextSanitizer.IsValidStrict("Hello World");
        Assert.True(result);
    }

    [Fact]
    public void IsValidStrict_ReturnsFalse_WhenContainsLessThan()
    {
        var result = TextSanitizer.IsValidStrict("foo<bar");
        Assert.False(result);
    }

    [Fact]
    public void IsValidStrict_ReturnsFalse_WhenContainsGreaterThan()
    {
        var result = TextSanitizer.IsValidStrict("foo>bar");
        Assert.False(result);
    }

    [Fact]
    public void IsValidStrict_ReturnsFalse_WhenContainsBackslash()
    {
        var result = TextSanitizer.IsValidStrict("foo\\bar");
        Assert.False(result);
    }

    [Fact]
    public void IsValidStrict_ReturnsFalse_WhenContainsForwardSlash()
    {
        var result = TextSanitizer.IsValidStrict("foo/bar");
        Assert.False(result);
    }

    [Fact]
    public void IsValidStrict_ReturnsFalse_WhenContainsBacktick()
    {
        var result = TextSanitizer.IsValidStrict("foo`bar");
        Assert.False(result);
    }

    [Fact]
    public void IsValidStrict_ReturnsFalse_WhenContainsNullCharacter()
    {
        var result = TextSanitizer.IsValidStrict("foo\0bar");
        Assert.False(result);
    }

    [Fact]
    public void IsValidStrict_ReturnsTrue_WhenContainsNewlineAndTab()
    {
        var result = TextSanitizer.IsValidStrict("foo\n\tbar");
        Assert.True(result);
    }

    #endregion

    #region GetInvalidStrictChars Tests

    [Fact]
    public void GetInvalidStrictChars_ReturnsEmpty_WhenInputNull()
    {
        var result = TextSanitizer.GetInvalidStrictChars(null!);
        Assert.Empty(result);
    }

    [Fact]
    public void GetInvalidStrictChars_ReturnsEmpty_WhenInputEmpty()
    {
        var result = TextSanitizer.GetInvalidStrictChars(string.Empty);
        Assert.Empty(result);
    }

    [Fact]
    public void GetInvalidStrictChars_ReturnsEmpty_WhenCleanInput()
    {
        var result = TextSanitizer.GetInvalidStrictChars("Hello World");
        Assert.Empty(result);
    }

    [Fact]
    public void GetInvalidStrictChars_ReturnsLessThan_WhenPresent()
    {
        var result = TextSanitizer.GetInvalidStrictChars("foo<bar");
        Assert.Single(result);
        Assert.Contains('<', result);
    }

    [Fact]
    public void GetInvalidStrictChars_ReturnsMultipleChars_WhenMultiplePresent()
    {
        var result = TextSanitizer.GetInvalidStrictChars("<script>`alert`</script>");
        Assert.Contains('<', result);
        Assert.Contains('>', result);
        Assert.Contains('`', result);
        Assert.Contains('/', result);
    }

    [Fact]
    public void GetInvalidStrictChars_ReturnsDistinctChars_WhenDuplicatesPresent()
    {
        var result = TextSanitizer.GetInvalidStrictChars("<<<>>>");
        Assert.Equal(2, result.Length);
        Assert.Contains('<', result);
        Assert.Contains('>', result);
    }

    [Fact]
    public void GetInvalidStrictChars_IncludesControlCharacters()
    {
        var result = TextSanitizer.GetInvalidStrictChars("foo\0bar");
        Assert.Single(result);
        Assert.Contains('\0', result);
    }

    [Fact]
    public void GetInvalidStrictChars_ExcludesAllowedWhitespace()
    {
        var result = TextSanitizer.GetInvalidStrictChars("foo\n\t\rbar");
        Assert.Empty(result);
    }

    #endregion

    #region IsValidPermissive Tests

    [Fact]
    public void IsValidPermissive_ReturnsTrue_WhenInputNull()
    {
        var result = TextSanitizer.IsValidPermissive(null!);
        Assert.True(result);
    }

    [Fact]
    public void IsValidPermissive_ReturnsTrue_WhenInputEmpty()
    {
        var result = TextSanitizer.IsValidPermissive(string.Empty);
        Assert.True(result);
    }

    [Fact]
    public void IsValidPermissive_ReturnsTrue_WhenContainsCodeCharacters()
    {
        var result = TextSanitizer.IsValidPermissive("<div>foo</div>");
        Assert.True(result);
    }

    [Fact]
    public void IsValidPermissive_ReturnsFalse_WhenContainsNullCharacter()
    {
        var result = TextSanitizer.IsValidPermissive("foo\0bar");
        Assert.False(result);
    }

    [Fact]
    public void IsValidPermissive_ReturnsFalse_WhenContainsBellCharacter()
    {
        var result = TextSanitizer.IsValidPermissive("foo\abar");
        Assert.False(result);
    }

    [Fact]
    public void IsValidPermissive_ReturnsTrue_WhenContainsNewlineAndTab()
    {
        var result = TextSanitizer.IsValidPermissive("foo\n\tbar");
        Assert.True(result);
    }

    #endregion

    #region Real-World Scenarios

    [Fact]
    public void SanitizePermissive_HandlesJavaScriptCodeSnippet()
    {
        var input = "function greet(name) {\n  return `Hello, ${name}!`;\n}";
        var result = TextSanitizer.SanitizePermissive(input);
        Assert.Contains("`Hello,", result);
        Assert.Contains("${name}", result);
    }

    [Fact]
    public void SanitizePermissive_HandlesHtmlSnippet()
    {
        var input = "<button onclick=\"alert('hi')\">Click</button>";
        var result = TextSanitizer.SanitizePermissive(input);
        Assert.Equal(input, result);
    }

    [Fact]
    public void SanitizePermissive_HandlesSqlSnippet()
    {
        var input = "SELECT * FROM users WHERE age > 18 AND name <> 'admin';";
        var result = TextSanitizer.SanitizePermissive(input);
        Assert.Equal(input, result);
    }

    [Fact]
    public void SanitizeStrict_CreatesCleanDeckName()
    {
        var input = "My <Cool> Deck/Collection";
        var result = TextSanitizer.SanitizeStrict(input);
        Assert.Equal("My Cool DeckCollection", result);
    }

    [Fact]
    public void SanitizeStrict_CreatesCleanTagName()
    {
        var input = "programming/javascript";
        var result = TextSanitizer.SanitizeStrict(input);
        Assert.Equal("programmingjavascript", result);
    }

    #endregion
}
