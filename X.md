# RRMS – Authentication and Security Refactoring Plan

## 1. Objectives

Refactor the existing Authentication module into an Infrastructure-owned security subsystem.

Authentication is treated as an infrastructure capability that integrates ASP.NET Core Authentication, Redis, Google OAuth, Data Protection, and Antiforgery.

The refactoring must:

- Move all Authentication service implementations into the Infrastructure layer.
- Keep Application responsible for service contracts and application models.
- Keep API responsible for HTTP endpoints, DTO mapping, middleware registration, and endpoint metadata.
- Keep Domain responsible for entities, domain constants, and domain rules.
- Preserve ASP.NET Core Cookie Authentication.
- Preserve Redis-backed authentication sessions.
- Preserve ASP.NET Core Data Protection.
- Preserve ASP.NET Core Antiforgery.
- Separate CSRF protection from Authentication.
- Reduce controller responsibilities and dependencies.
- Replace operation-based service methods with explicit use-case methods.
- Centralize authentication session management and cookie operations.
- Enforce secure-by-default security policies.
- Preserve existing business rules unless explicitly stated otherwise.

Do not introduce additional Application service implementations that simply delegate authentication operations to Infrastructure.

Avoid unnecessary interfaces, mediator patterns, generic handlers, and duplicated orchestration layers.

## 2. Architectural Principles

### 2.1 Layer Responsibilities

| Layer          | Responsibility                                                                                                             |
| -------------- | -------------------------------------------------------------------------------------------------------------------------- |
| Domain         | Entities, domain constants, domain rules                                                                                   |
| Application    | Service interfaces, request/response models, shared context contracts                                                      |
| Infrastructure | Authentication implementations, session management, Redis integration, OAuth integration, CSRF services, cookie management |
| API            | Controllers, DTOs, middleware, endpoint attributes, dependency registration composition                                    |

### 2.2 Dependency Direction

```text
RRMS.Api
    ├── RRMS.Application
    └── RRMS.Infrastructure
              ├── RRMS.Application
              └── RRMS.Domain

RRMS.Application
    └── RRMS.Domain

RRMS.Domain
    └── No application-specific dependencies
```

Infrastructure may depend on ASP.NET Core framework abstractions when implementing web-related security functionality.

Infrastructure must not reference the API project.

Application and Domain must not depend on:

- ASP.NET Core HTTP abstractions.
- Redis-specific types.
- AuthenticationTicket.
- AuthenticationProperties.
- CookieOptions.
- Google OAuth framework types.

### 2.3 Infrastructure Framework Reference

If Infrastructure requires ASP.NET Core types, add the framework reference to its project file:

```xml
<ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
</ItemGroup>
```

Use framework-provided security functionality instead of implementing equivalent primitives manually.

## 3. Target Project Structure

```text
RRMS.Domain/
├── Entities/
└── Constants/
    └── Authentication/
        ├── AuthenticationStates.cs
        └── RegistrationStages.cs

RRMS.Application/
├── Interfaces/
│   ├── Contexts/
│   │   ├── ICurrentUserContext.cs
│   │   └── ICurrentRequestContext.cs
│   │
│   └── Services/
│       └── Authentication/
│           ├── IAuthenticationService.cs
│           ├── IRegistrationService.cs
│           ├── ISessionManagementService.cs
│           └── IAuthenticationEventLogger.cs
│
└── Models/
    └── Authentication/

RRMS.Infrastructure/
├── Services/
│   ├── Authentication/
│   │   ├── AuthenticationService.cs
│   │   ├── RegistrationService.cs
│   │   ├── SessionManagementService.cs
│   │   ├── AuthenticationSessionService.cs
│   │   ├── SessionCookieService.cs
│   │   ├── GoogleAuthenticationService.cs
│   │   ├── AuthenticationDataService.cs
│   │   ├── AuthenticationChallengeStore.cs
│   │   ├── RedisTicketStore.cs
│   │   └── AuthenticationEventLogger.cs
│   │
│   └── Csrf/
│       └── CsrfService.cs
│
├── Contexts/
│   ├── CurrentUserContext.cs
│   └── CurrentRequestContext.cs
│
├── Constants/
│   └── Authentication/
│       ├── AuthenticationSchemes.cs
│       └── AuthenticationClaimTypes.cs
│
├── Options/
│   ├── AuthOptions.cs
│   └── CsrfOptions.cs
│
└── Extensions/
    ├── AuthenticationExtensions.cs
    ├── AuthorizationExtensions.cs
    └── CsrfExtensions.cs

RRMS.Api/
├── Controllers/
│   └── V1/
│       ├── AuthenticationController.cs
│       ├── GoogleAuthenticationController.cs
│       ├── SessionsController.cs
│       └── CsrfController.cs
│
├── Dtos/
│   └── V1/
│       └── Authentication/
│
├── Attributes/
│   └── SkipCsrfAttribute.cs
│
├── Middlewares/
│   └── CsrfMiddleware.cs
│
└── Program.cs
```

