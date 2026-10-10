namespace Adm.AI.Application.Configuration;

using System.ComponentModel.DataAnnotations;

public class SdsChunkGroup
{
    [Required] public string Name { get; set; } = string.Empty;
    [Required] public List<int> Sections { get; set; } = new();
}

public class SdsChunkingSettings
{
    public const string SectionName = "SdsChunking";

    public bool Enabled { get; set; } = true;

    [Required] 
    public List<SdsChunkGroup> Groups { get; set; } = new();
}
