using ClinIQ.Domain.Common;
using ClinIQ.Domain.Common.Results;

namespace ClinIQ.Domain.PracticeLocations;

public enum PracticeLocationStatus { Draft = 1, PendingReview = 2, NeedsInformation = 3, Rejected = 4, Approved = 5 }
public enum PracticeLifecycleState { Active = 1, PendingDeparture = 2, Ended = 3 }

public sealed class DoctorPracticeLocation : AuditableEntity
{
    public Guid DoctorProfileId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? ClinicBranchId { get; private set; }
    public Guid? SourceInvitationId { get; private set; }
    public Guid? SourceLinkId { get; private set; }
    public bool IsActive { get; private set; } = true;
    public PracticeLifecycleState LifecycleState { get; private set; } = PracticeLifecycleState.Active;
    public string Name { get; private set; } = null!;
    public string Address { get; private set; } = null!;
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }
    public decimal ConsultationFee { get; private set; }
    public string Currency { get; private set; } = null!;
    public PracticeLocationStatus Status { get; private set; } = PracticeLocationStatus.Draft;
    public int AttemptNumber { get; private set; }
    public Guid? CurrentSubmissionId { get; private set; }
    public byte[] Version { get; private set; } = [];
    private DoctorPracticeLocation() { }
    private DoctorPracticeLocation(Guid id) : base(id) { }
    public static DoctorPracticeLocation Create(Guid id, Guid profileId, Guid userId, PracticeLocationDetails details, ConsultationPrice price)
    {
        if (id == Guid.Empty || profileId == Guid.Empty || userId == Guid.Empty) throw new ArgumentException("Location ownership IDs are required.");
        var row = new DoctorPracticeLocation(id) { DoctorProfileId = profileId, UserId = userId };
        row.Apply(details, price); return row;
    }
    public Result<Success> UpdateDraft(Guid actor, PracticeLocationDetails details, ConsultationPrice price)
    {
        if (ClinicBranchId.HasValue) return PracticeLocationErrors.ManagedLocation;
        if (actor != UserId) return PracticeLocationErrors.Forbidden;
        if (Status is not (PracticeLocationStatus.Draft or PracticeLocationStatus.NeedsInformation or PracticeLocationStatus.Rejected))
            return PracticeLocationErrors.DraftLocked;
        Apply(details, price); return Result.Success;
    }
    public Result<PracticeLocationSubmission> Submit(Guid submissionId, Guid actor, IReadOnlyList<PracticeLocationEvidenceMetadata> documents)
    {
        if (ClinicBranchId.HasValue) return PracticeLocationErrors.ManagedLocation;
        if (actor != UserId) return PracticeLocationErrors.Forbidden;
        if (Status is not (PracticeLocationStatus.Draft or PracticeLocationStatus.NeedsInformation or PracticeLocationStatus.Rejected))
            return PracticeLocationErrors.DraftLocked;
        if (documents.Count is < 1 or > 2 || documents.Any(x => !x.IsValid) || documents.Select(x => x.Kind).Distinct().Count() != documents.Count ||
            !documents.Any(x => x.Kind == PracticeLocationEvidenceKind.FacilityLicense)) return PracticeLocationErrors.RequiredEvidence;
        var next = PracticeLocationSubmission.Create(submissionId, this, checked(AttemptNumber + 1), documents);
        AttemptNumber = next.AttemptNumber; CurrentSubmissionId = next.Id; Status = PracticeLocationStatus.PendingReview;
        return next;
    }
    public Result<Success> Review(PracticeLocationSubmission submission, PracticeLocationStatus outcome, string? reason,
        Guid reviewer, DateTimeOffset now, bool licenseMatchesAddress, bool doctorWorkLinkVerified)
    {
        if (ClinicBranchId.HasValue) return PracticeLocationErrors.ManagedLocation;
        if (reviewer == Guid.Empty) throw new ArgumentException("A reviewer ID is required.");
        if (reviewer == UserId) return PracticeLocationErrors.SelfReview;
        if (Status != PracticeLocationStatus.PendingReview || CurrentSubmissionId != submission.Id || submission.LocationId != Id ||
            submission.Status != PracticeLocationStatus.PendingReview) return PracticeLocationErrors.AlreadyReviewed;
        if (outcome is not (PracticeLocationStatus.Approved or PracticeLocationStatus.Rejected or PracticeLocationStatus.NeedsInformation))
            return PracticeLocationErrors.InvalidOutcome;
        var trimmed = reason?.Trim() ?? "";
        if (trimmed.Length is < 1 or > 2000) return PracticeLocationErrors.ReasonRequired;
        if (outcome == PracticeLocationStatus.Approved && (!licenseMatchesAddress || !doctorWorkLinkVerified))
            return PracticeLocationErrors.UnverifiedEvidence;
        submission.RecordDecision(outcome, trimmed, reviewer, now, licenseMatchesAddress, doctorWorkLinkVerified);
        Status = outcome; return Result.Success;
    }
    public Result<Success> ChangeFee(Guid actor, ConsultationPrice price)
    {
        if (ClinicBranchId.HasValue) return PracticeLocationErrors.ManagedLocation;
        if (actor != UserId) return PracticeLocationErrors.Forbidden;
        if (Status != PracticeLocationStatus.Approved) return PracticeLocationErrors.NotApproved;
        ConsultationFee = price.Amount; Currency = price.Currency; return Result.Success;
    }
    public Result<Success> ApplyCorrection(PremisesCorrections.PremisesCorrection correction)
    {
        if (ClinicBranchId.HasValue) return PracticeLocationErrors.ManagedLocation;
        if (Status != PracticeLocationStatus.Approved) return PremisesCorrections.PremisesCorrectionErrors.NotApproved;
        if (correction.Status != PremisesCorrections.PremisesCorrectionStatus.Approved || correction.LocationId != Id ||
            correction.ApplicantUserId != UserId || !Version.SequenceEqual(correction.OriginalVersion) ||
            PremisesCorrections.PremisesDetails.From(this) != correction.Original) return PremisesCorrections.PremisesCorrectionErrors.Stale;
        var details = correction.Proposed;
        Name = details.Name; Address = details.Address; Latitude = details.Latitude; Longitude = details.Longitude;
        return Result.Success;
    }
    private void Apply(PracticeLocationDetails details, ConsultationPrice price)
    {
        Name = details.Name; Address = details.Address; Latitude = details.Latitude; Longitude = details.Longitude;
        ConsultationFee = price.Amount; Currency = price.Currency;
    }
    public Result<bool> BeginDeparture()
    {
        if (!IsActive || Status != PracticeLocationStatus.Approved) return PracticeLifecycle.LifecycleErrors.Unavailable;
        if (LifecycleState == PracticeLifecycleState.PendingDeparture) return false;
        if (LifecycleState != PracticeLifecycleState.Active) return PracticeLifecycle.LifecycleErrors.Closed;
        LifecycleState = PracticeLifecycleState.PendingDeparture;
        return true;
    }
    public Result<bool> FinalizeDeparture(int outstanding)
    {
        if (outstanding < 0) throw new ArgumentOutOfRangeException(nameof(outstanding));
        if (LifecycleState == PracticeLifecycleState.Ended) return false;
        if (LifecycleState != PracticeLifecycleState.PendingDeparture) return PracticeLifecycle.LifecycleErrors.NotPending;
        if (outstanding != 0) return PracticeLifecycle.LifecycleErrors.Outstanding;
        LifecycleState = PracticeLifecycleState.Ended;
        IsActive = false;
        return true;
    }
    public static Result<DoctorPracticeLocation> FromInvitation(Invitations.WorkInvitation invitation, Doctors.DoctorProfile profile, Clinics.ClinicBranch branch)
    {
        if (invitation.Kind != Invitations.WorkInvitationKind.Doctor || invitation.Status != Invitations.WorkInvitationStatus.Accepted ||
            invitation.ClosedByUserId != profile.UserId || invitation.BranchId != branch.Id || !branch.IsActive || branch.LifecycleState != Clinics.BranchLifecycleState.Open) return Invitations.InvitationErrors.Forbidden;
        var row = Create(Guid.NewGuid(), profile.Id, profile.UserId, PracticeLocationDetails.Create(branch.Name, branch.Address, branch.Latitude, branch.Longitude).Value,
            ConsultationPrice.Create(invitation.ConsultationFee!.Value, invitation.Currency).Value);
        row.ClinicBranchId = branch.Id; row.SourceInvitationId = invitation.Id; row.Status = PracticeLocationStatus.Approved;
        return row;
    }
    public Result<Success> ChangeManagedFee(ConsultationPrice price)
    {
        if (!ClinicBranchId.HasValue || !IsActive || Status != PracticeLocationStatus.Approved) return Invitations.InvitationErrors.Forbidden;
        ConsultationFee = price.Amount; Currency = price.Currency; return Result.Success;
    }
    public Result<Success> LinkToBranch(PracticeLinks.PracticeLinkRequest request, Clinics.ClinicBranch branch)
    {
        if (!PracticeLinks.PracticeLinkRequest.Eligible(this, branch)) return PracticeLinks.PracticeLinkErrors.Unavailable;
        if (request.Status != PracticeLinks.PracticeLinkStatus.Accepted || request.LocationId != Id || request.BranchId != branch.Id ||
            request.DoctorUserId != UserId || !Version.SequenceEqual(request.LocationVersion) || !branch.Version.SequenceEqual(request.BranchVersion))
            return PracticeLinks.PracticeLinkErrors.Stale;
        // Retain independent evidence, details, fee, identity and every existing session's operational snapshot.
        ClinicBranchId = branch.Id; SourceLinkId = request.Id;
        return Result.Success;
    }
}

