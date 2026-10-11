namespace Adm.AI.Application.Configuration;

public class AiProcessingSettings
{
    public const string SectionName = "AiProcessing";
    public int MaxConcurrentRequests { get; set; } = 3;
    public int MaxConcurrentAiCalls { get; set; } = 5;
    public int MaxBatchSize { get; set; } = 10;
}
