# Authentication configuration

The authentication settings are under `Auth:Credentials`, `Auth:Google`, `Csrf`, and `Cors` in `RRMS.Api/appsettings.json`. Supply secrets through environment variables or another secure configuration provider; the checked-in secret values are intentionally empty.

Set three distinct Base64-encoded keys of at least 32 bytes in `Auth__Credentials__AccessToken__SecretKey`, `Auth__Credentials__RefreshToken__SecretKey`, and `Csrf__SecretKey`. Keep the same values across instances. Configure the database and Redis connection strings, and seed the `TENANT` role before accepting registrations.

For registration, configure `Auth__Credentials__VerificationUrl` and `Auth__Credentials__Smtp__Host`, `Port`, and `From`; also configure the SMTP username and password when authentication is enabled. The verification URL must use HTTPS outside the `Local` environment. SMTP STARTTLS is required outside `Local`.

To enable Google login, configure the Google client ID and secret, frontend `CallbackUrl`, `RoleSelectionUrl`, and `AllowedOrigins`. Register the backend callback URL ending in `/login/oauth2/code/google` with Google. Start the flow at `/oauth2/authorization/google`. The frontend redirect origins must be explicitly allowlisted. Google challenge state is protected with ASP.NET Core data-protection keys stored under `rrms:auth:data-protection-keys` in Redis; restrict access to that Redis instance and persist its data across restarts.

Configure allowed browser origins under `Cors:AllowedOrigins`. Refresh tokens are issued in an HTTP-only `RRMS_REFRESH` cookie; clients send the `XSRF-TOKEN` cookie value as the `X-XSRF-TOKEN` header for refresh and other protected unsafe requests. In the `Local` environment, CSRF validation is disabled to match the Java local profile. Secure cookies are required outside `Local`.
