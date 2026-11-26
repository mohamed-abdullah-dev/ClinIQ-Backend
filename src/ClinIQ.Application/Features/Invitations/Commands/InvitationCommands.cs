using ClinIQ.Application.Features.PracticeLocations.Commands;
using ClinIQ.Domain.Common.Results;
using ClinIQ.Domain.Invitations;
using ClinIQ.Domain.PracticeLocations;
using FluentValidation;
using MediatR;

namespace ClinIQ.Application.Features.Invitations.Commands;

public sealed record IssueInvitationCommand(WorkInvitationKind Kind, Guid? BranchId, Guid? LocationId, string Email, string Terms,
    decimal? ConsultationFee = null, string? Currency = null) : IRequest<Result<InvitationDto>>;
public sealed record ResendInvitationCommand(Guid Id, string Version) : IRequest<Result<InvitationDto>>;
public sealed record RevokeInvitationCommand(Guid Id, string Version, string Reason) : IRequest<Result<InvitationDto>>;
public sealed record RespondInvitationCommand(Guid Id, string Secret, string Version, bool Accept) : IRequest<Result<InvitationDto>>;
public sealed record RegisterReceptionistCommand(Guid InvitationId, string Secret, string FullName, string Email, string Password) : IRequest<Result<ReceptionistRegistrationReceipt>>;
public sealed record RevokeReceptionistAssignmentCommand(Guid Id, string Version, string Reason) : IRequest<Result<AssignmentDto>>;
public sealed record ChangeManagedFeeCommand(Guid LocationId, decimal Amount, string Currency, string Version) : IRequest<Result<FeeChangeDto>>;
public sealed record RetryFeeNoticeCommand(Guid Id) : IRequest<Result<FeeChangeDto>>;
public static class InvitationValidation
{
    public static bool Secret(string? value) => value is { Length: 43 } && value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
}
public sealed class IssueInvitationValidator : AbstractValidator<IssueInvitationCommand>
{
    public IssueInvitationValidator()
    {
        RuleFor(x => x.Kind).IsInEnum(); RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Terms).NotEmpty().MaximumLength(2000);
        RuleFor(x => x).Must(x => x.BranchId.HasValue != x.LocationId.HasValue && x.BranchId != Guid.Empty && x.LocationId != Guid.Empty && (x.Kind != WorkInvitationKind.Doctor || x.BranchId.HasValue)).WithMessage("Choose one permitted scope.");
        RuleFor(x => x).Must(x => x.Kind == WorkInvitationKind.Doctor
            ? x.ConsultationFee.HasValue && ConsultationPrice.Create(x.ConsultationFee.Value, x.Currency).IsSuccess
            : x.ConsultationFee is null && x.Currency is null).WithMessage("Doctor invitations require an explicit EGP fee; receptionist invitations have no consultation fee.");
    }
}
public sealed class ResendInvitationValidator : AbstractValidator<ResendInvitationCommand>
{ public ResendInvitationValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Version).Must(PracticeLocationValidation.IsVersion); } }
public sealed class RevokeInvitationValidator : AbstractValidator<RevokeInvitationCommand>
{ public RevokeInvitationValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Version).Must(PracticeLocationValidation.IsVersion); RuleFor(x => x.Reason).NotEmpty().MaximumLength(2000); } }
public sealed class RespondInvitationValidator : AbstractValidator<RespondInvitationCommand>
{ public RespondInvitationValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Version).Must(PracticeLocationValidation.IsVersion); RuleFor(x => x.Secret).Must(InvitationValidation.Secret); } }
public sealed class RegisterReceptionistValidator : AbstractValidator<RegisterReceptionistCommand>
{
    public RegisterReceptionistValidator()
    { RuleFor(x => x.InvitationId).NotEmpty(); RuleFor(x => x.Secret).Must(InvitationValidation.Secret); RuleFor(x => x.FullName).NotEmpty().MaximumLength(200); RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256); RuleFor(x => x.Password).NotEmpty().MaximumLength(128); }
}
public sealed class RevokeAssignmentValidator : AbstractValidator<RevokeReceptionistAssignmentCommand>
{ public RevokeAssignmentValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Version).Must(PracticeLocationValidation.IsVersion); RuleFor(x => x.Reason).NotEmpty().MaximumLength(2000); } }
public sealed class ChangeManagedFeeValidator : AbstractValidator<ChangeManagedFeeCommand>
{ public ChangeManagedFeeValidator() { RuleFor(x => x.LocationId).NotEmpty(); RuleFor(x => x.Version).Must(PracticeLocationValidation.IsVersion); RuleFor(x => x).Must(x => ConsultationPrice.Create(x.Amount, x.Currency).IsSuccess); } }
public sealed class RetryFeeNoticeValidator : AbstractValidator<RetryFeeNoticeCommand>
{ public RetryFeeNoticeValidator() => RuleFor(x => x.Id).NotEmpty(); }
public sealed class IssueInvitationHandler(IWorkInvitationService s) : IRequestHandler<IssueInvitationCommand, Result<InvitationDto>>
{ public Task<Result<InvitationDto>> Handle(IssueInvitationCommand r, CancellationToken ct) => s.IssueAsync(r.Kind, r.BranchId, r.LocationId, r.Email, r.Terms, r.ConsultationFee, r.Currency, ct); }
public sealed class ResendInvitationHandler(IWorkInvitationService s) : IRequestHandler<ResendInvitationCommand, Result<InvitationDto>>
{ public Task<Result<InvitationDto>> Handle(ResendInvitationCommand r, CancellationToken ct) => s.ResendAsync(r.Id, r.Version, ct); }
public sealed class RevokeInvitationHandler(IWorkInvitationService s) : IRequestHandler<RevokeInvitationCommand, Result<InvitationDto>>
{ public Task<Result<InvitationDto>> Handle(RevokeInvitationCommand r, CancellationToken ct) => s.RevokeAsync(r.Id, r.Version, r.Reason, ct); }
public sealed class RespondInvitationHandler(IWorkInvitationService s) : IRequestHandler<RespondInvitationCommand, Result<InvitationDto>>
{ public Task<Result<InvitationDto>> Handle(RespondInvitationCommand r, CancellationToken ct) => s.RespondAsync(r.Id, r.Secret, r.Version, r.Accept, ct); }
public sealed class RegisterReceptionistHandler(IReceptionistRegistrationService s) : IRequestHandler<RegisterReceptionistCommand, Result<ReceptionistRegistrationReceipt>>
{ public Task<Result<ReceptionistRegistrationReceipt>> Handle(RegisterReceptionistCommand r, CancellationToken ct) => s.RegisterAsync(r.InvitationId, r.Secret, r.FullName, r.Email, r.Password, ct); }
public sealed class RevokeAssignmentHandler(IWorkforceService s) : IRequestHandler<RevokeReceptionistAssignmentCommand, Result<AssignmentDto>>
{ public Task<Result<AssignmentDto>> Handle(RevokeReceptionistAssignmentCommand r, CancellationToken ct) => s.RevokeAssignmentAsync(r.Id, r.Version, r.Reason, ct); }
public sealed class ChangeManagedFeeHandler(IWorkforceService s) : IRequestHandler<ChangeManagedFeeCommand, Result<FeeChangeDto>>
{ public Task<Result<FeeChangeDto>> Handle(ChangeManagedFeeCommand r, CancellationToken ct) => s.ChangeFeeAsync(r.LocationId, r.Amount, r.Currency, r.Version, ct); }
public sealed class RetryFeeNoticeHandler(IWorkforceService s) : IRequestHandler<RetryFeeNoticeCommand, Result<FeeChangeDto>>
{ public Task<Result<FeeChangeDto>> Handle(RetryFeeNoticeCommand r, CancellationToken ct) => s.RetryFeeNoticeAsync(r.Id, ct); }