The structure represents target responsibilities rather than a mandatory list of new files.

Reuse and rename existing implementations where possible.

Do not create duplicate services when an existing service already fulfills the required responsibility.

## 4. Service Responsibility Overview

| Service                      | Layer          | Responsibility                                                                                    |
| ---------------------------- | -------------- | ------------------------------------------------------------------------------------------------- |
| AuthenticationService        | Infrastructure | Credential authentication, Google identity authentication, pending authentication, role selection |
| RegistrationService          | Infrastructure | Registration and email verification                                                               |
| AuthenticationSessionService | Infrastructure | Complete authentication and establish HTTP sessions                                               |
| SessionCookieService         | Infrastructure | Authentication and pending session cookie operations                                              |
| SessionManagementService     | Infrastructure | Session retrieval, revocation, logout, and active role switching                                  |
| GoogleAuthenticationService  | Infrastructure | Google OAuth HTTP orchestration                                                                   |
| RedisTicketStore             | Infrastructure | Redis-backed authentication ticket persistence                                                    |
| AuthenticationDataService    | Infrastructure | Authentication-related database access                                                            |
| AuthenticationChallengeStore | Infrastructure | Temporary authentication challenges                                                               |
| AuthenticationEventLogger    | Infrastructure | Structured authentication event logging                                                           |
| CsrfService                  | Infrastructure | Antiforgery token issuance and validation                                                         |
| CurrentUserContext           | Infrastructure | Current authenticated identity                                                                    |
| CurrentRequestContext        | Infrastructure | Current HTTP request metadata                                                                     |
| CsrfMiddleware               | API            | Endpoint-level CSRF enforcement                                                                   |

Do not create a separate Application implementation for any service listed above.

## 5. Refactor Authentication Service Contracts

### 5.1 Replace operation-based authentication

The existing `LoginAsync()` method handles several unrelated operations through an operation discriminator.

Replace this design with explicit methods.

```csharp
public interface IAuthenticationService
{
    Task<AuthenticationResultResponseModel> LoginAsync(
        LoginRequestModel request,
        CancellationToken cancellationToken);

    Task<AuthenticationResultResponseModel> GoogleLoginAsync(
        GoogleLoginRequestModel request,
        CancellationToken cancellationToken);

    Task<AuthenticationResultResponseModel> GetPendingSessionAsync(
        CancellationToken cancellationToken);

    Task<AuthenticationResultResponseModel> SelectRoleAsync(
        SelectRoleRequestModel request,
        CancellationToken cancellationToken);

    Task<CurrentUserResponseModel> GetCurrentUserAsync(
        CancellationToken cancellationToken);
}
```

Implement the interface in:

```text
RRMS.Infrastructure.Services.Authentication.AuthenticationService
```

Requirements:

- Remove the `Operation` property from `LoginRequestModel`.
- Remove `AuthenticationOperations` if no longer required.
- Introduce dedicated request models.
- Reuse common credential, account status, and role eligibility validation.
- Do not expose temporary authentication tokens through public application responses.
- Resolve pending session information internally through Infrastructure services.
- Resolve the current user through `ICurrentUserContext`.
- Keep HTTP response construction outside the service.

Because the implementation resides in Infrastructure, it may directly use authentication-specific data services, Redis challenge storage, and cookie readers without introducing additional Application-level orchestration.

