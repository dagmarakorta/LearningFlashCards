using System.ComponentModel.DataAnnotations;
using LearningFlashCards.Api.Controllers.Requests;

namespace LearningFlashCards.Api.Tests.Requests;

public class UpsertCardRequestTests
{
    [Fact]
    public void Validation_Fails_WhenFrontIsEmpty()
    {
        var request = new UpsertCardRequest
        {
            Front = "",
            Back = "Valid back"
        };

        var results = ValidateModel(request);

        Assert.Contains(results, r => r.MemberNames.Contains("Front"));
    }

    [Fact]
    public void Validation_Fails_WhenFrontIsNull()
    {
        var request = new UpsertCardRequest
        {
            Front = null!,
            Back = "Valid back"
        };

        var results = ValidateModel(request);

        Assert.Contains(results, r => r.MemberNames.Contains("Front"));
    }

    [Fact]
    public void Validation_Fails_WhenFrontExceedsMaxLength()
    {
        var request = new UpsertCardRequest
        {
            Front = new string('a', 2049),
            Back = "Valid back"
        };

        var results = ValidateModel(request);

        Assert.Contains(results, r => r.MemberNames.Contains("Front"));
    }

    [Fact]
    public void Validation_Fails_WhenBackIsEmpty()
    {
        var request = new UpsertCardRequest
        {
            Front = "Valid front",
            Back = ""
        };

        var results = ValidateModel(request);

        Assert.Contains(results, r => r.MemberNames.Contains("Back"));
    }

    [Fact]
    public void Validation_Fails_WhenBackIsNull()
    {
        var request = new UpsertCardRequest
        {
            Front = "Valid front",
            Back = null!
        };

        var results = ValidateModel(request);

        Assert.Contains(results, r => r.MemberNames.Contains("Back"));
    }

    [Fact]
    public void Validation_Fails_WhenBackExceedsMaxLength()
    {
        var request = new UpsertCardRequest
        {
            Front = "Valid front",
            Back = new string('b', 2049)
        };

        var results = ValidateModel(request);

        Assert.Contains(results, r => r.MemberNames.Contains("Back"));
    }

    [Fact]
    public void Validation_Fails_WhenNotesExceedsMaxLength()
    {
        var request = new UpsertCardRequest
        {
            Front = "Valid front",
            Back = "Valid back",
            Notes = new string('n', 4097)
        };

        var results = ValidateModel(request);

        Assert.Contains(results, r => r.MemberNames.Contains("Notes"));
    }

    [Fact]
    public void Validation_Succeeds_WhenNotesIsNull()
    {
        var request = new UpsertCardRequest
        {
            Front = "Valid front",
            Back = "Valid back",
            Notes = null
        };

        var results = ValidateModel(request);

        Assert.Empty(results);
    }

    [Fact]
    public void Validation_Succeeds_WithValidRequest()
    {
        var request = new UpsertCardRequest
        {
            Front = "What is 2+2?",
            Back = "4",
            Notes = "Basic math"
        };

        var results = ValidateModel(request);

        Assert.Empty(results);
    }

    [Fact]
    public void Validation_Succeeds_WithMaxLengthValues()
    {
        var request = new UpsertCardRequest
        {
            Front = new string('a', 2048),
            Back = new string('b', 2048),
            Notes = new string('n', 4096)
        };

        var results = ValidateModel(request);

        Assert.Empty(results);
    }

    [Fact]
    public void Validation_Succeeds_WithOptionalId()
    {
        var request = new UpsertCardRequest
        {
            Id = Guid.NewGuid(),
            Front = "Front",
            Back = "Back"
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
