using ClinIQ.Domain.Common.Results;

namespace ClinIQ.Domain.Specialties;

public static class SpecialtyErrors
{
    public static readonly Error NotFound = Error.NotFound("Specialty.NotFound", "The specialty was not found.");
    public static readonly Error Unauthorized = Error.Unauthorized("Specialty.Unauthorized", "Please log in to continue.");
    public static readonly Error Forbidden = Error.Forbidden("Specialty.Forbidden", "A confirmed Admin account is required.");
    public static readonly Error NameArTaken = Error.Conflict("Specialty.NameArTaken", "The Arabic name is already used by another specialty.");
    public static readonly Error NameEnTaken = Error.Conflict("Specialty.NameEnTaken", "The English name is already used by another specialty.");
    public static readonly Error InUse = Error.Conflict("Specialty.InUse", "A specialty referenced by a doctor, registration or professional change cannot be deleted.");
    public static readonly Error Stale = Error.Conflict("Specialty.StaleVersion", "Get the current specialty and retry with its version.");
    public static readonly Error Busy = Error.Conflict("Specialty.Busy", "The specialty is being edited. Retry the request.");
    public static readonly Error NameArRequired = Error.Validation(
        "Specialty.NameArRequired", "Arabic name is required.", nameof(Specialty.NameAr));

    public static readonly Error NameArTooLong = Error.Validation(
        "Specialty.NameArTooLong",
        $"Arabic name must not exceed {Specialty.NameMaxLength} characters.",
        nameof(Specialty.NameAr));

    public static readonly Error NameEnRequired = Error.Validation(
        "Specialty.NameEnRequired", "English name is required.", nameof(Specialty.NameEn));

    public static readonly Error NameEnTooLong = Error.Validation(
        "Specialty.NameEnTooLong",
        $"English name must not exceed {Specialty.NameMaxLength} characters.",
        nameof(Specialty.NameEn));
}