### 5.2 Separate Registration Service

```csharp
public interface IRegistrationService
{
    Task<RegistrationResponseModel> RegisterAsync(
        RegistrationRequestModel request,
        CancellationToken cancellationToken);

    Task<VerifyEmailResponseModel> VerifyEmailAsync(
        VerifyEmailRequestModel request,
        CancellationToken cancellationToken);
}
```

Implement the interface entirely in Infrastructure.

Responsibilities:

- Validate registration requests.
- Manage registration stages.
- Create email verification challenges.
- Validate verification codes or tokens.
- Create or activate user accounts.
- Preserve existing registration business rules.
- Coordinate database transactions.
- Prevent duplicate accounts and concurrent registration conflicts.

Do not create a corresponding RegistrationService implementation in Application.

## 6. Centralize Current User and Request Contexts

### 6.1 Current User Context

Keep the contract in Application.

```csharp
public interface ICurrentUserContext
{
    bool IsAuthenticated { get; }

    Guid? UserId { get; }

    string? UserName { get; }

    string? ActiveRole { get; }

    string? SessionReference { get; }

    Guid GetRequiredUserId();
}
```

Implement `CurrentUserContext` in Infrastructure using `IHttpContextAccessor`.

Requirements:

- Centralize claim extraction.
- Do not repeatedly parse claims in controllers.
- Do not return `Guid.Empty` for missing or invalid user identifiers.
- Throw an appropriate exception when required authentication information is unavailable.
- Resolve the current principal from the active HTTP context.
- Do not cache stale identity information across authentication transitions.

### 6.2 Current Request Context

```csharp
public interface ICurrentRequestContext
{
    string? RemoteIpAddress { get; }

    string? UserAgent { get; }

    string? TraceId { get; }

    string? ClientActionId { get; }

    string? ClientRequestId { get; }
}
```

Implement the interface in Infrastructure.

Use it for:

- Authentication event logging.
- Authentication challenge rate limiting.
- Audit logging.
- Request correlation.
- Other services that require HTTP request metadata.

Configure trusted forwarded headers before resolving the remote IP address.

## 7. Centralize Authentication Constants

Move framework-specific authentication constants into Infrastructure.

```csharp
public static class AuthenticationSchemes
{
    public const string Session = "Session";

    public const string Google = "Google";

    public const string External = "External";
}
```

Preserve the existing registered scheme names during migration.

```csharp
public static class AuthenticationClaimTypes
{
    public const string Subject = "sub";

    public const string UserName = "userName";

    public const string ActiveRole = "activeRole";

    public const string SessionReference = "sessionRef";
}
```

Requirements:

- Remove duplicated raw claim strings.
- Remove authentication scheme constants from unrelated extension classes.
- Preserve compatibility with existing authentication tickets.
- Keep domain-level authentication states and registration stages in Domain.
- Keep HTTP and framework-specific constants in Infrastructure.

## 8. Refactor Authentication Session Orchestration

### 8.1 AuthenticationSessionService

Create or refactor an Infrastructure service responsible for completing authentication.

```csharp
public sealed class AuthenticationSessionService
{
    public Task CompleteAsync(
        AuthenticationResultResponseModel result,
        CancellationToken cancellationToken);
}
```

Move the existing controller `CompleteAsync()` implementation into this service.

Handle authentication states explicitly.

```csharp
switch (result.State)
{
    case AuthenticationStates.Authenticated:
        // Establish authenticated session.
        break;

    case AuthenticationStates.RoleSelectionRequired:
        // Establish pending authentication.
        break;

    default:
        throw new InvalidOperationException(
            "Unsupported authentication state.");
}
```

Responsibilities:

- Establish authenticated sessions.
- Establish pending authentication sessions.
- Delegate cookie operations to `SessionCookieService`.
- Clear obsolete pending session cookies.
- Apply appropriate cache-control headers.
- Record authentication events after successful completion.

Do not:

- Validate credentials.
- Reimplement role eligibility rules.
- Perform Google OAuth challenges.
- Generate CSRF tokens.
- Expose authentication ticket internals to controllers.

This is an Infrastructure-internal orchestration service.

