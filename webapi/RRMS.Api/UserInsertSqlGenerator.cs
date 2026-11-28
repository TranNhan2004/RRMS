using System.Net.Mail;
using System.Text;
using RRMS.Application.Interfaces.Services.Authentication;

namespace RRMS.Api;

public sealed class UserInsertSqlGenerator
{
    private readonly IPasswordService _passwordService;

    public UserInsertSqlGenerator(IPasswordService passwordService)
    {
        _passwordService = passwordService;
    }

    public string Generate(string userName, string email, string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        userName = userName.Trim();
        email = email.Trim().ToLowerInvariant();
        if (userName.Length > 64 || email.Length > 255 || !MailAddress.TryCreate(email, out var address) || address.Address != email)
        {
            throw new ArgumentException("Provide a username of at most 64 characters and a valid email of at most 255 characters.");
        }

        var userId = Guid.CreateVersion7();
        var credentialId = Guid.CreateVersion7();
        var passwordHash = _passwordService.Hash(password);

        return $"""
            START TRANSACTION;
            INSERT INTO `users` (`id`, `user_name`, `email`, `status`, `is_email_verified`, `is_deleted`, `created_at`)
            VALUES ('{userId}', {ToSqlText(userName)}, {ToSqlText(email)}, 'Active', TRUE, FALSE, CURRENT_TIMESTAMP(6));
            INSERT INTO `user_credentials` (`id`, `user_id`, `password_hash`, `is_deleted`, `created_at`)
            VALUES ('{credentialId}', '{userId}', {ToSqlText(passwordHash)}, FALSE, CURRENT_TIMESTAMP(6));
            COMMIT;
            """;
    }

    private static string ToSqlText(string value) =>
        $"CONVERT(0x{Convert.ToHexString(Encoding.UTF8.GetBytes(value))} USING utf8mb4)";
}
