using ClinIQ.Domain.PracticeLocations;

namespace ClinIQ.Application.Features.PracticeLocations;

public sealed record PracticeLocationUpload(PracticeLocationEvidenceKind Kind, string FileName, string ContentType, long Length, Stream Content);
