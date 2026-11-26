using Microsoft.AspNetCore.Antiforgery;
using RRMS.Api.Attributes;
using RRMS.Api.Dtos.Common;
using RRMS.Domain.Utils;
using RRMS.Infrastructure.Services.Csrf;

namespace RRMS.Api.Middlewares;

public class CsrfMiddleware
{
    private readonly RequestDelegate _next;

    public CsrfMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, CsrfService csrfService)
    {
        var endpoint = context.GetEndpoint();
        var shouldSkip = endpoint == null || HttpMethods.IsGet(context.Request.Method) ||
            HttpMethods.IsHead(context.Request.Method) || HttpMethods.IsOptions(context.Request.Method) ||
            HttpMethods.IsTrace(context.Request.Method) || endpoint.Metadata.GetMetadata<SkipCsrfAttribute>() != null;
        if (!shouldSkip)
        {
            try
            {
                await csrfService.ValidateAsync(context);
            }
            catch (AntiforgeryValidationException)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new ErrorResponse
                {
                    Message = "Invalid or missing antiforgery token.",
                    Details = new Dictionary<string, object?>
                    {
                        ["code"] = "CSRF_VALIDATION_FAILED"
                    }
                }, JsonUtils.WebSerializerOptions);
                return;
            }
        }

        await _next(context);
    }
}
