using Microsoft.OpenApi;
using Serilog;
using RRMS.Api.Extensions;
using RRMS.Api.Middlewares;
using RRMS.Application;
using RRMS.Infrastructure;
using RRMS.Infrastructure.Options;

var builder = WebApplication.CreateBuilder(args);

var isDeployedEnvironment =
    builder.Environment.IsEnvironment("Dev")
    || builder.Environment.IsEnvironment("Test")
    || builder.Environment.IsStaging()
    || builder.Environment.IsProduction();

var exposeApiDocs =
    builder.Environment.IsDevelopment()
    || builder.Environment.IsEnvironment("Dev")
    || builder.Environment.IsEnvironment("Test");

var credentialOptions = builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>()?.Credentials ?? new CredentialAuthOptions();
if (!credentialOptions.SecureCookies && !builder.Environment.IsEnvironment("Local"))
{
    throw new InvalidOperationException("Insecure authentication cookies require the Local environment.");
}

builder.Services.AddAppOptions(builder.Configuration);
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplicationAuthentication(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddApplicationCors(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddApiVersioningConfiguration();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Team Organization API",
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

app.UseSerilogRequestLogging();
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
app.UseCors(CorsExtensions.PolicyName);

app.UseAuthentication();
if (!app.Environment.IsEnvironment("Local"))
{
    app.UseMiddleware<CsrfMiddleware>();
}
app.UseAuthorization();

app.MapControllers();

app.Run();

