using System.Text.Json.Serialization;

namespace Adm.AI.Domain.Models;

public class GhsPictogram
{
    [JsonPropertyName("ghsCode")]
    public string? GhsCode { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}
