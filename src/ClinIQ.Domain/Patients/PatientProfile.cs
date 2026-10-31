using ClinIQ.Domain.Common;
using ClinIQ.Domain.Common.Results;

namespace ClinIQ.Domain.Patients;

public sealed class PatientProfile : AuditableEntity
{
    public Guid UserId { get; private set; }
    public string FullName { get; private set; } = null!;
    public string PhoneNumber { get; private set; } = null!;
    public byte[] Version { get; private set; } = [];

    private PatientProfile() { }
    private PatientProfile(Guid id) : base(id) { }

    public static Result<PatientProfile> Create(Guid userId, string fullName, string phoneNumber)
    {
        if (userId == Guid.Empty) throw new ArgumentException("A patient profile requires an account.", nameof(userId));
        var profile = new PatientProfile(Guid.NewGuid()) { UserId = userId };
        var updated = profile.UpdateContact(fullName, phoneNumber);
        return updated.IsSuccess ? profile : Result<PatientProfile>.Failure(updated.Errors);
    }

    public Result<Success> UpdateContact(string fullName, string phoneNumber)
    {
        var name = fullName?.Trim();
        var phone = phoneNumber?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length > 200) return PatientProfileErrors.InvalidName;
        if (string.IsNullOrEmpty(phone)) return PatientProfileErrors.InvalidPhone;
        var digits = phone.StartsWith('+') ? phone.AsSpan(1) : phone.AsSpan();
        if (digits.Length is < 7 or > 15) return PatientProfileErrors.InvalidPhone;
        foreach (var character in digits)
            if (!char.IsAsciiDigit(character)) return PatientProfileErrors.InvalidPhone;
        // Validate both fields before mutating, so a failure leaves the existing contact intact.
        FullName = name;
        PhoneNumber = phone;
        return Result.Success;
    }
}

public static class PatientProfileErrors
{
    public static readonly Error InvalidName = Error.Validation("PatientProfile.InvalidName", "Provide a name of 1 to 200 characters.", "FullName");
    public static readonly Error InvalidPhone = Error.Validation("PatientProfile.InvalidPhone", "Provide 7 to 15 digits, optionally starting with +.", "PhoneNumber");
    public static readonly Error NotFound = Error.NotFound("PatientProfile.NotFound", "Complete your patient profile before booking.");
    public static readonly Error Unauthorized = Error.Unauthorized("PatientProfile.Unauthorized", "Please log in to continue.");
    public static readonly Error Forbidden = Error.Forbidden("PatientProfile.Forbidden", "A confirmed Patient account is required.");
    public static readonly Error Stale = Error.Conflict("PatientProfile.StaleVersion", "Get the current profile and retry with its version.");
    public static readonly Error Busy = Error.Conflict("PatientProfile.Busy", "Your profile is being updated. Retry the request.");
}
