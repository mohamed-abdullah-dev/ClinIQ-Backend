using System.Text;
using System.Text.RegularExpressions;
using ClinIQ.Domain.Common.Results;
using ClinIQ.Domain.PracticeLocations;

namespace ClinIQ.Domain.Clinics;

public enum ClinicApplicantCapacity { LicensedOperator = 1, AuthorizedRepresentative = 2 }
public enum ManagementAuthorityBasis { RecordedEvidence = 1, SignedDelegation = 2 }
public enum ClinicApplicationStatus { Draft = 1, PendingReview = 2, NeedsInformation = 3, Rejected = 4, Approved = 5 }
public enum ClinicEvidenceKind { FacilityLicense = 1, ApplicantIdentity = 2, ManagementDelegation = 3 }

public sealed record ClinicApplicationData(string ClinicName, string Description, string BranchName, string Address,
    decimal? Latitude, decimal? Longitude, string PublicPhone, string LicensingAuthority, string LicenseNumber,
    string LicensedOperator, ClinicApplicantCapacity ApplicantCapacity, ManagementAuthorityBasis AuthorityBasis)
{
    public Result<ClinicApplicationData> Normalize()
    {
        var place = PracticeLocationDetails.Create(BranchName, Address, Latitude, Longitude);
        if (place.IsError) return Result<ClinicApplicationData>.Failure(place.Errors.Select(x =>
            Error.Validation("ClinicApplication.PhysicalDetails", x.Description, x.Field == "Name" ? "BranchName" : x.Field)));
        var normalized = this with { ClinicName = ClinicName?.Trim() ?? "", Description = Description?.Trim() ?? "",
            BranchName = place.Value.Name, Address = place.Value.Address, PublicPhone = PublicPhone?.Trim() ?? "",
            LicensingAuthority = LicensingAuthority?.Trim() ?? "", LicenseNumber = LicenseNumber?.Trim() ?? "", LicensedOperator = LicensedOperator?.Trim() ?? "" };
        if (normalized.ClinicName.Length is < 1 or > 200 || normalized.Description.Length > 2000 ||
            normalized.LicensingAuthority.Length is < 1 or > 100 || normalized.LicenseNumber.Length is < 1 or > 100 ||
            normalized.LicensedOperator.Length is < 1 or > 200 || !Regex.IsMatch(normalized.PublicPhone, @"^\+?[0-9]{7,15}$") ||
            !Enum.IsDefined(ApplicantCapacity) || !Enum.IsDefined(AuthorityBasis) ||
            LicenseKey(normalized.LicensingAuthority).Length > 100 || LicenseKey(normalized.LicenseNumber).Length > 100) return ClinicErrors.InvalidDetails;
        return normalized;
    }
    public static string LicenseKey(string value) => value.Trim().Normalize(NormalizationForm.FormKC).ToUpperInvariant();
}

public sealed record ClinicEvidenceMetadata(ClinicEvidenceKind Kind, string StorageKey, string OriginalFileName, string MediaType, long Length, string Sha256)
{
    public bool IsValid => Enum.IsDefined(Kind) && !string.IsNullOrWhiteSpace(StorageKey) && StorageKey.Length <= 160 &&
        !string.IsNullOrWhiteSpace(OriginalFileName) && OriginalFileName.Length <= 255 && MediaType is "application/pdf" or "image/jpeg" or "image/png" &&
        Length is > 0 and <= 5 * 1024 * 1024 && Sha256 is { Length: 64 } && Sha256.All(Uri.IsHexDigit);
}

public static class ClinicErrors
{
    public static readonly Error Unauthorized = Error.Unauthorized("Clinic.Unauthorized", "Sign in before continuing.");
    public static readonly Error Forbidden = Error.Forbidden("Clinic.Forbidden", "A confirmed account with permission for this clinic is required.");
    public static readonly Error NotFound = Error.NotFound("Clinic.NotFound", "The clinic, application, submission or document was not found.");
    public static readonly Error InvalidDetails = Error.Validation("ClinicApplication.Details", "Provide valid clinic, branch, license, operator, contact and applicant-capacity details.");
    public static readonly Error RequiredEvidence = Error.Validation("ClinicApplication.Documents", "Provide a facility license, applicant identity and signed delegation when that authority basis is selected, without repeated kinds.", "Documents");
    public static readonly Error DraftLocked = Error.Conflict("ClinicApplication.DraftLocked", "Only a draft, rejected or information-requested application can be edited and submitted.");
    public static readonly Error Stale = Error.Conflict("ClinicApplication.Stale", "The application changed. Reload it before continuing.");
    public static readonly Error Busy = Error.Conflict("Clinic.Busy", "Another clinic operation is in progress. Try again.");
    public static readonly Error SelfReview = Error.Forbidden("ClinicApplication.SelfReview", "An applicant cannot review their own application.");
    public static readonly Error AlreadyReviewed = Error.Conflict("ClinicApplication.AlreadyReviewed", "Only the current pending submission can be reviewed.");
    public static readonly Error ReasonRequired = Error.Validation("ClinicApplication.Reason", "Provide a review reason of 1 to 2000 characters.", "Reason");
    public static readonly Error UnverifiedEvidence = Error.Validation("ClinicApplication.Verification", "Approval requires license-detail, identity, management-authority and independent-corroboration verification.", "Verification");
    public static readonly Error InvalidOutcome = Error.Validation("ClinicApplication.Outcome", "Choose approval, rejection or a request for information.", "Outcome");
    public static readonly Error ExistingFacility = Error.Conflict("Clinic.ExistingFacility", "This facility license already belongs to a registered branch. Use the existing clinic's authorized process instead of creating a duplicate.");
}
