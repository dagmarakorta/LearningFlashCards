using LearningFlashCards.Infrastructure.Persistence;

namespace LearningFlashCards.Infrastructure.Tests.Persistence;

public class SyncTokenHelperTests
{
    [Fact]
    public void Parse_WithValidIso8601Token_ReturnsDateTimeOffset()
    {
        // Arrange
        var expectedDate = new DateTimeOffset(2026, 1, 12, 10, 30, 45, TimeSpan.Zero);
        var token = expectedDate.ToString("O");

        // Act
        var result = SyncTokenHelper.Parse(token);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedDate, result.Value);
    }

    [Fact]
    public void Parse_WithNullToken_ReturnsNull()
    {
        // Act
        var result = SyncTokenHelper.Parse(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Parse_WithEmptyString_ReturnsNull()
    {
        // Act
        var result = SyncTokenHelper.Parse(string.Empty);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Parse_WithWhitespaceString_ReturnsNull()
    {
        // Act
        var result = SyncTokenHelper.Parse("   ");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Parse_WithInvalidToken_ReturnsNull()
    {
        // Act
        var result = SyncTokenHelper.Parse("invalid-token");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Parse_WithMalformedDateString_ReturnsNull()
    {
        // Act
        var result = SyncTokenHelper.Parse("2026-13-45T99:99:99");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Parse_WithPartialDateString_ParsesSuccessfully()
    {
        // Arrange
        var token = "2026-01-12";

        // Act
        var result = SyncTokenHelper.Parse(token);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2026, result.Value.Year);
        Assert.Equal(1, result.Value.Month);
        Assert.Equal(12, result.Value.Day);
    }

    [Fact]
    public void Parse_WithDifferentDateFormats_ParsesSuccessfully()
    {
        // Arrange
        var token = "01/12/2026 10:30:45 AM";

        // Act
        var result = SyncTokenHelper.Parse(token);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void NewToken_ReturnsValidIso8601Format()
    {
        // Act
        var token = SyncTokenHelper.NewToken();

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);

        // Verify it can be parsed back
        var parsed = DateTimeOffset.Parse(token);
        Assert.NotEqual(default(DateTimeOffset), parsed);
    }

    [Fact]
    public void NewToken_ReturnsUtcTime()
    {
        // Act
        var token = SyncTokenHelper.NewToken();
        var parsed = DateTimeOffset.Parse(token);

        // Assert
        Assert.Equal(TimeSpan.Zero, parsed.Offset);
    }

    [Fact]
    public void NewToken_ReturnsCurrentTime()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;

        // Act
        var token = SyncTokenHelper.NewToken();
        var parsed = DateTimeOffset.Parse(token);

        // Arrange
        var after = DateTimeOffset.UtcNow;

        // Assert
        Assert.True(parsed >= before);
        Assert.True(parsed <= after);
    }

    [Fact]
    public void NewToken_GeneratesUniqueTokensOverTime()
    {
        // Act
        var token1 = SyncTokenHelper.NewToken();
        Thread.Sleep(10); // Small delay to ensure different timestamps
        var token2 = SyncTokenHelper.NewToken();

        // Assert
        Assert.NotEqual(token1, token2);
    }

    [Fact]
    public void Parse_RoundTrip_PreservesValue()
    {
        // Arrange
        var originalToken = SyncTokenHelper.NewToken();

        // Act
        var parsed = SyncTokenHelper.Parse(originalToken);
        var roundTripToken = parsed!.Value.ToString("O");

        // Assert
        Assert.Equal(originalToken, roundTripToken);
    }

    [Fact]
    public void Parse_WithTokenGeneratedByNewToken_Succeeds()
    {
        // Arrange
        var token = SyncTokenHelper.NewToken();

        // Act
        var result = SyncTokenHelper.Parse(token);

        // Assert
        Assert.NotNull(result);
        Assert.True((DateTimeOffset.UtcNow - result.Value).TotalSeconds < 1);
    }
}
