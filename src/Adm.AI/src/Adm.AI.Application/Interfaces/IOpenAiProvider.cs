namespace Adm.AI.Application.Interfaces;

using Adm.AI.Application.DTOs.Internal;

public interface IOpenAiProvider
{
    /// <summary>
    /// Calls OpenAI Chat Completion with Structured Outputs.
    /// Returns raw JSON string + token usage + cost estimate.
    /// Transient HTTP errors (429, 500, 503) handled by SDK built-in retry.
    /// Timeout configured via OpenAiSettings.TimeoutSeconds.
    /// </summary>
    Task<AiCompletionResult> GetStructuredCompletionAsync(
        string systemPrompt,
        string userPrompt,
        string jsonSchemaName,
        string jsonSchema,
        CancellationToken ct = default);

    Task<AiCompletionResult> GetStructuredVisionCompletionAsync(
        string systemPrompt,
        string userPrompt,
        List<byte[]> imageBytesList,
        string mimeType,
        string jsonSchemaName,
        string jsonSchema,
        CancellationToken ct = default);

}