An interface is not required unless there is an actual abstraction requirement.

### 8.2 SessionCookieService

Refactor the existing `SessionCookies` class into a centralized cookie service.

Responsibilities:

- Establish authenticated sessions through ASP.NET Core Cookie Authentication.
- Create pending session cookies.
- Read pending session cookies.
- Remove pending session cookies.
- Rotate authentication sessions.
- Sign out authenticated sessions.
- Apply consistent cookie settings.

Requirements:

- Use `HttpOnly` for authentication cookies.
- Use `HttpOnly` for pending authentication cookies.
- Use secure cookies.
- Preserve configured SameSite behavior.
- Ensure cookie deletion uses the correct path and domain.
- Do not expose raw session identifiers in response bodies.
- Do not manually create framework-managed authentication cookies.
- Do not issue CSRF tokens from this service.

Reuse the registered authentication scheme and existing Redis-backed ticket store.

## 9. Refactor Session Management

### 9.1 Service Contract

Define the Application contract.

```csharp
public interface ISessionManagementService
{
    Task<IReadOnlyList<SessionResponseModel>> GetSessionsAsync(
        CancellationToken cancellationToken);

    Task<bool> RevokeSessionAsync(
        string reference,
        CancellationToken cancellationToken);

    Task LogoutAsync(
        CancellationToken cancellationToken);

    Task LogoutAllAsync(
        CancellationToken cancellationToken);

    Task SwitchRoleAsync(
        string roleCode,
        CancellationToken cancellationToken);
}
```

Implement the entire service in Infrastructure.

### 9.2 Responsibilities

SessionManagementService must:

- Resolve the current user.
- Resolve the current session.
- Retrieve sessions owned by the current user.
- Revoke individual sessions.
- Revoke all sessions belonging to the current user.
- Perform logout.
- Validate requested active roles.
- Coordinate session rotation.
- Clear authentication cookies when required.
- Record authentication events.

The controller must not directly call `RedisTicketStore`.

### 9.3 RedisTicketStore

Keep the existing Redis-backed implementation of `ITicketStore`.

Because both SessionManagementService and RedisTicketStore reside in Infrastructure, SessionManagementService may directly depend on RedisTicketStore.

Do not introduce an Application-level session store interface solely to allow one Infrastructure service to call another Infrastructure service.

Introduce such an interface only if another storage implementation is genuinely required.

RedisTicketStore must:

- Keep Redis keys private.
- Store authentication tickets with appropriate TTL.
- Support session retrieval.
- Support session renewal.
- Support session removal.
- Maintain user-session indexes.
- Scope public session references to their owning users.
- Remove obsolete index entries.
- Prevent revoked sessions from being recreated by concurrent renewal.
- Preserve absolute session expiration.

Do not expose `AuthenticationTicket` through Application service contracts.

Map internal authentication tickets to `SessionResponseModel` inside Infrastructure.

### 9.4 Session Rotation

During active role switching:

```text
Resolve current session
    → Validate requested role
    → Validate session is still active
    → Create replacement session
    → Invalidate previous session
    → Issue replacement cookie
    → Record authentication event
```

Ensure rotation cannot restore a revoked session.

Use atomic Redis operations where required.

Review the existing `RotateAsync()` implementation before replacing it.

Preserve the original absolute session lifetime.

## 10. Refactor CSRF Protection

### 10.1 Preserve ASP.NET Core Antiforgery

Use the existing framework implementation.

Do not introduce custom cryptographic CSRF token generation.

The token configuration remains:

| Component            | Purpose                         |
| -------------------- | ------------------------------- |
| `__Host-antiforgery` | Framework-managed cookie token  |
| `XSRF-TOKEN`         | Frontend-readable request token |
| `X-XSRF-TOKEN`       | Request token header            |

The two cookies are intentional.

Do not store antiforgery tokens in Redis.

### 10.2 CsrfOptions

Place `CsrfOptions` in Infrastructure.

Use one configuration source for both Antiforgery registration and request token cookie issuance.

Suggested configuration:

