using System.Security.Cryptography;
using Adm.AI.Application.Interfaces;

namespace Adm.AI.Application.Services;

public class HashService : IHashService
{
    public string ComputeHash(byte[] data)
    {
        if (data == null || data.Length == 0)
        {
            return string.Empty;
        }

        var hashBytes = SHA256.HashData(data);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
