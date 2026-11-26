namespace RRMS.Domain.Constants.Authentication;

public static class AuthenticationStates
{
    public const string RegistrationPending = "REGISTRATION_PENDING";
    public const string RegistrationCompleted = "REGISTRATION_COMPLETED";
    public const string EmailVerified = "EMAIL_VERIFIED";
    public const string RoleSelectionRequired = "ROLE_SELECTION_REQUIRED";
    public const string Authenticated = "AUTHENTICATED";
}
