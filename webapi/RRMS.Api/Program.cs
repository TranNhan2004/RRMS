using Microsoft.OpenApi;
using Serilog;
using RRMS.Api.Extensions;
using RRMS.Infrastructure.Services.Authentication;
using RRMS.Api.Middlewares;
using RRMS.Application;
using RRMS.Infrastructure;
using RRMS.Infrastructure.Persistence.Extensions;
using RRMS.Infrastructure.Persistence;
using RRMS.Domain.Constants.Policies;
using RRMS.Api;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
{
    var userFilePath = Path.Combine(builder.Environment.ContentRootPath, "admin-cre.txt");
    if (File.Exists(userFilePath))
    {
        var lines = await File.ReadAllLinesAsync(userFilePath);
        if (lines.Length != 3 || lines.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidOperationException("The user file must contain exactly three non-empty lines: username, email, and password.");
        }

        var generator = new UserInsertSqlGenerator(new Argon2PasswordService());
        Console.WriteLine(generator.Generate(lines[0], lines[1], lines[2]));
    }
}

var isDeployedEnvironment =
    builder.Environment.IsEnvironment("Dev")
    || builder.Environment.IsEnvironment("Test")
    || builder.Environment.IsStaging()
    || builder.Environment.IsProduction();

var exposeApiDocs =
    builder.Environment.IsDevelopment()
    || builder.Environment.IsEnvironment("Dev")
    || builder.Environment.IsEnvironment("Test");

builder.Services.AddApplicationOptions(builder.Configuration);
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplicationAuthentication(builder.Configuration);
builder.Services.AddApplicationAuthorization();
builder.Services.AddApplicationCsrf();
builder.Services.AddTrustedForwardedHeaders(builder.Configuration);
builder.Services.AddApplicationCors(builder.Configuration);
builder.Services.AddApplicationRateLimiter(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddApiVersioningConfiguration();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RRMS API",
        Version = "v1"
    });
});

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services);
});

var app = builder.Build();

await app.Services.WarmupDatabaseAsync<AppDbContext>(connectionCount: 5);

app.UseForwardedHeaders();
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate =
        "HTTP {RequestMethod} {RequestPath} responded {StatusCode} " +
        "in {Elapsed:0.0000} ms " +
        "IP={RemoteIpAddress} " +
        "XFF={XForwardedFor} " +
        "ClientActionId={ClientActionId} " +
        "ClientRequestId={ClientRequestId}";

    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set(
            "XForwardedFor",
            httpContext.Request.Headers["X-Forwarded-For"].ToString());

        diagnosticContext.Set(
            "RemoteIpAddress",
            httpContext.Connection.RemoteIpAddress?.ToString());

        diagnosticContext.Set(
            "ClientActionId",
            httpContext.Request.Headers["X-Client-Action-Id"].FirstOrDefault());

        diagnosticContext.Set(
            "ClientRequestId",
            httpContext.Request.Headers["X-Client-Request-Id"].FirstOrDefault());
    };
});
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

if (isDeployedEnvironment)
{
    app.UseHttpsRedirection();
}

if (exposeApiDocs)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRouting();
app.UseCors(CorsPolicies.Default);

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.UseApplicationCsrf(app.Environment);

app.MapControllers();

await app.RunAsync();

