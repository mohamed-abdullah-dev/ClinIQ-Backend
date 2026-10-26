namespace ClinIQ.Domain.Common.Results.Abstractions;

public interface IResult
{
    bool IsSuccess { get; }
    bool IsError { get; }
    IReadOnlyList<Error> Errors { get; }
}

// TSelf lets the validation pipeline create the exact response type without reflection.
public interface IResult<TSelf> : IResult where TSelf : IResult<TSelf>
{
    static abstract TSelf Failure(IEnumerable<Error> errors);
}
