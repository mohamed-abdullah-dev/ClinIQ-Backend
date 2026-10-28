using ClinIQ.Domain.Common;
using ClinIQ.Domain.Common.Results;

namespace ClinIQ.Domain.Specialties;

public sealed class Specialty : AuditableEntity
{
    public const int NameMaxLength = 200;

    public string NameAr { get; private set; } = null!;
    public string NameEn { get; private set; } = null!;
    public string NormalizedNameAr { get; private set; } = null!; // TODO: الطريقه الي احنا استخدمناها دي علشان نمنع التكرار طريقه كويسه ولا في طريقه احسن ؟
    public string NormalizedNameEn { get; private set; } = null!;
    public byte[] Version { get; private set; } = [];

    private Specialty() { }

    private Specialty(string nameAr, string nameEn) : base(Guid.NewGuid())
    {
        NameAr = nameAr;
        NameEn = nameEn;
        NormalizedNameAr = NormalizeArabicName(nameAr);
        NormalizedNameEn = nameEn.ToUpperInvariant();
    }

    public static Result<Specialty> Create(string? nameAr, string? nameEn)
    {
        var trimmedNameAr = nameAr?.Trim() ?? string.Empty;
        var trimmedNameEn = nameEn?.Trim() ?? string.Empty;
        var errors = ValidateNames(trimmedNameAr, trimmedNameEn);
        if (errors.Count > 0) return errors;
        return new Specialty(trimmedNameAr, trimmedNameEn);
    }

    public Result<Success> Rename(string? nameAr, string? nameEn)
    {
        var trimmedNameAr = nameAr?.Trim() ?? string.Empty;
        var trimmedNameEn = nameEn?.Trim() ?? string.Empty;
        var errors = ValidateNames(trimmedNameAr, trimmedNameEn);
        if (errors.Count > 0) return Result<Success>.Failure(errors);
        NameAr = trimmedNameAr;
        NameEn = trimmedNameEn;
        NormalizedNameAr = NormalizeArabicName(trimmedNameAr);
        NormalizedNameEn = trimmedNameEn.ToUpperInvariant();
        return Result.Success;
    }

    private static List<Error> ValidateNames(string trimmedNameAr, string trimmedNameEn)
    {
        var errors = new List<Error>();

        if (trimmedNameAr.Length == 0)
            errors.Add(SpecialtyErrors.NameArRequired);
        else if (trimmedNameAr.Length > NameMaxLength)
            errors.Add(SpecialtyErrors.NameArTooLong);

        if (trimmedNameEn.Length == 0)
            errors.Add(SpecialtyErrors.NameEnRequired);
        else if (trimmedNameEn.Length > NameMaxLength)
            errors.Add(SpecialtyErrors.NameEnTooLong);

        return errors;
    }

    private static string NormalizeArabicName(string name) => name
        // Handle the decomposed spellings of أ, إ and آ as well as their single-character forms.
        .Replace("ا\u0654", "ا", StringComparison.Ordinal)
        .Replace("ا\u0655", "ا", StringComparison.Ordinal)
        .Replace("ا\u0653", "ا", StringComparison.Ordinal)
        .Replace('أ', 'ا')
        .Replace('إ', 'ا')
        .Replace('آ', 'ا')
        .ToUpperInvariant();
}
