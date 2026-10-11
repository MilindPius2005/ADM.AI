using Adm.AI.Application.Configuration;
using Adm.AI.Application.Interfaces;
using Adm.AI.Application.Services;
using Adm.AI.Domain.Repositories;
using Adm.AI.Infrastructure.Persistence;
using Adm.AI.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ──
builder.Host.UseSerilog((context, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// ── Strongly typed configuration ──
builder.Services.Configure<OpenAiSettings>(
    builder.Configuration.GetSection(OpenAiSettings.SectionName));
builder.Services.Configure<FileUploadSettings>(
    builder.Configuration.GetSection(FileUploadSettings.SectionName));
builder.Services.Configure<PromptSettings>(
    builder.Configuration.GetSection(PromptSettings.SectionName));
builder.Services.Configure<AiProcessingSettings>(
    builder.Configuration.GetSection(AiProcessingSettings.SectionName));
builder.Services.Configure<SdsChunkingSettings>(
    builder.Configuration.GetSection(SdsChunkingSettings.SectionName));

// ── Database (Switched to InMemory for demo) ──
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("AdmAiDemoDb"));

// ── Repositories ──
builder.Services.AddScoped<IAnalysisRepository, AnalysisRepository>();

// ── Application Services (registered in later phases) ──
// Phase 2
builder.Services.AddScoped<IFileValidationService, FileValidationService>();
builder.Services.AddScoped<IHashService, HashService>();
builder.Services.AddScoped<IPdfExtractionService, PdfExtractionService>();
builder.Services.AddScoped<ITextCleaningService, TextCleaningService>();
builder.Services.AddScoped<ISdsSectionParser, SdsSectionParser>();
// Phase 3
builder.Services.AddScoped<IPromptService, PromptService>();
builder.Services.AddSingleton<IOpenAiProvider, Adm.AI.Infrastructure.Providers.OpenAiProvider>();
builder.Services.AddScoped<IPdfRenderingService, Adm.AI.Infrastructure.Services.PdfRenderingService>();
builder.Services.AddScoped<ISdsAiProcessingService, SdsAiProcessingService>();
// Phase 4
builder.Services.AddSingleton<IAiConcurrencyLimiter, AiConcurrencyLimiter>();
builder.Services.AddScoped<ISdsAnalysisService, SdsAnalysisService>();

// ── Controllers ──
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// ── Swagger ──
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── Health Checks ──
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

// ── CORS (demo only – allows the frontend to call the API from any origin) ──
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// ── Middleware Pipeline ──
app.UseCors();
app.UseDefaultFiles();   // serves index.html at /
app.UseStaticFiles();    // serves wwwroot/

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Make the implicit Program class public for integration tests
public partial class Program { }
