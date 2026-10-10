using Adm.AI.Application.DTOs.Internal;

namespace Adm.AI.Application.Interfaces;

public interface ISdsSectionParser
{
    List<SdsSection> ParseSections(string text);
}
