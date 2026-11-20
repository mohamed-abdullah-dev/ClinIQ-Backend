using ClinIQ.Domain.Common.Results;
using ClinIQ.Domain.Doctors;
using FluentValidation;
using MediatR;

namespace ClinIQ.Application.Features.DoctorProfessionalChanges.Queries;

public sealed record GetOwnVerifiedProfileQuery : IRequest<Result<VerifiedDoctorProfileDto>>;
public sealed record ListProfessionalChangesQuery(bool Admin, int Page, int PageSize, ProfessionalChangeStatus? Status) : IRequest<Result<ProfessionalChangePage>>;
public sealed record GetProfessionalChangeQuery(Guid Id, bool Admin) : IRequest<Result<ProfessionalChangeDetail>>;
public sealed record DownloadProfessionalChangeEvidenceQuery(Guid Id, Guid DocumentId, bool Admin) : IRequest<Result<ChangeEvidenceDownload>>;
public sealed class ListProfessionalChangesQueryValidator : AbstractValidator<ListProfessionalChangesQuery>
{
    public ListProfessionalChangesQueryValidator()
    {
        RuleFor(x => x.Page).InclusiveBetween(1, 100000); RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Status).IsInEnum().When(x => x.Status is not null);
    }
}
public sealed class GetProfessionalChangeQueryValidator : AbstractValidator<GetProfessionalChangeQuery>
{
    public GetProfessionalChangeQueryValidator() => RuleFor(x => x.Id).NotEmpty();
}
public sealed class DownloadProfessionalChangeEvidenceQueryValidator : AbstractValidator<DownloadProfessionalChangeEvidenceQuery>
{
    public DownloadProfessionalChangeEvidenceQueryValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.DocumentId).NotEmpty(); }
}
public sealed class GetOwnVerifiedProfileQueryHandler(IProfessionalChangeService changes) : IRequestHandler<GetOwnVerifiedProfileQuery, Result<VerifiedDoctorProfileDto>>
{
    public Task<Result<VerifiedDoctorProfileDto>> Handle(GetOwnVerifiedProfileQuery request, CancellationToken ct) => changes.GetOwnProfileAsync(ct);
}
public sealed class ListProfessionalChangesQueryHandler(IProfessionalChangeService changes) : IRequestHandler<ListProfessionalChangesQuery, Result<ProfessionalChangePage>>
{
    public Task<Result<ProfessionalChangePage>> Handle(ListProfessionalChangesQuery request, CancellationToken ct) => changes.ListAsync(request.Admin, request.Page, request.PageSize, request.Status, ct);
}
public sealed class GetProfessionalChangeQueryHandler(IProfessionalChangeService changes) : IRequestHandler<GetProfessionalChangeQuery, Result<ProfessionalChangeDetail>>
{
    public Task<Result<ProfessionalChangeDetail>> Handle(GetProfessionalChangeQuery request, CancellationToken ct) => changes.GetAsync(request.Id, request.Admin, ct);
}
public sealed class DownloadProfessionalChangeEvidenceQueryHandler(IProfessionalChangeService changes) : IRequestHandler<DownloadProfessionalChangeEvidenceQuery, Result<ChangeEvidenceDownload>>
{
    public Task<Result<ChangeEvidenceDownload>> Handle(DownloadProfessionalChangeEvidenceQuery request, CancellationToken ct) => changes.DownloadAsync(request.Id, request.DocumentId, request.Admin, ct);
}
