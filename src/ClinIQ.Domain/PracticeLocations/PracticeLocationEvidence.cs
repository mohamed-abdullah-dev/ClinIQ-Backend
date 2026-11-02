namespace ClinIQ.Domain.PracticeLocations;

public enum PracticeLocationEvidenceKind { FacilityLicense = 1, WorkAuthorization = 2 }

public sealed record PracticeLocationEvidenceMetadata(PracticeLocationEvidenceKind Kind, string StorageKey,
    string OriginalFileName, string MediaType, long Length, string Sha256)
{
    public const long MaxLength = 5 * 1024 * 1024;
    public bool IsValid => Enum.IsDefined(Kind) && !string.IsNullOrWhiteSpace(StorageKey) && StorageKey.Length <= 160 &&
        !string.IsNullOrWhiteSpace(OriginalFileName) && OriginalFileName.Length <= 255 &&
        MediaType is "application/pdf" or "image/jpeg" or "image/png" && Length is > 0 and <= MaxLength &&
        Sha256 is { Length: 64 } && Sha256.All(Uri.IsHexDigit);
}
