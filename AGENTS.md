# 1. BACKEND

## 1.1. Technology Stack

- Language: Java 21+
- Framework: Spring Boot
- ORM: Spring Data JPA + Hibernate
- Database: MySQL
- Cache: Spring Data Redis
- Object Storage: AWS SDK v2 (S3-compatible, RustFS)
- Security: Spring Security + JWT + Google OAuth2/OIDC
- Testing: JUnit 5 + Mockito + AssertJ

## 1.2. Structure

Follow a traditional **Layered Architecture (Package by Layer)**.

```text
src/main/java/com/roomrental/webapi
├── config/
├── controller/
├── service/
│   ├── UserQueryService.java
│   ├── UserCommandService.java
│   ├── RoomQueryService.java
│   └── RoomCommandService.java
├── repository/
├── entity/
├── dto/
│   ├── common/
│   └── v1/
├── model/
│   ├── common/
│   └── user/
├── mapper/
├── exception/
└── WebApiApplication.java
```

- Organize packages by technical responsibility, not by domain or feature.
- Do not introduce Clean Architecture, Hexagonal Architecture, or domain-specific architectural layers.
- Controllers handle HTTP requests, validation, and response mapping.
- Services contain business logic, application workflows, and transaction boundaries.
- Repositories handle database access.
- Entities represent JPA persistence models.
- DTOs represent API contracts.
- Models represent input/output data used by Services.
- Mappers handle conversions between DTOs, Models, and Entities.
- Use constructor-based dependency injection with `final` fields.
- Use `var` for Java local variables when the type is clear from the initializer; otherwise, declare the type explicitly.
- Keep implementations simple and avoid unnecessary abstraction.

## 1.3. API and Models

- Always create Request/Response DTOs for each API with the suffixes `Request` and `Response`.
- Always create corresponding service-level models with the suffixes `RequestModel` and `ResponseModel`.
- Place API DTOs in `dto` and `dto`.
- Place Service Models in `model` and `model`.
- Controllers must convert DTOs to Models before calling Services.
- Services must return Models rather than API DTOs.
- Controllers must convert ResponseModels to Response DTOs.
- Use static mapping methods in dedicated classes under `mapper/`.
- Never return JPA Entities directly from Controllers.
- Use Jakarta Bean Validation (`@Valid`, `@NotNull`, `@NotBlank`, etc.) for request validation.
- Use Java classes for DTOs and Models. Use records only for small objects declared directly inside a repository or service, with private, protected, or package-private accessibility.

## 1.4. Query and Command Services

- Put all read-only operations (SELECT) in `<X>QueryService`.
- Put all write operations (INSERT, UPDATE, DELETE) in `<X>CommandService`.
- Do not execute database modifications inside Query Services.
- Use `@Transactional(readOnly = true)` for Query Service operations requiring a transactional persistence context.
- Use `@Transactional` for Command Service operations that modify database records.
- Let Spring manage transaction commit and rollback automatically.
- Do not introduce a custom `IUnitOfWork` abstraction.
- Keep transaction boundaries at the Service level.
- Avoid long-running operations and external API calls inside database transactions.
- Use optimistic or pessimistic locking when concurrent modifications must be controlled.

## 1.5. Repository and Database Access

- Use Spring Data JPA (`JpaRepository`) for standard database operations.
- Use the `find` prefix for Repository query methods.
- Use the `get` prefix for read operations in Services and Controllers.
- Create dedicated Repository methods for complex queries involving multiple tables.
- Avoid unnecessary database round trips by fetching related data efficiently.
- Prevent N+1 queries using DTO projections, JPQL JOIN FETCH, or EntityGraph where appropriate.
- Prefer JPQL or derived queries for simple queries.
- Use native SQL when necessary for database-specific operations or performance.
- Fetch only the fields required by the feature when practical.
- Use Spring Data `Pageable` and `Sort` for pagination and sorting.
- Reuse existing common filtering and pagination utilities.
- Always parameterize SQL and JPQL queries.
- Consider indexes and execution plans when optimizing complex queries.

