using ClinIQ.Domain.Common.Results;
using FluentValidation;
using MediatR;

namespace ClinIQ.Application.Features.Invitations.Queries;

public sealed record ListInvitationsQuery(bool Received, int Page = 1, int PageSize = 20) : IRequest<Result<InvitationPage>>;
public sealed record GetInvitationQuery(Guid Id, bool Received) : IRequest<Result<InvitationDto>>;
public sealed record ListAssignmentsQuery : IRequest<Result<IReadOnlyList<AssignmentDto>>>;
public sealed record AuthorizedDoctorsQuery : IRequest<Result<IReadOnlyList<AuthorizedDoctorDto>>>;
public sealed record AuthorizedDoctorPlacesQuery(Guid DoctorId) : IRequest<Result<IReadOnlyList<AuthorizedPlaceDto>>>;
public sealed record FeeHistoryQuery(Guid LocationId) : IRequest<Result<IReadOnlyList<FeeChangeDto>>>;
public sealed class ListInvitationsValidator : AbstractValidator<ListInvitationsQuery>
{ public ListInvitationsValidator() { RuleFor(x => x.Page).InclusiveBetween(1, 100000); RuleFor(x => x.PageSize).InclusiveBetween(1, 100); } }
public sealed class GetInvitationValidator : AbstractValidator<GetInvitationQuery>
{ public GetInvitationValidator() => RuleFor(x => x.Id).NotEmpty(); }
public sealed class AuthorizedDoctorPlacesValidator : AbstractValidator<AuthorizedDoctorPlacesQuery>
{ public AuthorizedDoctorPlacesValidator() => RuleFor(x => x.DoctorId).NotEmpty(); }
public sealed class FeeHistoryValidator : AbstractValidator<FeeHistoryQuery>
{ public FeeHistoryValidator() => RuleFor(x => x.LocationId).NotEmpty(); }
public sealed class ListInvitationsHandler(IWorkInvitationService s) : IRequestHandler<ListInvitationsQuery, Result<InvitationPage>>
{ public Task<Result<InvitationPage>> Handle(ListInvitationsQuery r, CancellationToken ct) => s.ListAsync(r.Received, r.Page, r.PageSize, ct); }
public sealed class GetInvitationHandler(IWorkInvitationService s) : IRequestHandler<GetInvitationQuery, Result<InvitationDto>>
{ public Task<Result<InvitationDto>> Handle(GetInvitationQuery r, CancellationToken ct) => s.GetAsync(r.Id, r.Received, ct); }
public sealed class ListAssignmentsHandler(IWorkforceService s) : IRequestHandler<ListAssignmentsQuery, Result<IReadOnlyList<AssignmentDto>>>
{ public Task<Result<IReadOnlyList<AssignmentDto>>> Handle(ListAssignmentsQuery r, CancellationToken ct) => s.ListAssignmentsAsync(ct); }
public sealed class AuthorizedDoctorsHandler(IWorkforceService s) : IRequestHandler<AuthorizedDoctorsQuery, Result<IReadOnlyList<AuthorizedDoctorDto>>>
{ public Task<Result<IReadOnlyList<AuthorizedDoctorDto>>> Handle(AuthorizedDoctorsQuery r, CancellationToken ct) => s.AuthorizedDoctorsAsync(ct); }
public sealed class AuthorizedDoctorPlacesHandler(IWorkforceService s) : IRequestHandler<AuthorizedDoctorPlacesQuery, Result<IReadOnlyList<AuthorizedPlaceDto>>>
{ public Task<Result<IReadOnlyList<AuthorizedPlaceDto>>> Handle(AuthorizedDoctorPlacesQuery r, CancellationToken ct) => s.AuthorizedPlacesAsync(r.DoctorId, ct); }
public sealed class FeeHistoryHandler(IWorkforceService s) : IRequestHandler<FeeHistoryQuery, Result<IReadOnlyList<FeeChangeDto>>>
{ public Task<Result<IReadOnlyList<FeeChangeDto>>> Handle(FeeHistoryQuery r, CancellationToken ct) => s.FeeHistoryAsync(r.LocationId, ct); }
