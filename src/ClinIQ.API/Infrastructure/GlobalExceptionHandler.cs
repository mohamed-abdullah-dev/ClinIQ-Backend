using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ClinIQ.API.Infrastructure;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Exception messages can contain SQL values, credentials, or patient input.
        logger.LogError("Unhandled {ExceptionType}; trace {TraceId}",
            exception.GetType().Name, httpContext.TraceIdentifier);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Server.Error",
            Detail = "An unexpected error occurred."
        };
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        var written = await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem
        });

        // Keep a safe JSON response even when the Accept header has no matching writer.
        if (!written)
            await httpContext.Response.WriteAsJsonAsync(problem,
                options: null, contentType: "application/problem+json", cancellationToken: cancellationToken);

        return true;
    }
}
