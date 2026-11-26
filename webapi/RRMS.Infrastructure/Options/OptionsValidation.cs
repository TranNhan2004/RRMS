using MimeKit;

namespace RRMS.Infrastructure.Options;

internal static class OptionsValidation
{
    public static bool ValidEmailAddress(string? value) =>
        !string.IsNullOrWhiteSpace(value) && !value.Contains('\r') && !value.Contains('\n') &&
        MailboxAddress.TryParse(value, out var address) && address.Address.Contains('@');

    public static bool MatchingCredentials(string? username, string? password) =>
        string.IsNullOrWhiteSpace(username) == string.IsNullOrWhiteSpace(password);

    public static bool HttpUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps) &&
        !string.IsNullOrWhiteSpace(uri.Host) && string.IsNullOrEmpty(uri.UserInfo);

    public static bool VerificationUrl(string? value, bool isDevelopment) =>
        HttpUrl(value) && Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttps || isDevelopment);

    public static bool FrontendUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttps ||
            (uri.Scheme == Uri.UriSchemeHttp && uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))) &&
        !string.IsNullOrWhiteSpace(uri.Host) && string.IsNullOrEmpty(uri.UserInfo);

    public static bool CorsOrigin(string? value) =>
        HttpUrl(value) && !value!.Contains('*') && Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        uri.AbsolutePath == "/" && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment);

    public static bool NonEmptyValues(string[]? values) =>
        values != null && values.All(value => !string.IsNullOrWhiteSpace(value));
}
