using ClinIQ.Domain.Common.Results.Abstractions;

namespace ClinIQ.Domain.Common.Results;

public static class Result
{
    public static Success Success => default;
    public static Created Created => default;
    public static Updated Updated => default;
    public static Deleted Deleted => default;
}

public sealed class Result<TValue> : IResult<Result<TValue>>
{
    private readonly TValue? _value;

    public bool IsSuccess { get; }
    public bool IsError => !IsSuccess;
    public IReadOnlyList<Error> Errors { get; }

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access the value of a failed result.");

    private Result(TValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _value = value;
        IsSuccess = true;
        Errors = Array.Empty<Error>();
    }

    private Result(IEnumerable<Error> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        var snapshot = errors.ToArray();

        if (snapshot.Length == 0 || snapshot.Any(error => error is null))
            throw new ArgumentException("Failure requires at least one non-null error.", nameof(errors));

        Errors = Array.AsReadOnly(snapshot);
        IsSuccess = false;
    }

    public static Result<TValue> Success(TValue value) => new(value);
    public static Result<TValue> Failure(Error error) => new(new[] { error });
    public static Result<TValue> Failure(IEnumerable<Error> errors) => new(errors);

    public TResponse Match<TResponse>(
        Func<TValue, TResponse> onSuccess,
        Func<IReadOnlyList<Error>, TResponse> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return IsSuccess ? onSuccess(Value) : onFailure(Errors);
    }

    public static implicit operator Result<TValue>(TValue value) => Success(value);
    public static implicit operator Result<TValue>(Error error) => Failure(error);
    public static implicit operator Result<TValue>(List<Error> errors) => Failure(errors);
}

public readonly record struct Success;
public readonly record struct Created;
public readonly record struct Updated;
public readonly record struct Deleted;
