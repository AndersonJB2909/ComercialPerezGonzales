using System.Security.Cryptography;
using System.Text;

namespace ComercialPerezGonzales;

public static class SecurityHelper
{
    /// <summary>
    /// Retorna el hash SHA-256 (64 hex chars) del valor dado.
    /// Los valores almacenados en la BD siempre son hashes — nunca texto plano.
    /// </summary>
    public static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public static bool IsHashed(string value) => value.Length == 64;
}
