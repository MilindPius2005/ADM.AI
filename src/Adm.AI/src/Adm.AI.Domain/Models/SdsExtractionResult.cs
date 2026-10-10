namespace Adm.AI.Domain.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Root extraction result following GHS 16-section SDS standard.
/// All fields nullable — null means "not found in document."
/// This record and its nested types define THE canonical SDS extraction schema.
/// The JSON Schema for OpenAI Structured Outputs is derived from this model.
/// </summary>
public record SdsExtractionResult
{
    // ── GHS Pictograms ──
    [JsonPropertyName("pictograms")]
    public List<GhsPictogram>? Pictograms { get; init; }

    // ── Section 1: Identification ──
    public string? ChemicalName { get; init; }
    public string? ProductIdentifier { get; init; }
    public string? CasNumber { get; init; }
    public string? UnNumber { get; init; }
    public string? RecommendedUse { get; init; }
    public ContactInfo? Manufacturer { get; init; }
    public ContactInfo? Supplier { get; init; }
    public string? EmergencyPhone { get; init; }

    // ── Section 2: Hazard Identification ──
    public string? SignalWord { get; init; }
    public List<string>? HazardClassification { get; init; }
    public List<CodedStatement>? HazardStatements { get; init; }
    public List<CodedStatement>? PrecautionaryStatements { get; init; }

    // ── Section 3: Composition / Information on Ingredients ──
    public List<CompositionComponent>? Composition { get; init; }

    // ── Section 4: First Aid Measures ──
    public FirstAidInfo? FirstAidMeasures { get; init; }

    // ── Section 5: Fire-Fighting Measures ──
    public FireFightingInfo? FireFightingMeasures { get; init; }

    // ── Section 6: Accidental Release Measures ──
    public AccidentalReleaseInfo? AccidentalReleaseMeasures { get; init; }

    // ── Section 7: Handling and Storage ──
    public HandlingStorageInfo? HandlingStorage { get; init; }

    // ── Section 8: Exposure Controls / Personal Protection ──
    public ExposureControlsInfo? ExposureControls { get; init; }
    public PpeInfo? Ppe { get; init; }

    // ── Section 9: Physical and Chemical Properties ──
    public PhysicalPropertiesInfo? PhysicalProperties { get; init; }

    // ── Section 10: Stability and Reactivity ──
    public StabilityReactivityInfo? StabilityReactivity { get; init; }

    // ── Section 11: Toxicological Information ──
    public ToxicologicalInfoData? ToxicologicalInformation { get; init; }

    // ── Section 12: Ecological Information ──
    public EcologicalInfoData? EcologicalInformation { get; init; }

    // ── Section 13: Disposal Considerations ──
    public DisposalInfo? DisposalConsiderations { get; init; }

    // ── Section 14: Transport Information ──
    public TransportInfoData? TransportInformation { get; init; }

    // ── Section 15: Regulatory Information ──
    public RegulatoryInfoData? RegulatoryInformation { get; init; }

    // ── Confidence Metadata ──
    public ConfidenceInfo? Confidence { get; init; }
}

// ── Reusable nested records ──

public record ContactInfo(
    string? Name,
    string? Address,
    string? Phone,
    string? Email
);

public record CodedStatement(
    string? Code,
    string Statement
);

public record CompositionComponent(
    string Name,
    string? CasNumber,
    string? Concentration
);

// ── Section-specific records ──

public record FirstAidInfo(
    string? Inhalation,
    string? SkinContact,
    string? EyeContact,
    string? Ingestion,
    string? Notes
);

public record FireFightingInfo(
    string? SuitableExtinguishingMedia,
    string? UnsuitableExtinguishingMedia,
    string? SpecialHazards,
    string? ProtectiveEquipment
);

public record AccidentalReleaseInfo(
    string? PersonalPrecautions,
    string? EnvironmentalPrecautions,
    string? ContainmentMethods,
    string? CleanupMethods
);

public record HandlingStorageInfo(
    string? HandlingPrecautions,
    string? StorageConditions,
    string? IncompatibleMaterials
);

public record ExposureControlsInfo(
    List<ExposureLimit>? OccupationalExposureLimits,
    string? EngineeringControls
);

public record ExposureLimit(
    string Substance,
    string? LimitType,
    string? Value
);

public record PpeInfo(
    string? Respiratory,
    string? Hand,
    string? Eye,
    string? Skin,
    string? Other
);

public record PhysicalPropertiesInfo(
    string? Appearance,
    string? Odor,
    [property: JsonPropertyName("pH")] string? Ph,
    string? MeltingPoint,
    string? BoilingPoint,
    string? FlashPoint,
    string? EvaporationRate,
    string? Flammability,
    string? VaporPressure,
    string? VaporDensity,
    string? SpecificGravity,
    string? Solubility,
    string? AutoIgnitionTemperature,
    string? DecompositionTemperature,
    string? Viscosity,
    string? MolecularWeight,
    string? MolecularFormula
);

public record StabilityReactivityInfo(
    string? Stability,
    string? ConditionsToAvoid,
    string? IncompatibleMaterials,
    string? HazardousDecompositionProducts,
    string? HazardousPolymerization
);

public record ToxicologicalInfoData(
    string? AcuteToxicity,
    string? SkinCorrosion,
    string? EyeDamage,
    string? Sensitization,
    string? Mutagenicity,
    string? Carcinogenicity,
    string? ReproductiveToxicity,
    string? OrganToxicity,
    string? AspirationHazard,
    string? AdditionalNotes
);

public record EcologicalInfoData(
    string? AquaticToxicity,
    string? Persistence,
    string? Bioaccumulation,
    string? Mobility,
    string? OtherEffects
);

public record DisposalInfo(
    string? DisposalMethods,
    string? ContaminatedPackaging,
    string? SpecialPrecautions
);

public record TransportInfoData(
    string? UnNumber,
    string? ProperShippingName,
    string? HazardClass,
    string? PackingGroup,
    string? EnvironmentalHazard,
    string? SpecialPrecautions
);

public record RegulatoryInfoData(
    string? UsRegulations,
    string? EuRegulations,
    string? OtherRegulations,
    string? InventoryStatus
);

// ── Confidence metadata ──

public record ConfidenceInfo(
    string OverallConfidence,
    string? OverallNotes,
    List<FieldConfidence>? FieldConfidences
);

public record FieldConfidence(
    string FieldName,
    string Level,
    string? SourceSection,
    int? SourcePage
);
