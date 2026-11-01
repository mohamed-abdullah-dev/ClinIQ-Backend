using ClinIQ.Domain.Common;
using ClinIQ.Domain.Common.Results;

namespace ClinIQ.Domain.Doctors;

public enum DoctorRegistrationStatus { PendingReview = 1, Approved = 2, Rejected = 3 }
public enum ProfessionalGrade { GeneralPractitioner = 1, Specialist = 2, Consultant = 3 }
public enum DoctorEvidenceKind { PracticeLicense = 1, SyndicateRegistration = 2, Identity = 3, ProfessionalGrade = 4 }

public sealed class DoctorRegistration : AuditableEntity
{
    public Guid UserId { get; private set; }
    public string ProfessionalName { get; private set; } = null!;
    public string PhoneNumber { get; private set; } = null!;
    public string RegistrationNumber { get; private set; } = null!;
    public Guid SpecialtyId { get; private set; }
    public ProfessionalGrade ProfessionalGrade { get; private set; }
    public DoctorRegistrationStatus Status { get; private set; }
    public bool IsCurrent { get; private set; } = true;
    public int AttemptNumber { get; private set; } = 1;
    public Guid? PreviousRegistrationId { get; private set; }
    // The email capability identifies the submitting account without creating an authenticated session.
    public Guid? ResubmittedByUserId { get; private set; }
    public byte[] Version { get; private set; } = [];
    private readonly List<DoctorEvidenceDocument> _documents = [];
    public IReadOnlyCollection<DoctorEvidenceDocument> Documents => _documents.AsReadOnly();

    private DoctorRegistration() { }

    public Result<DoctorRegistrationReview> Review(bool approve, string? reason, Guid reviewerId, DateTimeOffset now)
    {
        if (reviewerId == Guid.Empty) throw new ArgumentException("A reviewer ID is required.", nameof(reviewerId));
        if (!IsCurrent || Status != DoctorRegistrationStatus.PendingReview) return DoctorReviewErrors.AlreadyReviewed;
        if (reviewerId == UserId) return DoctorReviewErrors.SelfReview;
        var trimmed = reason?.Trim() ?? "";
        if (trimmed.Length is < 1 or > 2000) return DoctorReviewErrors.ReasonRequired;
        Status = approve ? DoctorRegistrationStatus.Approved : DoctorRegistrationStatus.Rejected;
        return DoctorRegistrationReview.Create(Id, reviewerId, Status, trimmed, now);
    }

    public Result<DoctorRegistration> Resubmit(Guid id, string? name, string? phone, string? number,
        Guid specialtyId, ProfessionalGrade grade, IReadOnlyList<DoctorEvidenceMetadata> documents)
    {
        if (!IsCurrent || Status != DoctorRegistrationStatus.Rejected) return DoctorResubmissionErrors.NotRejected;
        var next = Create(id, UserId, name, phone, number, specialtyId, grade, documents);
        if (next.IsError) return next;
        next.Value.PreviousRegistrationId = Id;
        next.Value.AttemptNumber = checked(AttemptNumber + 1);
        next.Value.ResubmittedByUserId = UserId;
        IsCurrent = false;
        return next;
    }

    private DoctorRegistration(Guid id, Guid userId, string name, string phone, string number,
        Guid specialtyId, ProfessionalGrade grade) : base(id)
    {
        UserId = userId;
        ProfessionalName = name;
        PhoneNumber = phone;
        RegistrationNumber = number;
        SpecialtyId = specialtyId;
        ProfessionalGrade = grade;
        Status = DoctorRegistrationStatus.PendingReview;
    }

