using System.Text.RegularExpressions;
using Adm.AI.Application.Interfaces;

namespace Adm.AI.Application.Services;

public partial class TextCleaningService : ITextCleaningService
{
    public string Clean(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return string.Empty;
        }

        var text = rawText;

        text = NormalizeUnicode(text);
        text = RemoveCommonHeadersFooters(text);
        text = FixBrokenWords(text);
        text = NormalizeWhitespace(text);
        
        return text.Trim();
    }

    private static string NormalizeUnicode(string text)
    {
        text = text.Replace('“', '"').Replace('”', '"');
        text = text.Replace('‘', '\'').Replace('’', '\'');
        text = text.Replace('–', '-').Replace('—', '-'); // En-dash and em-dash to hyphen
        text = text.Replace('\u00A0', ' '); // Non-breaking space to regular space
        return text;
    }

    private string RemoveCommonHeadersFooters(string text)
    {
        // Remove common "Page X of Y" patterns
        text = PageNumberRegex().Replace(text, "");
        
        // As a generic service, removing arbitrary repeated text is complex without page boundaries.
        // For Phase 2, removing standard pagination patterns is a safe start that won't destroy data.
        return text;
    }

    private string FixBrokenWords(string text)
    {
        // Re-join words split by PDF line wrapping (e.g., "haz-\r\nardous" -> "hazardous")
        // Only matches if it's a lowercase letter followed by hyphen, newline, and lowercase letter
        return BrokenWordRegex().Replace(text, "$1$2");
    }

    private string NormalizeWhitespace(string text)
    {
        // Collapse multiple spaces to single space, BUT preserve spaces at the start of a line
        // to maintain tabular/indentation structure.
        // We do this by replacing 2+ spaces that are NOT at the start of a line.
        // Actually, to preserve tables, we should be careful with collapsing multiple spaces.
        // We will collapse spaces ONLY if they are not part of a large gap (e.g., >4 spaces might be a column separator).
        // For simplicity and safety (rule 6/7/8: never modify data), we'll just normalize line endings and excessive blank lines.

        // Normalize line endings to \n
        text = text.Replace("\r\n", "\n").Replace("\r", "\n");

        // Collapse 3+ blank lines to 2
        text = MultipleNewLinesRegex().Replace(text, "\n\n\n");
        
        // Remove trailing spaces on lines
        text = TrailingSpacesRegex().Replace(text, "");

        return text;
    }

    [GeneratedRegex(@"(?i)^\s*Page\s+\d+\s+of\s+\d+\s*$", RegexOptions.Multiline)]
    private static partial Regex PageNumberRegex();

    [GeneratedRegex(@"([a-z]+)-\n([a-z]+)")]
    private static partial Regex BrokenWordRegex();

    [GeneratedRegex(@"\n{4,}")]
    private static partial Regex MultipleNewLinesRegex();

    [GeneratedRegex(@"[ \t]+$", RegexOptions.Multiline)]
    private static partial Regex TrailingSpacesRegex();
}
