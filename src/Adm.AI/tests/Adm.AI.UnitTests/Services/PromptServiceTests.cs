using Adm.AI.Application.Configuration;
using Adm.AI.Application.Services;
using Microsoft.Extensions.Options;
namespace Adm.AI.UnitTests.Services;
public class PromptServiceTests : IDisposable
{
    private readonly string _tempPath;

    public PromptServiceTests()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempPath);
    }
    public void Dispose()
    {
        if (Directory.Exists(_tempPath))
        {
            Directory.Delete(_tempPath, true);
        }
    }
    [Fact]
    public async Task GetPromptAsync_LoadsFilesCorrectly()
    {
        var promptName = "sds";
        var promptDir = Path.Combine(_tempPath, promptName);
        Directory.CreateDirectory(promptDir);
        await File.WriteAllTextAsync(Path.Combine(promptDir, "system_prompt.txt"), "system");
        await File.WriteAllTextAsync(Path.Combine(promptDir, "user_prompt_template.txt"), "user");
        await File.WriteAllTextAsync(Path.Combine(promptDir, "schema.json"), "{}");
        await File.WriteAllTextAsync(Path.Combine(promptDir, "version.txt"), "1.0.0");
        var settings = Options.Create(new PromptSettings { BasePath = _tempPath });
        var service = new PromptService(settings);
        var result = await service.GetPromptAsync(promptName);
        Assert.Equal("system", result.SystemPrompt);
        Assert.Equal("user", result.UserPromptTemplate);
        Assert.Equal("{}", result.JsonSchema);
        Assert.Equal("1.0.0", result.Version);
    }
}