```json
{
  "Csrf": {
    "HeaderName": "X-XSRF-TOKEN",
    "CookieTokenName": "__Host-antiforgery",
    "RequestTokenCookieName": "XSRF-TOKEN",
    "SameSite": "Lax",
    "Secure": true
  }
}
```

Requirements:

- Validate options during application startup.
- Use `/` as the cookie path.
- Do not configure a domain for `__Host-` cookies.
- Require secure cookies in production.
- Prefer HTTPS in local development.
- Keep cookie and header names consistent with Angular.

### 10.3 CsrfService

Implement CsrfService in Infrastructure.

```csharp
public sealed class CsrfService
{
    public void IssueToken(HttpContext context);

    public Task ValidateAsync(HttpContext context);
}
```

Responsibilities:

- Wrap `IAntiforgery`.
- Retrieve tokens through `GetAndStoreTokens()`.
- Write the request token into the frontend-readable cookie.
- Apply no-store response headers.
- Validate incoming tokens through `ValidateRequestAsync()`.

Example implementation:

```csharp
public sealed class CsrfService(
    IAntiforgery antiforgery,
    IOptions<CsrfOptions> options)
{
    public void IssueToken(HttpContext context)
    {
        var token = antiforgery
            .GetAndStoreTokens(context)
            .RequestToken
            ?? throw new InvalidOperationException(
                "Unable to generate CSRF request token.");

        context.Response.Cookies.Append(
            options.Value.RequestTokenCookieName,
            token,
            new CookieOptions
            {
                HttpOnly = false,
                Secure = true,
                SameSite = options.Value.SameSite,
                Path = "/"
            });

        context.Response.Headers.CacheControl = "no-store";
    }

    public Task ValidateAsync(HttpContext context)
    {
        return antiforgery.ValidateRequestAsync(context);
    }
}
```

Adapt property types to the final options implementation.

Do not introduce an `ICsrfService` interface in Application because CSRF is an HTTP security concern rather than an application use case.

### 10.4 SkipCsrfAttribute

Keep endpoint metadata in API.

```csharp
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Method,
    AllowMultiple = false,
    Inherited = true)]
public sealed class SkipCsrfAttribute : Attribute
{
}
```

Requirements:

- Keep independent from `AllowAnonymousAttribute`.
- Resolve through endpoint metadata.
- Do not implement MVC filter interfaces.
- Prefer action-level exemptions.

### 10.5 CsrfMiddleware

Keep the middleware in API.

The middleware delegates token validation to Infrastructure's CsrfService.

```csharp
public sealed class CsrfMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        CsrfService csrfService)
    {
        var endpoint = context.GetEndpoint();

        var shouldSkip = endpoint switch
        {
            null => true,

            _ when HttpMethods.IsGet(
                context.Request.Method) => true,

            _ when HttpMethods.IsHead(
                context.Request.Method) => true,

            _ when HttpMethods.IsOptions(
                context.Request.Method) => true,

            _ when HttpMethods.IsTrace(
                context.Request.Method) => true,

            _ when endpoint.Metadata
                .GetMetadata<SkipCsrfAttribute>() is not null => true,

            _ => false
        };

        if (!shouldSkip)
        {
            await csrfService.ValidateAsync(context);
        }

        await next(context);
    }
}
```

Requirements:

- Validate unsafe HTTP methods by default.
- Do not bypass CSRF because an endpoint allows anonymous access.
- Validate anonymous login and registration requests.
- Do not perform duplicate validation through MVC antiforgery filters.
- Ensure ordinary GET endpoints do not perform business state mutations.
- Ensure any non-endpoint request handlers are separately reviewed.

Map `AntiforgeryValidationException` to HTTP 400 with a stable error code:

```text
CSRF_VALIDATION_FAILED
```

Do not expose internal token validation details.

### 10.6 CsrfController

Create a dedicated API controller.

```csharp
[ApiController]
[ApiVersion("1.0")]
[AllowAnonymous]
[Route("api/v{version:apiVersion}/csrf")]
public sealed class CsrfController(
    CsrfService csrfService) : ControllerBase
{
    [HttpGet]
    public IActionResult GetToken()
    {
        csrfService.IssueToken(HttpContext);

        return NoContent();
    }
}
```

