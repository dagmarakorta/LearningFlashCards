using System.ComponentModel.DataAnnotations;
using LearningFlashCards.Api.Controllers.Requests;

namespace LearningFlashCards.Api.Tests.Requests;

public class UpsertDeckRequestTests
{
    [Fact]
    public void Validation_Fails_WhenNameIsEmpty()
    {
        var request = new UpsertDeckRequest
        {
            Name = ""
        };

        var results = ValidateModel(request);

        Assert.Contains(results, r => r.MemberNames.Contains("Name"));
    }

    [Fact]
    public void Validation_Fails_WhenNameIsNull()
    {
        var request = new UpsertDeckRequest
        {
            Name = null!
        };

        var results = ValidateModel(request);

        Assert.Contains(results, r => r.MemberNames.Contains("Name"));
    }

    [Fact]
    public void Validation_Fails_WhenNameExceedsMaxLength()
    {
        var request = new UpsertDeckRequest
        {
            Name = new string('a', 257)
        };

        var results = ValidateModel(request);

        Assert.Contains(results, r => r.MemberNames.Contains("Name"));
    }

    [Fact]
    public void Validation_Fails_WhenDescriptionExceedsMaxLength()
    {
        var request = new UpsertDeckRequest
        {
            Name = "Valid name",
            Description = new string('d', 1025)
        };

        var results = ValidateModel(request);

        Assert.Contains(results, r => r.MemberNames.Contains("Description"));
    }

    [Fact]
    public void Validation_Succeeds_WhenDescriptionIsNull()
    {
        var request = new UpsertDeckRequest
        {
            Name = "Valid name",
            Description = null
        };

        var results = ValidateModel(request);

        Assert.Empty(results);
    }

    [Fact]
    public void Validation_Succeeds_WithValidRequest()
    {
        var request = new UpsertDeckRequest
        {
            Name = "My Deck",
            Description = "A deck for learning"
        };

        var results = ValidateModel(request);

        Assert.Empty(results);
    }

    [Fact]
    public void Validation_Succeeds_WithMaxLengthValues()
    {
        var request = new UpsertDeckRequest
        {
            Name = new string('a', 256),
            Description = new string('d', 1024)
        };

        var results = ValidateModel(request);

        Assert.Empty(results);
    }

    [Fact]
    public void Validation_Succeeds_WithMinLengthName()
    {
        var request = new UpsertDeckRequest
        {
            Name = "A"
        };

        var results = ValidateModel(request);

        Assert.Empty(results);
    }

    [Fact]
    public void Validation_Succeeds_WithOptionalId()
    {
        var request = new UpsertDeckRequest
        {
            Id = Guid.NewGuid(),
            Name = "Deck"
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
