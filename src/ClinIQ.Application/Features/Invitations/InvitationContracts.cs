using ClinIQ.Domain.Common.Results;
using ClinIQ.Domain.Invitations;

namespace ClinIQ.Application.Features.Invitations;

public sealed record InvitationDto(Guid Id, WorkInvitationKind Kind, Guid? BranchId, Guid? LocationId, string PlaceName,
    string Email, string Terms, decimal? ConsultationFee, string? Currency, string Status, DateTimeOffset ExpiresAtUtc,
    string DeliveryStatus, string Version, Guid? ReplacesInvitationId);
public sealed record InvitationPage(IReadOnlyList<InvitationDto> Items, int TotalCount, int Page, int PageSize);
public sealed record ReceptionistRegistrationReceipt(Guid UserId, bool EmailConfirmationRequired, string EmailDeliveryStatus);
public sealed record AssignmentDto(Guid Id, Guid UserId, Guid? BranchId, Guid? LocationId, Guid SourceInvitationId, bool IsActive,
    Guid? RevokedByUserId, DateTimeOffset? RevokedAtUtc, string? RevocationReason, string Version);
public sealed record AuthorizedDoctorDto(Guid DoctorProfileId, string ProfessionalName);
public sealed record AuthorizedPlaceDto(Guid LocationId, Guid DoctorProfileId, Guid? BranchId, string Name, string Address, decimal ConsultationFee, string Currency, string Version);
public sealed record FeeChangeDto(Guid Id, Guid LocationId, decimal PreviousFee, decimal NewFee, string Currency, string DeliveryStatus);
public interface IWorkInvitationService
{
    Task<Result<InvitationDto>> IssueAsync(WorkInvitationKind kind, Guid? branch, Guid? location, string email, string terms, decimal? fee, string? currency, CancellationToken ct);
    Task<Result<InvitationDto>> ResendAsync(Guid id, string version, CancellationToken ct);
    Task<Result<InvitationDto>> RevokeAsync(Guid id, string version, string reason, CancellationToken ct);
    Task<Result<InvitationPage>> ListAsync(bool received, int page, int pageSize, CancellationToken ct);
    Task<Result<InvitationDto>> GetAsync(Guid id, bool received, CancellationToken ct);
    Task<Result<InvitationDto>> RespondAsync(Guid id, string secret, string version, bool accept, CancellationToken ct);
}
public interface IReceptionistRegistrationService
{
    Task<Result<ReceptionistRegistrationReceipt>> RegisterAsync(Guid invitationId, string secret, string fullName, string email, string password, CancellationToken ct);
}
public interface IWorkforceService
{
    Task<Result<IReadOnlyList<AssignmentDto>>> ListAssignmentsAsync(CancellationToken ct);
    Task<Result<AssignmentDto>> RevokeAssignmentAsync(Guid id, string version, string reason, CancellationToken ct);
    Task<Result<IReadOnlyList<AuthorizedDoctorDto>>> AuthorizedDoctorsAsync(CancellationToken ct);
    Task<Result<IReadOnlyList<AuthorizedPlaceDto>>> AuthorizedPlacesAsync(Guid doctorId, CancellationToken ct);
    Task<Result<FeeChangeDto>> ChangeFeeAsync(Guid locationId, decimal amount, string currency, string version, CancellationToken ct);
    Task<Result<IReadOnlyList<FeeChangeDto>>> FeeHistoryAsync(Guid locationId, CancellationToken ct);
    Task<Result<FeeChangeDto>> RetryFeeNoticeAsync(Guid id, CancellationToken ct);
}