Move `/auth/csrf` to `/csrf`.

Update the frontend accordingly.

Do not return the request token in the response body unless a specific integration requires it.

## 11. Authentication and Authorization Configuration

### 11.1 AuthenticationExtensions

Move authentication registration into Infrastructure.

Responsibilities:

- Register Cookie Authentication.
- Configure default authentication schemes.
- Register Google OAuth.
- Register external temporary authentication.
- Configure authentication cookies.
- Register RedisTicketStore.
- Configure Data Protection.
- Register authentication-related services.

Preserve the existing scheme names and cookie configuration where compatible with security requirements.

### 11.2 AuthorizationExtensions

Configure secure-by-default authorization.

```csharp
services.AddAuthorization(options =>
{
    options.FallbackPolicy =
        new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();
});
```

Requirements:

- Require authentication by default.
- Explicitly declare public endpoints with `[AllowAnonymous]`.
- Preserve `[Authorize]` attributes for readability.
- Ensure custom authorization policies also require authentication.
- Do not treat pending authentication or external OAuth tickets as authenticated application sessions.

API authentication failures must return HTTP 401.

API authorization failures must return HTTP 403.

Do not redirect ordinary API requests to HTML login pages or Google.

### 11.3 CsrfExtensions

Register Antiforgery and CsrfService from Infrastructure.

```csharp
services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.Name = "__Host-antiforgery";
    options.Cookie.HttpOnly = true;
    options.Cookie.Path = "/";
    options.Cookie.SecurePolicy =
        CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});
```

The final implementation must bind these values from centralized validated configuration rather than duplicate hardcoded values.

### 11.4 Middleware Pipeline

Configure the relevant pipeline in API.

```csharp
app.UseForwardedHeaders();

app.UseExceptionHandler();

app.UseRouting();

app.UseCors();

app.UseAuthentication();

app.UseAuthorization();

app.UseMiddleware<CsrfMiddleware>();

app.MapControllers();
```

Preserve existing correlation, logging, rate limiting, and other middleware.

CSRF validation must occur:

- After endpoint selection.
- After authentication.
- Before controller execution.

Authorization precedes CSRF validation so unauthenticated requests to protected endpoints receive HTTP 401.

Anonymous unsafe endpoints still undergo CSRF validation.

## 12. Refactor Google Authentication

Move all Google OAuth orchestration into Infrastructure's `GoogleAuthenticationService`.

Responsibilities:

- Resolve the Google authentication scheme.
- Initiate OAuth login challenges.
- Initiate account-linking challenges.
- Apply authentication challenge rate limiting.
- Read external authentication results.
- Extract and validate provider claims.
- Distinguish login from account linking.
- Validate the initiating user and session.
- Invoke AuthenticationService for Google login.
- Invoke existing Google identity linking logic.
- Complete authentication sessions.
- Clear temporary external authentication cookies.
- Resolve the configured frontend redirect URL.

Do not return MVC-specific `IActionResult` objects from Infrastructure.

Use Infrastructure-specific operation results or ASP.NET Core authentication properties where needed.

Keep MVC response construction in API controllers.

For example:

```text
GoogleAuthenticationController
    → GoogleAuthenticationService
        → ASP.NET Core Google OAuth
        → AuthenticationService
        → AuthenticationSessionService
        → SessionCookieService
```

Requirements:

- Preserve framework-managed OAuth state and correlation protection.
- Preserve the external authentication scheme.
- Use provider subject as the stable external identity identifier.
- Do not identify Google accounts solely by email.
- Preserve verified-email rules.
- Validate frontend redirect configuration at startup.
- Do not accept arbitrary redirect destinations.
- Clear external authentication state when callback processing fails.
- Ensure temporary external authentication cannot grant access to business endpoints.

For account linking:

- Require an authenticated application session.
- Match the current user against the initiating user.
- Match the current session against the initiating session.
- Verify the external authentication operation belongs to the linking flow.
- Ensure the Google identity is not already linked to another user.
- Consider recent-authentication requirements for sensitive account changes.

## 13. Pending Authentication

Keep all pending authentication implementations in Infrastructure.

A pending authentication session represents successful credential verification without completed role selection.

