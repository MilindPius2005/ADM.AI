namespace Adm.AI.Application.Interfaces;

public interface ITextCleaningService
{
    /// <summary>
    /// Cleans and normalizes raw extracted text. Returns AI-ready text.
    /// Steps (in order):
    ///   1. Remove repeated headers/footers (patterns appearing on 80%+ of pages)
    ///   2. Normalize Unicode (smart quotes, em-dashes, NBSP → ASCII)
    ///   3. Fix broken words (re-join "haz-\nardous" → "hazardous")
    ///   4. Normalize whitespace (collapse multiple spaces, collapse 3+ blank lines to 2)
    ///   5. Preserve section headings (SECTION 1:, Section 2 -, etc.)
    ///   6. Never modify: CAS numbers, UN numbers, H/P codes, concentrations, units
    ///   7. Final trim
    /// Pure function. Synchronous.
    /// </summary>
    string Clean(string rawText);
}
