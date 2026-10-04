using System.Security.Cryptography;
using System.Text;
using Ticksi.Application.Interfaces;

namespace Ticksi.Infrastructure.Security;

public sealed class Sha256PasswordHasher : IPasswordHasher
{
    public string Hash(string password) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(password)));

    public bool Verify(string password, string passwordHash) =>
        Hash(password) == passwordHash;
}
