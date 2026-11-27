using System.Text.Json;
using StackExchange.Redis;
using RRMS.Api.Dtos.Common;
using RRMS.Domain.Exceptions;
using RRMS.Domain.Utils;

namespace RRMS.Api.Middlewares;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly Serilog.ILogger _logger;
    private readonly IHostEnvironment _hostEnvironment;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        Serilog.ILogger logger,
        IHostEnvironment hostEnvironment)
    {
        _next = next;
        _logger = logger;
        _hostEnvironment = hostEnvironment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            var (statusCode, response) = CreateResponse(exception);

            if (statusCode >= StatusCodes.Status500InternalServerError)
            {
                _logger.Error(
                    exception,
                    "Unhandled exception while processing [{RequestMethod}] {RequestPath}",
                    context.Request.Method,
                    context.Request.Path);
            }
            else
            {
                _logger.Warning(
                    exception,
                    "Request failed with status code {StatusCode}: [{RequestMethod}] {RequestPath}",
                    statusCode,
                    context.Request.Method,
                    context.Request.Path);
            }

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonUtils.WebSerializerOptions));
        }
    }

    private (int StatusCode, ErrorResponse Response) CreateResponse(Exception exception)
    {
        return exception switch
        {
            RedisException => (StatusCodes.Status503ServiceUnavailable, new ErrorResponse
            {
                Message = "Authentication service is temporarily unavailable."
            }),
            RateLimitException rateLimit => (StatusCodes.Status429TooManyRequests, new ErrorResponse
            {
                Message = rateLimit.Message
            }),
            UnauthorizedException unauthorized =>
                (StatusCodes.Status401Unauthorized,
                new ErrorResponse
                {
                    Message = unauthorized.Message,
                    Details = unauthorized.Details
                }),

            ForbiddenException forbidden =>
                (StatusCodes.Status403Forbidden,
                new ErrorResponse
                {
                    Message = forbidden.Message,
                    Details = forbidden.Details
                }),

            NotFoundException notFound =>
                (StatusCodes.Status404NotFound,
                new ErrorResponse
                {
                    Message = notFound.Message,
                    Details = notFound.Details
                }),

            ConflictException conflict =>
                (StatusCodes.Status409Conflict,
                new ErrorResponse
                {
                    Message = conflict.Message,
                    Details = conflict.Details
                }),

            UnprocessableEntityException unprocessable =>
                (StatusCodes.Status422UnprocessableEntity,
                new ErrorResponse
                {
                    Message = unprocessable.Message,
                    Details = unprocessable.Details
                }),

            Exception ex =>
                (StatusCodes.Status500InternalServerError,
                new ErrorResponse
                {
                    Message = !_hostEnvironment.IsProduction() ? ex.Message : "Something went wrong.",
                })
        };
    }
}
