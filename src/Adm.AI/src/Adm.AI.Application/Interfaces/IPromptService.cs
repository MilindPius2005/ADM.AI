namespace Adm.AI.Application.Interfaces;

using Adm.AI.Application.DTOs.Internal;

public interface IPromptService
{
    /// <summary>
    /// Loads and caches a prompt template by name.
    /// Reads from: {PromptSettings.BasePath}/{promptName}/
    ///   - system_prompt.txt
    ///   - user_prompt_template.txt
    ///   - schema.json
    ///   - version.txt
    /// Cached in static ConcurrentDictionary after first load.
    /// Generic interface — only "sds" prompt implemented in V1.
    /// </summary>
    Task<PromptTemplate> GetPromptAsync(string promptName, CancellationToken ct = default);
}
