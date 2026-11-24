namespace RRMS.Application.Interfaces.Services.Authentication;

public interface IPasswordService
{
    string Hash(string password);
    bool Verify(string password, string storedHash);
}
