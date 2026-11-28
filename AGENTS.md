# 1. BACKEND

## 1.1. Structure

- Use Clean Architecture for C# code, following the existing `webapi2` projects: API, Application, Domain, and Infrastructure.
- Use a flexible module architecture for Go code.
- Keep implementations simple and avoid unnecessary abstractions.

## 1.2. API and Models

- Create Request and Response DTOs for each API, using the `Request` and `Response` suffixes.
- Create corresponding application models for each feature, using the `RequestModel` and `ResponseModel` suffixes.
- Place DTOs in the API layer and models in the Application layer. Controllers convert request DTOs to models before calling services and convert response models to DTOs before returning them.
- Define static mapping methods on DTOs: `ToModel` for requests and `FromModel` for responses.
- Do not return persistence entities directly from controllers.
- Follow the existing controller style: explicitly declare response status codes and body types, and use request mapping, service invocation, and response mapping as separate steps.
- Validate requests and return consistent API error responses with appropriate status codes.

## 1.3. Services and Persistence

- Put read-only operations (SELECT) in `<X>QueryService` and write operations (INSERT, UPDATE, DELETE) in `<X>CommandService`.
- Use `Find` as the prefix for repository read methods and `Get` for service and controller read methods.
- Create dedicated repository methods for complex queries involving joins. Fetch only the data needed and avoid unnecessary database round trips and N+1 queries.
- Use parameterized database queries.
- For C#, use `IUnitOfWork` to commit database changes in command services.
- For C#, prioritize existing `GenericRepository` methods; create dedicated repository methods for complex queries or dynamic filtering.
- For C#, name injected repository and service fields using their full role, such as `_userRepository` and `_userService`, rather than abbreviated names such as `_users`.
- For C#, reuse the existing generic filtering, pagination, and sorting utilities when a feature needs them.
- For C#, use LINQ query syntax for joins; do not use method syntax for joins.
- For C#, use conventional constructors and constructor-based dependency injection. Do not use primary constructors.
- Keep database transactions short and use concurrency controls when needed.

## 1.4. Database Scripts

- Use MySQL `TIMESTAMP` for timestamp columns in DDL scripts.
- Use `snake_case` for table, column, and constraint names in DDL and DML scripts, and enclose each name in backticks.
- Format DDL for readability: put each column on its own indented line, align names, types, and constraints, and place `CREATE INDEX` and `ALTER TABLE` clauses on separate lines.
- Add a new SQL script under the latest `DDL/vX.XX` directory for new or modified tables, columns, indexes, or constraints. Add predefined data changes under the latest `DML/vX.XX` directory.
- Define primary keys inside `CREATE TABLE`. Define unique, foreign key, and check constraints in separate `ALTER TABLE` statements immediately below the corresponding `CREATE TABLE`.
- Do not add a redundant index for a foreign key when MySQL creates a suitable index automatically.
- Do not modify previously applied production scripts. Keep schema changes backward-compatible when required.

## 1.5. Security, Errors, and Logging

- Validate authentication and authorization for protected operations. Use JWT Bearer tokens for protected APIs.
- Use the Google `sub` claim as the stable identifier when mapping Google identities to internal users.
- Never hardcode secrets, passwords, tokens, or object storage credentials; use environment variables or secure configuration providers.
- Handle exceptions centrally, define meaningful business exceptions, and never expose stack traces or sensitive internal details to clients.
- Use parameterized logging and include correlation IDs when available. Never log passwords, tokens, or secrets.

## 1.6. Tests

- Always write unit tests for new or modified behavior. For C#, use xUnit and AwesomeAssertions, and name tests `FunctionName_Should{Outcome}_When{Condition}`.
- Structure every C# backend test using Arrange, Act, Assert (AAA). Mark each section with `// Arrange`, `// Act`, and `// Assert`, and separate the sections with a blank line.
- Test success, failure, and important edge cases; mock repositories and external dependencies in service unit tests.
- Run `dotnet test` for C# changes and ensure all tests pass. Never disable failing tests merely to pass the build.
- Use integration tests for database-specific behavior, constraints, and transaction rollback when appropriate.

## 1.7. C# Formatting

- Always use braces for control-flow bodies, including `if`, `else`, `for`, `foreach`, `while`, and `do`, even when the body contains only one statement.
- Format non-empty object initializers over multiple lines, with each property assignment on its own indented line:

  ```csharp
  _context.UserCredentials.Add(new UserCredential
  {
      Id = Guid.CreateVersion7(),
      UserId = user.Id,
      PasswordHash = passwordHash
  });
  ```

## 1.8. Go Formatting

- Format non-empty struct and map initializations over multiple lines, with each field or entry on its own line and a trailing comma.
- Format slice and array initializations over multiple lines; short slices and arrays of simple values, such as integers or short strings, may be inline.

# 2. FRONTEND

## 2.1. Structure

- `src/app/core`: Contains core logic, services, interceptors, guards, pipes, and application-wide infrastructure.
- `src/app/pages`: Contains page components reflecting the application's routing structure.
- `src/app/shared`: Contains reusable components, directives, and utilities shared across pages.

## 2.2. Rules

- Prioritize using `team-platform/ui` common components before implementing new components.
- Reuse common SCSS variables from `team-platform/ui` for application-specific styling.
- Follow Material Design principles.
- Avoid duplicating functionality already available in shared components.
- Follow existing project conventions for routing, state management, and error handling.
- Always integrate `clientCorrelationService` for API requests:
  - Use `startAction()` at the beginning of a user action that triggers API calls.
  - Use `completeAction()` when the action completes, whether successful or unsuccessful.
  - Ensure the required client correlation headers are attached through the existing interceptor.
  - Preserve the same action correlation identifier across requests belonging to the same logical user action.
