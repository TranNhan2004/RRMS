namespace RRMS.Application.Interfaces.Services.Authentication;

/// <summary>
/// Creates password hashes and verifies passwords against stored hashes.
/// </summary>
public interface IPasswordService
{
    string Hash(string password);
    bool Verify(string password, string storedHash);
}
