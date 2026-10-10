namespace Adm.AI.Application.Configuration;

using System.ComponentModel.DataAnnotations;

public class PromptSettings
{
    public const string SectionName = "Prompts";

    [Required] public string BasePath { get; set; } = "prompts";
}
