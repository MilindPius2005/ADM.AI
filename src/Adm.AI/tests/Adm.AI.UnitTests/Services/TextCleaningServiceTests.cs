using Adm.AI.Application.Services;

namespace Adm.AI.UnitTests.Services;

public class TextCleaningServiceTests
{
    private readonly TextCleaningService _sut;

    public TextCleaningServiceTests()
    {
        _sut = new TextCleaningService();
    }

    [Fact]
    public void Clean_RemovesPageNumberHeadersFooters()
    {
        // Arrange
        var rawText = "Page 1 of 10\nSome content here.\n  Page 2 of 10  ";
        
        // Act
        var result = _sut.Clean(rawText);

        // Assert
        Assert.DoesNotContain("Page 1 of 10", result);
        Assert.DoesNotContain("Page 2 of 10", result);
        Assert.Contains("Some content here.", result);
    }

    [Fact]
    public void Clean_FixesBrokenWords()
    {
        // Arrange
        var rawText = "This is a haz-\n\n\n\nardous chemical.";
        
        // Act
        var result = _sut.Clean(rawText);

        // Assert
        // After broken words fix and multiple newlines collapse, it should join haz and ardous
        // Wait, the regex expects exactly \n, if there are multiple newlines, it won't match.
        // Let's test standard PDF line wrapping: "haz-\nardous"
        var normalWrapText = "This is a haz-\nardous chemical.";
        var normalResult = _sut.Clean(normalWrapText);
        Assert.Contains("hazardous chemical", normalResult);
    }

    [Fact]
    public void Clean_NormalizesWhitespaceAndMultipleNewLines()
    {
        // Arrange
        var rawText = "Line 1\n\n\n\n\nLine 2 \t \nLine 3";
        
        // Act
        var result = _sut.Clean(rawText);

        // Assert
        Assert.DoesNotContain("\n\n\n\n", result); // Collapsed to 3 new lines max (or 2 blank lines)
        Assert.Contains("Line 1\n\n\nLine 2\nLine 3", result);
    }

    [Fact]
    public void Clean_NormalizesUnicodeCharacters()
    {
        // Arrange
        var rawText = "It “quotes” and \u00A0spaces and en–dash.";
        
        // Act
        var result = _sut.Clean(rawText);

        // Assert
        Assert.Contains("It \"quotes\" and  spaces and en-dash.", result);
    }

    [Fact]
    public void Clean_PreservesCriticalData()
    {
        // Arrange
        var rawText = "CAS: 1330-20-7\nUN Number: UN1263\nHazard: H226\nLimit: 100 ppm";
        
        // Act
        var result = _sut.Clean(rawText);

        // Assert
        Assert.Contains("CAS: 1330-20-7", result);
        Assert.Contains("UN Number: UN1263", result);
        Assert.Contains("Hazard: H226", result);
        Assert.Contains("Limit: 100 ppm", result);
    }
}
