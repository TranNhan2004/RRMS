using Microsoft.OpenApi;
using Serilog;
using RRMS.Api.Extensions;
using RRMS.Api.Middlewares;
using RRMS.Application;
using RRMS.Infrastructure;
using RRMS.Infrastructure.Extensions;
using RRMS.Infrastructure.Persistence.Extensions;
using RRMS.Infrastructure.Persistence;

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

builder.Services.AddAppOptions(builder.Configuration);
builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplicationAuthentication(builder.Configuration);
builder.Services.AddApplicationAuthorization();
builder.Services.AddApplicationCsrf();
builder.Services.AddTrustedForwardedHeaders(builder.Configuration);
builder.Services.AddApplicationCors(builder.Configuration);
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
app.UseAuthorization();
app.UseMiddleware<CsrfMiddleware>();

app.MapControllers();

await app.RunAsync();

