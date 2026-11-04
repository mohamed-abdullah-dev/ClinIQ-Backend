using System.Text.RegularExpressions;
using ClinIQ.Domain.Clinics;
using ClinIQ.Domain.Common.Results;
using ClinIQ.Domain.PracticeLocations;

namespace ClinIQ.Domain.PremisesCorrections;

// A correction never changes affiliation, ownership, fees, or the clinic's shared name.
public sealed record PremisesDetails(string Name, string Address, decimal? Latitude, decimal? Longitude,
    string? PublicPhone = null, string? LicensingAuthority = null, string? LicenseNumber = null)
{
    public Result<PremisesDetails> Normalize(bool branch)
    {
        var physical = PracticeLocationDetails.Create(Name, Address, Latitude, Longitude);
        if (physical.IsError) return Result<PremisesDetails>.Failure(physical.Errors);
        if (!branch && (PublicPhone is not null || LicensingAuthority is not null || LicenseNumber is not null))
            return PremisesCorrectionErrors.InvalidDetails;
        var result = this with
        {
            Name = physical.Value.Name,
            Address = physical.Value.Address,
            PublicPhone = PublicPhone?.Trim(),
            LicensingAuthority = LicensingAuthority?.Trim(),
            LicenseNumber = LicenseNumber?.Trim()
        };
        if (branch && (result.PublicPhone is null || !Regex.IsMatch(result.PublicPhone, @"^\+?[0-9]{7,15}$") ||
            result.LicensingAuthority is null || result.LicensingAuthority.Length is < 1 or > 100 ||
            result.LicenseNumber is null || result.LicenseNumber.Length is < 1 or > 100 ||
            ClinicApplicationData.LicenseKey(result.LicensingAuthority).Length > 100 ||
            ClinicApplicationData.LicenseKey(result.LicenseNumber).Length > 100)) return PremisesCorrectionErrors.InvalidDetails;
        return result;
    }

    public static PremisesDetails From(DoctorPracticeLocation place) => new(place.Name, place.Address, place.Latitude, place.Longitude);
    public static PremisesDetails From(ClinicBranch branch) => new(branch.Name, branch.Address, branch.Latitude, branch.Longitude,
        branch.PublicPhone, branch.LicensingAuthority, branch.LicenseNumber);
}

public enum PremisesCorrectionStatus { PendingReview = 1, Approved = 2, Rejected = 3, NeedsInformation = 4 }

public static class PremisesCorrectionErrors
{
    public static readonly Error Unauthorized = Error.Unauthorized("PremisesCorrection.Unauthorized", "Sign in before continuing.");
    public static readonly Error Forbidden = Error.Forbidden("PremisesCorrection.Forbidden", "A confirmed, eligible account and current permission for this place are required.");
    public static readonly Error NotFound = Error.NotFound("PremisesCorrection.NotFound", "The place, correction or document was not found.");
    public static readonly Error InvalidDetails = Error.Validation("PremisesCorrection.Details", "Provide valid physical details; branch corrections also require phone and license details.", "Details");
    public static readonly Error NotApproved = Error.Conflict("PremisesCorrection.NotApproved", "Only an active, approved place can be corrected.");
    public static readonly Error NoChanges = Error.Validation("PremisesCorrection.NoChanges", "Propose at least one change to the approved physical details.", "Details");
    public static readonly Error Reason = Error.Validation("PremisesCorrection.Reason", "Provide a reason of 1 to 2000 characters.", "Reason");
    public static readonly Error Evidence = Error.Validation("PremisesCorrection.Evidence", "Provide a facility license and optional work/authorization evidence without repeated kinds.", "Documents");
    public static readonly Error PendingExists = Error.Conflict("PremisesCorrection.PendingExists", "This place already has a pending correction.");
    public static readonly Error Stale = Error.Conflict("PremisesCorrection.Stale", "The place or correction changed. Reload before continuing.");
    public static readonly Error Busy = Error.Conflict("PremisesCorrection.Busy", "Another operation for this place is in progress. Try again.");
    public static readonly Error AlreadyReviewed = Error.Conflict("PremisesCorrection.AlreadyReviewed", "This correction already has a decision. Submit a new correction after rejection or a request for information.");
    public static readonly Error SelfReview = Error.Forbidden("PremisesCorrection.SelfReview", "An applicant or manager of this place cannot review its correction.");
    public static readonly Error Verification = Error.Validation("PremisesCorrection.Verification", "Approval requires verification of the same physical premises, license details and the applicant's work/management authority. Relocation requires a new place.", "Verification");
    public static readonly Error Outcome = Error.Validation("PremisesCorrection.Outcome", "Choose approval, rejection or a request for information.", "Outcome");
    public static readonly Error DuplicateLicense = Error.Conflict("PremisesCorrection.DuplicateLicense", "This facility license already belongs to another branch.");
}
