namespace Adm.AI.Infrastructure.Persistence.Configurations;

using Adm.AI.Domain.Entities;
using Adm.AI.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class SdsAnalysisConfiguration : IEntityTypeConfiguration<SdsAnalysis>
{
    public void Configure(EntityTypeBuilder<SdsAnalysis> builder)
    {
        builder.ToTable("sds_analyses");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        // File hash — unique constraint for deduplication
        builder.Property(e => e.FileHash)
            .HasColumnName("file_hash")
            .HasMaxLength(64)
            .IsRequired();

        builder.HasIndex(e => e.FileHash)
            .IsUnique()
            .HasDatabaseName("IX_sds_analyses_file_hash");

        // File metadata
        builder.Property(e => e.FileName)
            .HasColumnName("file_name")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.FileSizeBytes)
            .HasColumnName("file_size_bytes")
            .IsRequired();

        // Extracted text — stored for re-analysis capability
        builder.Property(e => e.ExtractedText)
            .HasColumnName("extracted_text")
            .HasColumnType("text")
            .IsRequired();

        // Analysis result — stored as jsonb in PostgreSQL
        builder.Property(e => e.AnalysisResultJson)
            .HasColumnName("analysis_result_json")
            .HasColumnType("jsonb")
            .IsRequired();

        // Status — stored as string
        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasMaxLength(20)
            .HasConversion(
                v => v.ToString(),
                v => Enum.Parse<AnalysisStatus>(v))
            .IsRequired();

        // Prompt versioning — for auditing and reprocessing
        builder.Property(e => e.PromptVersion)
            .HasColumnName("prompt_version")
            .HasMaxLength(20)
            .IsRequired();

        // OpenAI metadata
        builder.Property(e => e.OpenAiModel)
            .HasColumnName("openai_model")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.PromptTokens)
            .HasColumnName("prompt_tokens")
            .IsRequired();

        builder.Property(e => e.CompletionTokens)
            .HasColumnName("completion_tokens")
            .IsRequired();

        builder.Property(e => e.TotalTokens)
            .HasColumnName("total_tokens")
            .IsRequired();

        // Cost tracking
        builder.Property(e => e.EstimatedCostUsd)
            .HasColumnName("estimated_cost_usd")
            .HasColumnType("numeric(10,6)")
            .IsRequired();

        // Performance tracking
        builder.Property(e => e.ProcessingTimeMs)
            .HasColumnName("processing_time_ms")
            .IsRequired();

        // Timestamp
        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_sds_analyses_created_at");
    }
}
