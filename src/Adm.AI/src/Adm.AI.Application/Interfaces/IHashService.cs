namespace Adm.AI.Application.Interfaces;

public interface IHashService
{
    /// <summary>
    /// Computes SHA-256 hash of raw bytes. Returns lowercase hex string (64 chars).
    /// Pure function. Synchronous. Deterministic.
    /// </summary>
    string ComputeHash(byte[] data);
}
