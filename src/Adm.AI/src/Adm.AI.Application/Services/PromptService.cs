using System.Collections.Concurrent;
using Adm.AI.Application.Configuration;
using Adm.AI.Application.DTOs.Internal;
using Adm.AI.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace Adm.AI.Application.Services;

public class PromptService : IPromptService
{
    private readonly PromptSettings _settings;
    private static readonly ConcurrentDictionary<string, PromptTemplate> _cache = new();

    public PromptService(IOptions<PromptSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task<PromptTemplate> GetPromptAsync(string promptName, CancellationToken ct = default)
    {
        if (_cache.TryGetValue(promptName, out var cachedTemplate))
        {
            return cachedTemplate;
        }

        var promptPath = Path.Combine(_settings.BasePath, promptName);

        if (!Directory.Exists(promptPath))
        {
            throw new DirectoryNotFoundException($"Prompt directory not found: {promptPath}");
        }

        var systemPromptPath = Path.Combine(promptPath, "system_prompt.txt");
        var userPromptPath = Path.Combine(promptPath, "user_prompt_template.txt");
        var schemaPath = Path.Combine(promptPath, "schema.json");
        var versionPath = Path.Combine(promptPath, "version.txt");

        var systemPrompt = await File.ReadAllTextAsync(systemPromptPath, ct);
        var userPromptTemplate = await File.ReadAllTextAsync(userPromptPath, ct);
        var schema = await File.ReadAllTextAsync(schemaPath, ct);
        var version = await File.ReadAllTextAsync(versionPath, ct);

        var template = new PromptTemplate(
            systemPrompt.Trim(),
            userPromptTemplate.Trim(),
            schema.Trim(),
            version.Trim()
        );

        _cache.TryAdd(promptName, template);
        return template;
    }
}
