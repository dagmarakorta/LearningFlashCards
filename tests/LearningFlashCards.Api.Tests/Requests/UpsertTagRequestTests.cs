using System.ComponentModel.DataAnnotations;
using LearningFlashCards.Api.Controllers.Requests;

namespace LearningFlashCards.Api.Tests.Requests;

public class UpsertTagRequestTests
{
    [Fact]
    public void Validation_Fails_WhenNameIsEmpty()
    {
        var request = new UpsertTagRequest
        {
            Name = ""
        };

        var results = ValidateModel(request);

        Assert.Contains(results, r => r.MemberNames.Contains("Name"));
    }

    [Fact]
    public void Validation_Fails_WhenNameIsNull()
    {
        var request = new UpsertTagRequest
        {
            Name = null!
        };

        var results = ValidateModel(request);

        Assert.Contains(results, r => r.MemberNames.Contains("Name"));
    }

    [Fact]
    public void Validation_Fails_WhenNameExceedsMaxLength()
    {
        var request = new UpsertTagRequest
        {
            Name = new string('a', 129)
        };

        var results = ValidateModel(request);

        Assert.Contains(results, r => r.MemberNames.Contains("Name"));
    }

    [Fact]
    public void Validation_Succeeds_WithValidRequest()
    {
        var request = new UpsertTagRequest
        {
            Name = "Important"
        };

        var results = ValidateModel(request);

        Assert.Empty(results);
    }

    [Fact]
    public void Validation_Succeeds_WithMaxLengthName()
    {
        var request = new UpsertTagRequest
        {
            Name = new string('a', 128)
        };

        var results = ValidateModel(request);

        Assert.Empty(results);
    }

    [Fact]
    public void Validation_Succeeds_WithMinLengthName()
    {
        var request = new UpsertTagRequest
        {
            Name = "A"
        };

        var results = ValidateModel(request);

        Assert.Empty(results);
    }

    [Fact]
    public void Validation_Succeeds_WithOptionalId()
    {
        var request = new UpsertTagRequest
        {
            Id = Guid.NewGuid(),
            Name = "Tag"
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
