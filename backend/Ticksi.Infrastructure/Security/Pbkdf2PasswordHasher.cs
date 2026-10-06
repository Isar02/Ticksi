using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Ticksi.Application.Interfaces;
using Ticksi.Domain.Entities;

namespace Ticksi.Infrastructure.Security;

public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int LegacyHashLength = 44;
    private const int LegacyHashBytes = 32;

    private readonly PasswordHasher<AppUser> _hasher = new();

    public string Hash(AppUser user, string password) =>
        _hasher.HashPassword(user, password);

    public PasswordCheck Verify(AppUser user, string password)
    {
        if (TryReadLegacyHash(user.PasswordHash, out var legacyHash))
            return CryptographicOperations.FixedTimeEquals(legacyHash, SHA256.HashData(Encoding.UTF8.GetBytes(password)))
                ? PasswordCheck.ValidNeedsRehash
                : PasswordCheck.Failed;

        return _hasher.VerifyHashedPassword(user, user.PasswordHash, password) switch
        {
            PasswordVerificationResult.Success => PasswordCheck.Valid,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordCheck.ValidNeedsRehash,
            _ => PasswordCheck.Failed
        };
    }

    // Accounts created before PBKDF2 store an unsalted Base64 SHA-256 digest.
    private static bool TryReadLegacyHash(string passwordHash, out byte[] hash)
    {
        hash = new byte[LegacyHashBytes];
        return passwordHash.Length == LegacyHashLength
            && Convert.TryFromBase64String(passwordHash, hash, out var written)
            && written == LegacyHashBytes;
    }
}
