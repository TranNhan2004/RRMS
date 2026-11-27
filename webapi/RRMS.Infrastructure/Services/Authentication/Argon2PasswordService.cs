using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using RRMS.Application.Interfaces.Services.Authentication;

namespace RRMS.Infrastructure.Services.Authentication;

/// <summary>
/// Hashes passwords with Argon2id and verifies stored hashes using a constant-time comparison.
/// </summary>
public sealed class Argon2PasswordService : IPasswordService
{
    private const string Prefix = "{argon2}$argon2id$v=19$m=19456,t=2,p=1$";

    public string Hash(string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Derive(password, salt);
        return Prefix + Encode(salt) + "$" + Encode(hash);
    }

    public bool Verify(string password, string storedHash)
    {
        if (password == null || storedHash == null || !storedHash.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return false;
        }

        var parts = storedHash[Prefix.Length..].Split('$');
        if (parts.Length != 2)
        {
            return false;
        }

        try
        {
            var salt = Decode(parts[0]);
            var expected = Decode(parts[1]);
            if (salt.Length != 16 || expected.Length != 32)
            {
                return false;
            }

            var actual = Derive(password, salt);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static byte[] Derive(string password, byte[] salt)
    {
        var parameters = new Argon2Parameters.Builder(Argon2Parameters.Argon2id)
            .WithVersion(Argon2Parameters.Version13)
            .WithMemoryAsKB(19456)
            .WithIterations(2)
            .WithParallelism(1)
            .WithSalt(salt)
            .Build();
        var generator = new Argon2BytesGenerator();
        generator.Init(parameters);
        var passwordBytes = Encoding.UTF8.GetBytes(password);
        try
        {
            var hash = new byte[32];
            generator.GenerateBytes(passwordBytes, hash);
            return hash;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(passwordBytes);
        }
    }

    private static string Encode(byte[] value) => Convert.ToBase64String(value).TrimEnd('=');
    private static byte[] Decode(string value) => Convert.FromBase64String(value.PadRight((value.Length + 3) / 4 * 4, '='));
}
