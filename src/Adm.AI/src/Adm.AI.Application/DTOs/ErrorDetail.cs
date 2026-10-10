namespace Adm.AI.Application.DTOs;

/// <summary>
/// Error detail — always non-null when Success = false.
/// </summary>
public record ErrorDetail(
    string Code,
    string Message
);
