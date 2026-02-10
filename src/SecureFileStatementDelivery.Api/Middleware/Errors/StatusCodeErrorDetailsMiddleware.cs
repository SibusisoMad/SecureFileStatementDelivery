using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace SecureFileStatementDelivery.Api.Middleware.Errors;

internal sealed class StatusCodeErrorDetailsMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IProblemDetailsService problemDetailsService)
    {
        await next(context);

        if (context.Response.HasStarted)
        {
            return;
        }

        var statusCode = context.Response.StatusCode;
        if (statusCode < 400)
        {
            return;
        }

        if (statusCode is StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(context.Response.ContentType))
        {
            return;
        }

        var traceId = context.TraceIdentifier;

        var title = statusCode switch
        {
            StatusCodes.Status400BadRequest => "Bad Request",
            StatusCodes.Status404NotFound => "Not Found",
            StatusCodes.Status409Conflict => "Conflict",
            StatusCodes.Status422UnprocessableEntity => "Unprocessable Entity",
            _ => "Request failed"
        };

        var details = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Instance = context.Request.Path
        };

        details.Extensions["traceId"] = traceId;

        context.Response.ContentType = null;

        await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = details
        });
    }
}
