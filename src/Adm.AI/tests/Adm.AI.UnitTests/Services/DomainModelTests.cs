namespace Adm.AI.UnitTests.Services;

using Adm.AI.Domain.Entities;
using Adm.AI.Domain.Enums;
using Adm.AI.Domain.Models;

/// <summary>
/// Phase 1 verification tests — validates that domain models compile correctly
/// and can be instantiated with expected values.
/// </summary>
public class DomainModelTests
{
    [Fact]
    public void SdsAnalysis_Entity_CanBeCreated_WithAllProperties()
    {
        var analysis = new SdsAnalysis
        {
            Id = Guid.NewGuid(),
            FileHash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            FileName = "test_sds.pdf",
            FileSizeBytes = 1024,
            ExtractedText = "Section 1: Identification...",
            AnalysisResultJson = "{}",
            Status = AnalysisStatus.Completed,
            PromptVersion = "1.0.0",
            OpenAiModel = "gpt-4o",
            PromptTokens = 100,
            CompletionTokens = 200,
            TotalTokens = 300,
            EstimatedCostUsd = 0.001m,
            ProcessingTimeMs = 5000,
            CreatedAt = DateTimeOffset.UtcNow
        };

        Assert.Equal("test_sds.pdf", analysis.FileName);
        Assert.Equal(64, analysis.FileHash.Length);
        Assert.Equal(AnalysisStatus.Completed, analysis.Status);
        Assert.Equal(300, analysis.TotalTokens);
    }

    [Fact]
    public void AnalysisStatus_Enum_HasExpectedValues()
    {
        Assert.Equal(0, (int)AnalysisStatus.Completed);
        Assert.Equal(1, (int)AnalysisStatus.PartiallyCompleted);
        Assert.Equal(2, (int)AnalysisStatus.Failed);
    }

    [Fact]
    public void SdsExtractionResult_CanBeCreated_WithNullableFields()
    {
        var result = new SdsExtractionResult
        {
            ChemicalName = "Acetone",
            CasNumber = "67-64-1",
            SignalWord = "Danger",
            Manufacturer = new ContactInfo("ACME Corp", "123 Main St", "555-0100", "info@acme.com"),
            HazardStatements =
            [
                new CodedStatement("H225", "Highly flammable liquid and vapour"),
                new CodedStatement("H319", "Causes serious eye irritation")
            ],
            Composition =
            [
                new CompositionComponent("Acetone", "67-64-1", "100%")
            ],
            PhysicalProperties = new PhysicalPropertiesInfo(
                Appearance: "Clear, colorless liquid",
                Odor: "Characteristic pungent odor",
                Ph: null,
                MeltingPoint: "-95 °C",
                BoilingPoint: "56 °C",
                FlashPoint: "-20 °C",
                EvaporationRate: "7.7",
                Flammability: "Highly flammable",
                VaporPressure: "24 kPa at 20 °C",
                VaporDensity: "2.0",
                SpecificGravity: "0.79",
                Solubility: "Miscible with water",
                AutoIgnitionTemperature: "465 °C",
                DecompositionTemperature: null,
                Viscosity: "0.32 mPa·s",
                MolecularWeight: "58.08 g/mol",
                MolecularFormula: "C3H6O"
            ),
            Confidence = new ConfidenceInfo(
                OverallConfidence: "high",
                OverallNotes: "All major sections present and clearly structured.",
                FieldConfidences:
                [
                    new FieldConfidence("casNumber", "high", "Section 1 - Identification", 1),
                    new FieldConfidence("flashPoint", "high", "Section 9 - Physical Properties", 7)
                ]
            )
        };

        Assert.Equal("Acetone", result.ChemicalName);
        Assert.Equal("67-64-1", result.CasNumber);
        Assert.Equal(2, result.HazardStatements!.Count);
        Assert.Equal("H225", result.HazardStatements[0].Code);
        Assert.Equal("56 °C", result.PhysicalProperties!.BoilingPoint);
        Assert.Equal("high", result.Confidence!.OverallConfidence);
        Assert.Equal(2, result.Confidence.FieldConfidences!.Count);

        // Verify nullable fields are null when not set
        Assert.Null(result.ProductIdentifier);
        Assert.Null(result.UnNumber);
        Assert.Null(result.FirstAidMeasures);
        Assert.Null(result.ToxicologicalInformation);
    }

    [Fact]
    public void SdsExtractionResult_AllFieldsNull_IsValid()
    {
        var result = new SdsExtractionResult();

        Assert.Null(result.ChemicalName);
        Assert.Null(result.Manufacturer);
        Assert.Null(result.HazardStatements);
        Assert.Null(result.Composition);
        Assert.Null(result.PhysicalProperties);
        Assert.Null(result.Confidence);
    }

    [Fact]
    public void SdsExtractionResult_RecordEquality_Works()
    {
        var contact1 = new ContactInfo("ACME", "123 St", "555", "a@b.com");
        var contact2 = new ContactInfo("ACME", "123 St", "555", "a@b.com");

        Assert.Equal(contact1, contact2);
    }

    [Fact]
    public void SdsExtractionResult_JsonSerialization_RoundTrips()
    {
        var options = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        var original = new SdsExtractionResult
        {
            ChemicalName = "Acetone",
            CasNumber = "67-64-1",
            PhysicalProperties = new PhysicalPropertiesInfo(
                Appearance: "Clear liquid", Odor: null, Ph: "7.0",
                MeltingPoint: null, BoilingPoint: "56 °C", FlashPoint: "-20 °C",
                EvaporationRate: null, Flammability: null, VaporPressure: null,
                VaporDensity: null, SpecificGravity: null, Solubility: null,
                AutoIgnitionTemperature: null, DecompositionTemperature: null,
                Viscosity: null, MolecularWeight: null, MolecularFormula: null
            )
        };

        var json = System.Text.Json.JsonSerializer.Serialize(original, options);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<SdsExtractionResult>(json, options);

        Assert.NotNull(deserialized);
        Assert.Equal("Acetone", deserialized!.ChemicalName);
        Assert.Equal("67-64-1", deserialized.CasNumber);
        Assert.Equal("56 °C", deserialized.PhysicalProperties!.BoilingPoint);

        // Verify pH custom JsonPropertyName
        Assert.Contains("\"pH\"", json);
    }
}