## 1.6. Database Scripts

- Use `snake_case` for table, column, and constraint names in DDL and DML scripts, and enclose each name in backticks.
- Format DDL for readability: put each column on its own indented line, align column names, data types, and nullability or constraints in columns using consistent spacing, and place `CREATE INDEX` and `ALTER TABLE` clauses on separate, clearly indented lines.
- For new or modified columns, tables, indexes, or constraints, add a new SQL script under `ddl/vX.XX` using the latest version.
- For predefined data insertion or updates, add a new SQL script under `dml/vX.XX` using the latest version.
- Define primary keys inside `CREATE TABLE` statements. Define unique, foreign key, and check constraints in separate `ALTER TABLE` statements immediately below the corresponding `CREATE TABLE` statement so each part can be run independently.
- Do not create a redundant index for a foreign key when MySQL will create a suitable index automatically.
- Do not modify previously applied production scripts.
- Do not rely on Hibernate `ddl-auto=update` in production.
- Prefer `ddl-auto=validate` when database schemas are managed through SQL scripts.
- Ensure schema changes are backward-compatible when required.

## 1.7. Authentication and Security

- Use Spring Security for authentication and authorization.
- Use JWT Bearer tokens for protected APIs.
- Use Spring Security OAuth2 Client for Google login.
- Map Google identities to internal users after successful authentication.
- Use the Google `sub` claim as the stable external identity identifier.
- Use application-managed JWTs for internal API authentication.
- Never hardcode secrets, passwords, tokens, or object storage credentials.
- Store sensitive configuration in environment variables or secure configuration providers.
- Validate both authentication and authorization before allowing protected operations.

## 1.8. Exception Handling and Logging

- Use `@RestControllerAdvice` for centralized exception handling.
- Return consistent API error responses with appropriate HTTP status codes.
- Define meaningful custom exceptions for business errors.
- Never expose stack traces or sensitive internal information to clients.
- Use SLF4J for application logging.
- Prefer parameterized logging.
- Include correlation IDs in logs when available.
- Never log passwords, access tokens, refresh tokens, or secrets.

## 1.9. Unit Tests and Integration Tests

- Always write Unit Tests for new or modified business logic.
- Use JUnit 5, Mockito, and AssertJ.
- Name Java test files and their test classes with the `Tests` suffix.
- Follow the test naming convention: `FunctionName_Should{Outcome}_When{Condition}`.
- Follow the Arrange–Act–Assert pattern in every Java unit test case, using the comments `// Arrange`, `// Act`, and `// Assert` to clearly mark each phase. Separate the phases with exactly one blank line.
- Use `@ExtendWith(MockitoExtension.class)` for Service Unit Tests.
- Use `@Mock` for dependencies and constructor injection or `@InjectMocks` for the tested Service.
- Keep Java unit tests free of null-safety and unchecked type warnings. Prefer explicitly null-safe lambdas or assertions and typed `@Mock` fields for generic dependencies; when a warning cannot be avoided, document and narrowly scope any suppression.
- Do not load Spring Application Context for ordinary Unit Tests.
- Mock Repositories and external dependencies when testing Services.
- Use Mockito `verify()`, `never()`, and `times()` to verify important interactions.
- Use AssertJ `assertThat()` and `assertThatThrownBy()` for assertions.
- Test success cases, failures, exceptions, and important edge cases.
- Use `@ParameterizedTest` for related test scenarios.
- Use `@WebMvcTest` and MockMvc for Controller tests.
- Use `@DataJpaTest` for Repository integration tests.
- Prefer Testcontainers with MySQL when testing database-specific behavior.
- Verify transaction rollback and database constraints through integration tests.
- Ensure every Java change compiles successfully and all tests pass by running `mvn test`.
- Never disable failing tests simply to pass the build.

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
