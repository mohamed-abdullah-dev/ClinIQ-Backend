using ClinIQ.Domain.Common.Results;
using ClinIQ.Domain.Common.Results.Abstractions;
using FluentValidation;
using MediatR;

namespace ClinIQ.Application.Common.Behaviours;

public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IResult<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var errors = new List<Error>();

        foreach (var validator in validators)
        {
            // A separate context per validator; scoped DbContext operations stay sequential.
            var validation = await validator.ValidateAsync(request, cancellationToken);
            errors.AddRange(validation.Errors.Select(failure =>
                Error.Validation(failure.ErrorCode, failure.ErrorMessage, failure.PropertyName)));
        }

        return errors.Count > 0
            ? TResponse.Failure(errors)
            : await next(cancellationToken);
    }
}