public static class PracticeLocationErrors
{
    public static readonly Error ManagedLocation = Error.Conflict("PracticeLocation.ManagedLocation", "Use the clinic's authorized workflow for a managed branch association.");
    public static readonly Error Unauthorized = Error.Unauthorized("PracticeLocation.Unauthorized", "Sign in before managing practice locations.");
    public static readonly Error Forbidden = Error.Forbidden("PracticeLocation.Forbidden", "A confirmed, approved account with the required role is necessary.");
    public static readonly Error NotFound = Error.NotFound("PracticeLocation.NotFound", "The location, submission or document was not found.");
    public static readonly Error DraftLocked = Error.Conflict("PracticeLocation.DraftLocked", "Only a draft, rejected or information-requested location can be edited and submitted through this endpoint.");
    public static readonly Error NotApproved = Error.Conflict("PracticeLocation.NotApproved", "Only an approved independent location supports a direct fee change.");
    public static readonly Error Stale = Error.Conflict("PracticeLocation.Stale", "The location changed. Reload it before trying again.");
    public static readonly Error Busy = Error.Conflict("PracticeLocation.Busy", "Another location operation is in progress. Try again.");
    public static readonly Error SelfReview = Error.Forbidden("PracticeLocation.SelfReview", "You cannot review your own practice location.");
    public static readonly Error AlreadyReviewed = Error.Conflict("PracticeLocation.AlreadyReviewed", "Only the current pending submission can be reviewed.");
    public static readonly Error RequiredEvidence = Error.Validation("PracticeLocation.Documents", "Provide facility-license evidence and optional work/authorization evidence, with no repeated kinds.", "Documents");
    public static readonly Error ReasonRequired = Error.Validation("PracticeLocation.Reason", "Provide a review reason of 1 to 2000 characters.", "Reason");
    public static readonly Error UnverifiedEvidence = Error.Validation("PracticeLocation.UnverifiedEvidence", "Approval requires verification of the facility license against this address and the doctor's work link.", "EvidenceVerification");
    public static readonly Error InvalidOutcome = Error.Validation("PracticeLocation.Outcome", "Choose approval, rejection or a request for information.", "Outcome");
}
