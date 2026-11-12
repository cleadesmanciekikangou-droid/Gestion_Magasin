using System.Security.Cryptography;
using System.Text.Json;

namespace GestionMagasin.Services;

/// <summary>Hachage de mots de passe PBKDF2-SHA256. Le sel est unique pour chaque compte.</summary>
public sealed class PasswordHasher
{
    public const int Iterations = 600_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public PasswordHashResult Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        var parameters = JsonSerializer.Serialize(new
        {
            algorithm = "PBKDF2-SHA256",
            iterations = Iterations,
            saltBytes = SaltSize,
            hashBytes = HashSize
        });
        return new PasswordHashResult(Convert.ToBase64String(hash), Convert.ToBase64String(salt), parameters);
    }
}

public sealed record PasswordHashResult(string Hash, string Salt, string Parameters);
