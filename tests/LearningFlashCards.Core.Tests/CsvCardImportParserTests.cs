using LearningFlashCards.Core.Application.Import;

namespace LearningFlashCards.Core.Tests;

public class CsvCardImportParserTests
{
    private readonly CsvCardImportParser _parser = new();

    [Fact]
    public void Parse_SupportsHeaderAndMultilineFields()
    {
        var content = """
Front,Back
"<div>
<h2>Hello</h2>
</div>","<p>Back line 1
Back line 2</p>"
""";

        var result = _parser.Parse(content);

        Assert.Single(result.Cards);
        Assert.Empty(result.InvalidRowNumbers);
        Assert.Contains("<h2>Hello</h2>", result.Cards[0].Front);
        Assert.Contains("Back line 2", result.Cards[0].Back);
    }

    [Fact]
    public void Parse_HandlesCommasAndEscapedQuotes()
    {
        var content = "Front,Back\n\"Hello, \"\"world\"\"\",\"Back, with comma\"";

        var result = _parser.Parse(content);

        Assert.Single(result.Cards);
        Assert.Empty(result.InvalidRowNumbers);
        Assert.Equal("Hello, \"world\"", result.Cards[0].Front);
        Assert.Equal("Back, with comma", result.Cards[0].Back);
    }

    [Fact]
    public void Parse_ReportsRowsMissingFrontOrBack()
    {
        var content = "Front,Back\n\"Front only\",\n,Back only\n\"Valid\",\"Row\"";

        var result = _parser.Parse(content);

        Assert.Single(result.Cards);
        Assert.Equal("Valid", result.Cards[0].Front);
        Assert.Equal("Row", result.Cards[0].Back);
        Assert.Equal(new[] { 1, 2 }, result.InvalidRowNumbers);
    }

    [Fact]
    public void Parse_AllowsBomHeader()
    {
        var content = "\uFEFFFront,Back\n\"Hi\",\"There\"";

        var result = _parser.Parse(content);

        Assert.Single(result.Cards);
        Assert.Empty(result.InvalidRowNumbers);
        Assert.Equal("Hi", result.Cards[0].Front);
        Assert.Equal("There", result.Cards[0].Back);
    }
}
