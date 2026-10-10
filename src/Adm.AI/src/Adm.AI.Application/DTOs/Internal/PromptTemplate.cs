namespace Adm.AI.Application.DTOs.Internal;

/// <summary>
/// Returned by IPromptService. Contains all prompt components for a given prompt name.
/// </summary>
public record PromptTemplate(
    string SystemPrompt,
    string UserPromptTemplate,
    string JsonSchema,
    string Version
);