    public static Result<DoctorRegistration> Create(Guid id, Guid userId, string? name, string? phone,
        string? registrationNumber, Guid specialtyId, ProfessionalGrade grade,
        IReadOnlyList<DoctorEvidenceMetadata> documents)
    {
        var errors = new List<Error>();
        if (id == Guid.Empty || userId == Guid.Empty)
            throw new ArgumentException("Registration and user IDs must be assigned.");
        var trimmedName = name?.Trim() ?? "";
        var trimmedPhone = phone?.Trim() ?? "";
        var trimmedNumber = registrationNumber?.Trim() ?? "";
        if (trimmedName.Length is < 1 or > 200)
            errors.Add(Error.Validation("DoctorRegistration.Name", "Professional name must contain 1 to 200 characters.", "FullName"));
        if (trimmedPhone.Length is < 7 or > 32)
            errors.Add(Error.Validation("DoctorRegistration.Phone", "A valid contact phone number is required.", "PhoneNumber"));
        if (trimmedNumber.Length is < 1 or > 100)
            errors.Add(Error.Validation("DoctorRegistration.Number", "Registration number must contain 1 to 100 characters.", "RegistrationNumber"));
        if (specialtyId == Guid.Empty)
            errors.Add(Error.Validation("DoctorRegistration.Specialty", "Choose a specialty from the catalog.", "SpecialtyId"));
        if (!Enum.IsDefined(grade))
            errors.Add(Error.Validation("DoctorRegistration.Grade", "Choose a valid professional grade.", "ProfessionalGrade"));

        var kinds = documents.Select(document => document.Kind).ToArray();
        if (kinds.Length is < 3 or > 4 || kinds.Distinct().Count() != kinds.Length ||
            kinds.Any(kind => !Enum.IsDefined(kind)) ||
            !kinds.Contains(DoctorEvidenceKind.PracticeLicense) ||
            !kinds.Contains(DoctorEvidenceKind.SyndicateRegistration) ||
            !kinds.Contains(DoctorEvidenceKind.Identity) ||
            (grade != ProfessionalGrade.GeneralPractitioner && !kinds.Contains(DoctorEvidenceKind.ProfessionalGrade)))
            errors.Add(Error.Validation("DoctorRegistration.Documents", "Provide the required license, syndicate, identity and applicable grade evidence.", "Documents"));
        if (documents.Any(document => !document.IsValid))
            errors.Add(Error.Validation("DoctorRegistration.DocumentMetadata", "Document metadata is invalid.", "Documents"));
        if (errors.Count > 0) return errors;

        var registration = new DoctorRegistration(id, userId, trimmedName, trimmedPhone, trimmedNumber, specialtyId, grade);
        registration._documents.AddRange(documents.Select(document => DoctorEvidenceDocument.Create(id, document)));
        return registration;
    }
}

public sealed record DoctorEvidenceMetadata(DoctorEvidenceKind Kind, string StorageKey, string OriginalFileName,
    string MediaType, long Length, string Sha256)
{
    public const long MaxLength = 5 * 1024 * 1024;
    public bool IsValid => !string.IsNullOrWhiteSpace(StorageKey) && StorageKey.Length <= 160 &&
        !string.IsNullOrWhiteSpace(OriginalFileName) && OriginalFileName.Length <= 255 &&
        MediaType is "application/pdf" or "image/jpeg" or "image/png" &&
        Length is > 0 and <= MaxLength && Sha256 is { Length: 64 } && Sha256.All(Uri.IsHexDigit);
}

public sealed class DoctorEvidenceDocument : AuditableEntity
{
    public Guid DoctorRegistrationId { get; private set; }
    public DoctorEvidenceKind Kind { get; private set; }
    public string StorageKey { get; private set; } = null!;
    public string OriginalFileName { get; private set; } = null!;
    public string MediaType { get; private set; } = null!;
    public long Length { get; private set; }
    public string Sha256 { get; private set; } = null!;

    private DoctorEvidenceDocument() { }
    internal static DoctorEvidenceDocument Create(Guid registrationId, DoctorEvidenceMetadata metadata) => new(Guid.NewGuid())
    {
        DoctorRegistrationId = registrationId, Kind = metadata.Kind, StorageKey = metadata.StorageKey,
        OriginalFileName = metadata.OriginalFileName, MediaType = metadata.MediaType,
        Length = metadata.Length, Sha256 = metadata.Sha256
    };

    private DoctorEvidenceDocument(Guid id) : base(id) { }
}
