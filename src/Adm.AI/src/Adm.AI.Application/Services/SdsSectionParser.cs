using System.Text.RegularExpressions;
using Adm.AI.Application.DTOs.Internal;
using Adm.AI.Application.Interfaces;

namespace Adm.AI.Application.Services;

public class SdsSectionParser : ISdsSectionParser
{
    // Standard GHS SDS section titles (keywords) for validation
    private static readonly Dictionary<int, string[]> ExpectedTitles = new()
    {
        { 1,  new[] { "identification" } },
        { 2,  new[] { "hazard" } },
        { 3,  new[] { "composition", "ingredient" } },
        { 4,  new[] { "first", "aid" } },
        { 5,  new[] { "fire" } },
        { 6,  new[] { "accidental", "release" } },
        { 7,  new[] { "handling", "storage" } },
        { 8,  new[] { "exposure", "protection" } },
        { 9,  new[] { "physical", "chemical" } },
        { 10, new[] { "stability", "reactivity" } },
        { 11, new[] { "toxicolog" } },
        { 12, new[] { "ecolog" } },
        { 13, new[] { "disposal" } },
        { 14, new[] { "transport" } },
        { 15, new[] { "regulat" } },
        { 16, new[] { "other", "additional" } },
    };

    public List<SdsSection> ParseSections(string text)
    {
        var sections = new List<SdsSection>();
        if (string.IsNullOrWhiteSpace(text)) return sections;

        // Matches: "SECTION 1:", "Section 1 -", "1. Identification", "SECTION 1: Identification"
        // Requires: section number 1-16, title must start with a letter (not a digit/unit/measurement)
        var regex = new Regex(
            @"^(?:SECTION\s+)?(\d{1,2})[\.\:\-\s]+([A-Za-z][A-Za-z\s\(\)/\-,]+)$",
            RegexOptions.Multiline | RegexOptions.IgnoreCase);

        var matches = regex.Matches(text);
        var validatedMatches = new List<(Match match, int sectionNum, string title)>();

        for (int i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            if (!int.TryParse(match.Groups[1].Value, out int sectionNum)) continue;
            if (sectionNum < 1 || sectionNum > 16) continue;

            var title = match.Groups[2].Value.Trim();

            // Validate: the title must loosely match an expected SDS section title
            if (ExpectedTitles.TryGetValue(sectionNum, out var keywords))
            {
                var titleLower = title.ToLowerInvariant();
                if (!keywords.Any(kw => titleLower.Contains(kw)))
                {
                    continue; // Skip false positives like "3. Packing Group"
                }
            }

            // Avoid duplicates: keep only the first occurrence of each section number
            if (validatedMatches.Any(v => v.sectionNum == sectionNum)) continue;

            validatedMatches.Add((match, sectionNum, title));
        }

        // Build sections with correct content boundaries
        for (int i = 0; i < validatedMatches.Count; i++)
        {
            var (match, sectionNum, title) = validatedMatches[i];
            int startIndex = match.Index;
            int endIndex = (i + 1 < validatedMatches.Count)
                ? validatedMatches[i + 1].match.Index
                : text.Length;

            string content = text.Substring(startIndex, endIndex - startIndex).Trim();
            sections.Add(new SdsSection(sectionNum, title, content));
        }

        return sections;
    }
}