It must not be considered an authenticated application session.

Flow:

```text
Credential verification
    → Retrieve eligible roles
    → Multiple roles detected
    → Create pending authentication challenge
    → Set pending session cookie
    → Return RoleSelectionRequired
```

Role completion:

```text
Read pending session cookie
    → Retrieve pending authentication
    → Validate account status
    → Validate selected role
    → Atomically consume pending authentication
    → Create authenticated session
    → Remove pending session cookie
```

Requirements:

- Use a separate pending session cookie.
- Store pending authentication state in Redis.
- Use a short configurable TTL.
- Use unpredictable identifiers.
- Do not expose pending session identifiers in response bodies.
- Do not grant application authorization through pending authentication.
- Validate current role eligibility before consuming the pending session.
- Prevent concurrent completion.
- Reject expired or previously consumed challenges.
- Ensure GET pending-session does not consume the challenge.

Reuse AuthenticationChallengeStore for existing challenge storage functionality.

Do not introduce another pending session store if the existing implementation can fulfill these requirements.

## 14. Separate Controller Responsibilities

### 14.1 AuthenticationController

Endpoints:

| Method | Endpoint                | Authorization  |
| ------ | ----------------------- | -------------- |
| POST   | `/auth/register`        | AllowAnonymous |
| POST   | `/auth/verify-email`    | AllowAnonymous |
| POST   | `/auth/login`           | AllowAnonymous |
| GET    | `/auth/pending-session` | AllowAnonymous |
| POST   | `/auth/select-role`     | AllowAnonymous |
| GET    | `/auth/me`              | Authorize      |

Expected dependencies:

```csharp
IAuthenticationService
IRegistrationService
AuthenticationSessionService
```

The controller should:

1. Receive request DTOs.
2. Map DTOs to Application models.
3. Invoke the appropriate service.
4. Complete HTTP authentication when required.
5. Map service results to response DTOs.
6. Return the appropriate HTTP status.

Remove direct dependencies on Redis, Antiforgery, authentication scheme discovery, cookie configuration, and logging infrastructure.

### 14.2 GoogleAuthenticationController

Endpoints:

| Method | Endpoint                | Authorization  |
| ------ | ----------------------- | -------------- |
| GET    | `/auth/google/login`    | AllowAnonymous |
| POST   | `/auth/google/link`     | Authorize      |
| GET    | `/auth/google/callback` | AllowAnonymous |

Expected dependency:

```csharp
GoogleAuthenticationService
```

The controller should only initiate the corresponding service operation and translate its result into a challenge, redirect, or HTTP response.

### 14.3 SessionsController

Endpoints:

| Method | Endpoint                     | Authorization |
| ------ | ---------------------------- | ------------- |
| POST   | `/auth/switch-role`          | Authorize     |
| POST   | `/auth/logout`               | Authorize     |
| POST   | `/auth/logout-all`           | Authorize     |
| GET    | `/auth/sessions`             | Authorize     |
| DELETE | `/auth/sessions/{reference}` | Authorize     |

Expected dependency:

```csharp
ISessionManagementService
```

Session-related infrastructure details must remain hidden from the controller.

### 14.4 CsrfController

Endpoint:

| Method | Endpoint | Authorization  |
| ------ | -------- | -------------- |
| GET    | `/csrf`  | AllowAnonymous |

Expected dependency:

```csharp
CsrfService
```

Preserve existing versioned authentication URLs except for the dedicated CSRF endpoint.

## 15. Authentication Event Logging

Implement AuthenticationEventLogger in Infrastructure.

Use the existing Serilog infrastructure.

Record structured events:

- LoginSucceeded
- LoginFailed
- PendingLoginCreated
- RoleSelected
- GoogleLoginSucceeded
- PendingGoogleLoginCreated
- GoogleIdentityLinked
- ActiveRoleSwitched
- Logout
- LogoutAll
- SessionRevoked

Include relevant information:

- Event name.
- User ID.
- Active role.
- Public session reference.
- Remote IP address.
- Trace ID.
- Client action ID.
- Client request ID.
- Timestamp.

Resolve request metadata through CurrentRequestContext.

