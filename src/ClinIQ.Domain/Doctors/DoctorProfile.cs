using ClinIQ.Domain.Common;
using ClinIQ.Domain.Common.Results;

namespace ClinIQ.Domain.Doctors;

public sealed class DoctorProfile : AuditableEntity
{
    public Guid UserId { get; private set; }
    public Guid DoctorRegistrationId { get; private set; }
    public string ProfessionalName { get; private set; } = null!;
    public Guid SpecialtyId { get; private set; }
    public ProfessionalGrade ProfessionalGrade { get; private set; }
    public byte[] Version { get; private set; } = [];

    internal Result<Success> ApplyApprovedChange(DoctorProfessionalChange request)
    {
        if (request.Status != ProfessionalChangeStatus.PendingReview || request.DoctorProfileId != Id || request.UserId != UserId ||
            !Version.AsSpan().SequenceEqual(request.OriginalProfileVersion)) return ProfessionalChangeErrors.StaleProfile;
        ProfessionalName = request.ProfessionalName; SpecialtyId = request.SpecialtyId; ProfessionalGrade = request.ProfessionalGrade;
        return Result.Success;
    }
    private DoctorProfile() { }
    private DoctorProfile(Guid id) : base(id) { }
    public static Result<DoctorProfile> FromApprovedRegistration(DoctorRegistration registration)
    {
        if (!registration.IsCurrent || registration.Status != DoctorRegistrationStatus.Approved)
            return Error.Conflict("DoctorProfile.NotApproved", "Only an approved registration can create a doctor profile.");
        return new DoctorProfile(Guid.NewGuid())
        {
            UserId = registration.UserId, DoctorRegistrationId = registration.Id,
            ProfessionalName = registration.ProfessionalName, SpecialtyId = registration.SpecialtyId,
            ProfessionalGrade = registration.ProfessionalGrade
        };
    }
}
