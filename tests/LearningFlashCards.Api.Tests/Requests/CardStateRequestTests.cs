using System.ComponentModel.DataAnnotations;
using LearningFlashCards.Api.Controllers.Requests;

namespace LearningFlashCards.Api.Tests.Requests;

public class CardStateRequestTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validation_Fails_WhenIntervalDaysIsNegative(int intervalDays)
    {
        var request = new CardStateRequest
        {
            IntervalDays = intervalDays
        };

        var results = ValidateModel(request);

        Assert.Contains(results, r => r.MemberNames.Contains("IntervalDays"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(365)]
    public void Validation_Succeeds_WhenIntervalDaysIsNonNegative(int intervalDays)
    {
        var request = new CardStateRequest
        {
            IntervalDays = intervalDays
        };

        var results = ValidateModel(request);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains("IntervalDays"));
    }

    [Theory]
    [InlineData(1.2)]
    [InlineData(0.5)]
    [InlineData(5.1)]
    [InlineData(6.0)]
    public void Validation_Fails_WhenEaseFactorOutOfRange(double easeFactor)
    {
        var request = new CardStateRequest
        {
            EaseFactor = easeFactor
        };

        var results = ValidateModel(request);

        Assert.Contains(results, r => r.MemberNames.Contains("EaseFactor"));
    }

    [Theory]
    [InlineData(1.3)]
    [InlineData(2.5)]
    [InlineData(5.0)]
    public void Validation_Succeeds_WhenEaseFactorInRange(double easeFactor)
    {
        var request = new CardStateRequest
        {
            EaseFactor = easeFactor
        };

        var results = ValidateModel(request);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains("EaseFactor"));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-50)]
    public void Validation_Fails_WhenStreakIsNegative(int streak)
    {
        var request = new CardStateRequest
        {
            Streak = streak
        };

        var results = ValidateModel(request);

        Assert.Contains(results, r => r.MemberNames.Contains("Streak"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public void Validation_Succeeds_WhenStreakIsNonNegative(int streak)
    {
        var request = new CardStateRequest
        {
            Streak = streak
        };

        var results = ValidateModel(request);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains("Streak"));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Validation_Fails_WhenLapsesIsNegative(int lapses)
    {
        var request = new CardStateRequest
        {
            Lapses = lapses
        };

        var results = ValidateModel(request);

        Assert.Contains(results, r => r.MemberNames.Contains("Lapses"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void Validation_Succeeds_WhenLapsesIsNonNegative(int lapses)
    {
        var request = new CardStateRequest
        {
            Lapses = lapses
        };

        var results = ValidateModel(request);

        Assert.DoesNotContain(results, r => r.MemberNames.Contains("Lapses"));
    }

    [Fact]
    public void Validation_Succeeds_WithAllFieldsNull()
    {
        var request = new CardStateRequest();

        var results = ValidateModel(request);

        Assert.Empty(results);
    }

    [Fact]
    public void Validation_Succeeds_WithValidCompleteRequest()
    {
        var request = new CardStateRequest
        {
            DueAt = DateTimeOffset.UtcNow,
            IntervalDays = 7,
            EaseFactor = 2.5,
            Streak = 3,
            Lapses = 1
        };

        var results = ValidateModel(request);

        Assert.Empty(results);
    }

    private static List<ValidationResult> ValidateModel(object model)
    {
        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }
}