Resolve authenticated identity through CurrentUserContext when appropriate.

Record successful authentication events only after the relevant operation succeeds.

Do not log:

- Passwords.
- Raw session identifiers.
- Pending session tokens.
- CSRF tokens.
- Verification codes.
- Google access or refresh tokens.

If authentication event logging is only used within Infrastructure, prefer a concrete implementation and remove the Application-level interface unless other Application services genuinely require it.

## 16. Frontend Integration

### 16.1 Initial Application Load

```text
Angular application starts
    → GET /api/v1/csrf
    → Receive antiforgery cookies
    → GET /api/v1/auth/me
    → Resolve authentication state
```

### 16.2 Normal Login

```text
POST /auth/login
    → Validate CSRF
    → AuthenticationService verifies credentials
    → Resolve eligible roles
    → AuthenticationSessionService completes result

Authenticated:
    → Establish session
    → Return authentication response
    → Frontend refreshes CSRF

RoleSelectionRequired:
    → Establish pending authentication
    → Return eligible roles
    → Frontend displays role selection
```

### 16.3 Role Selection

```text
POST /auth/select-role
    → Validate CSRF
    → Validate pending authentication
    → Validate selected role
    → Consume pending authentication
    → Establish authenticated session
    → Clear pending cookie
    → Frontend refreshes CSRF
```

### 16.4 Google Login

```text
GET /auth/google/login
    → Initiate OAuth challenge
    → Redirect to Google
    → Framework validates OAuth response
    → GoogleAuthenticationService processes external identity
    → Establish authenticated or pending session
    → Clear external authentication cookie
    → Redirect to frontend
    → Frontend refreshes CSRF
    → Resolve authentication state
```

### 16.5 Active Role Switching

```text
POST /auth/switch-role
    → Validate authentication
    → Validate CSRF
    → Validate requested role
    → Rotate session
    → Update authentication cookie
    → Frontend refreshes CSRF
```

### 16.6 Logout

```text
POST /auth/logout
    → Validate authentication
    → Validate CSRF
    → Remove authenticated session
    → Clear authentication cookie
    → Return HTTP 204
    → Frontend refreshes CSRF
    → Reset authentication state
```

### 16.7 CSRF Lifecycle

Refresh CSRF after authentication state changes:

- Successful credential login.
- Successful Google login.
- Successful pending role selection.
- Active role switching.
- Logout.
- Logout from all sessions.

Do not issue replacement CSRF tokens immediately after `SignInAsync()` using the potentially outdated principal from the same request.

Use a subsequent request to the dedicated CSRF endpoint.

Do not refresh CSRF for every request.

Ensure the frontend waits for refresh completion before issuing additional unsafe requests.

Do not automatically replay arbitrary state-changing requests after CSRF validation failures.

Use same-origin API routing and relative URLs for Angular's built-in XSRF integration where possible.

## 17. Endpoint Security Matrix

| Endpoint                     | Method | Authentication          | CSRF             |
| ---------------------------- | ------ | ----------------------- | ---------------- |
| `/csrf`                      | GET    | Anonymous allowed       | Skip             |
| `/auth/register`             | POST   | Anonymous allowed       | Required         |
| `/auth/verify-email`         | POST   | Anonymous allowed       | Required         |
| `/auth/login`                | POST   | Anonymous allowed       | Required         |
| `/auth/pending-session`      | GET    | Pending session         | Skip             |
| `/auth/select-role`          | POST   | Pending session         | Required         |
| `/auth/google/login`         | GET    | Anonymous allowed       | OAuth protection |
| `/auth/google/link`          | POST   | Required                | Required         |
| `/auth/google/callback`      | GET    | External authentication | OAuth protection |
| `/auth/me`                   | GET    | Required                | Skip             |
| `/auth/switch-role`          | POST   | Required                | Required         |
| `/auth/logout`               | POST   | Required                | Required         |
| `/auth/logout-all`           | POST   | Required                | Required         |
| `/auth/sessions`             | GET    | Required                | Skip             |
| `/auth/sessions/{reference}` | DELETE | Required                | Required         |

Keep pending authentication and external authentication validation independent from the default application authentication scheme.

Do not automatically
