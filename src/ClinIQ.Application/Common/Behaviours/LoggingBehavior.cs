using System.Diagnostics;
using ClinIQ.Domain.Common.Results.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClinIQ.Application.Common.Behaviours;

public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var startedAt = Stopwatch.GetTimestamp();
        logger.LogInformation("Processing {RequestName}", requestName);

        var response = await next(cancellationToken);
        var elapsedMs = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;

        if (response is IResult { IsError: true } failure)
        {
            logger.LogWarning(
                "{RequestName} failed after {ElapsedMs} ms with codes {ErrorCodes}",
                requestName, elapsedMs, string.Join(", ", failure.Errors.Select(error => error.Code)));
        }
        else
        {
            logger.LogInformation(
                "{RequestName} completed in {ElapsedMs} ms", requestName, elapsedMs);
        }

        return response;
    }
}
