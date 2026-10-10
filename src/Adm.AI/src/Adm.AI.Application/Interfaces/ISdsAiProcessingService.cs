namespace Adm.AI.Application.Interfaces;

using Adm.AI.Application.DTOs.Internal;

public interface ISdsAiProcessingService
{
    /// <summary>
    /// Sends document text to OpenAI with the SDS prompt and JSON schema.
    /// Uses Structured Outputs (strict mode) for schema-enforced JSON.
    /// Handles deserialization of response into SdsExtractionResult.
    /// On deserialization failure: retries ONCE with the same prompt.
    /// On second failure: throws AiProcessingException.
    /// </summary>
    Task<SdsAiResult> ProcessAsync(
        PromptTemplate prompt,
        string documentText,
        string schemaName = "sds_extraction",
        CancellationToken ct = default);

    Task<List<Adm.AI.Domain.Models.GhsPictogram>> DetectPictogramsAsync(PromptTemplate prompt, List<byte[]> images, CancellationToken ct = default);
}
