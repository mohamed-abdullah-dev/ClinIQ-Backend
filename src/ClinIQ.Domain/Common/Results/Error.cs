namespace ClinIQ.Domain.Common.Results;

public sealed record Error
{
    public string Code { get; }
    public string Description { get; }
    public ErrorType Type { get; }
    public string? Field { get; }

    private Error(string code, string description, ErrorType type, string? field = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        Code = code;
        Description = description;
        Type = type;
        Field = field;
    }

    public static Error Failure(string code, string description) =>
        new(code, description, ErrorType.Failure);

    public static Error Validation(string code, string description, string? field = null) =>
        new(code, description, ErrorType.Validation, field);

    public static Error NotFound(string code, string description) =>
        new(code, description, ErrorType.NotFound);

    public static Error Conflict(string code, string description) =>
        new(code, description, ErrorType.Conflict);

    public static Error Unauthorized(string code, string description) =>
        new(code, description, ErrorType.Unauthorized);

    public static Error Forbidden(string code, string description) =>
        new(code, description, ErrorType.Forbidden);
}
