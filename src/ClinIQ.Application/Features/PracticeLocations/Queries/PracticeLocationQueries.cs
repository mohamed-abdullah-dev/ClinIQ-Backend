using ClinIQ.Domain.Common.Results;
using ClinIQ.Domain.PracticeLocations;
using FluentValidation;
using MediatR;

namespace ClinIQ.Application.Features.PracticeLocations.Queries;

public sealed record ListPracticeLocationsQuery(bool Admin, int Page, int PageSize, PracticeLocationStatus? Status) : IRequest<Result<PracticeLocationPage>>;
public sealed record GetPracticeLocationQuery(Guid Id, bool Admin) : IRequest<Result<PracticeLocationDetail>>;
public sealed record ListPracticeLocationSubmissionsQuery(Guid Id, bool Admin, int Page, int PageSize) : IRequest<Result<PracticeLocationSubmissionPage>>;
public sealed record GetPracticeLocationSubmissionQuery(Guid Id, Guid SubmissionId, bool Admin) : IRequest<Result<PracticeLocationSubmissionDto>>;
public sealed record DownloadPracticeLocationEvidenceQuery(Guid Id, Guid SubmissionId, Guid DocumentId, bool Admin) : IRequest<Result<PracticeLocationEvidenceDownload>>;
public sealed class ListPracticeLocationsQueryValidator : AbstractValidator<ListPracticeLocationsQuery>
{
    public ListPracticeLocationsQueryValidator()
    { RuleFor(x => x.Page).InclusiveBetween(1, 100000); RuleFor(x => x.PageSize).InclusiveBetween(1, 100); RuleFor(x => x.Status).IsInEnum().When(x => x.Status is not null); }
}
public sealed class GetPracticeLocationQueryValidator : AbstractValidator<GetPracticeLocationQuery>
{ public GetPracticeLocationQueryValidator() => RuleFor(x => x.Id).NotEmpty(); }
public sealed class ListPracticeLocationSubmissionsQueryValidator : AbstractValidator<ListPracticeLocationSubmissionsQuery>
{
    public ListPracticeLocationSubmissionsQueryValidator()
    { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Page).InclusiveBetween(1, 100000); RuleFor(x => x.PageSize).InclusiveBetween(1, 100); }
}
public sealed class GetPracticeLocationSubmissionQueryValidator : AbstractValidator<GetPracticeLocationSubmissionQuery>
{ public GetPracticeLocationSubmissionQueryValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.SubmissionId).NotEmpty(); } }
public sealed class DownloadPracticeLocationEvidenceQueryValidator : AbstractValidator<DownloadPracticeLocationEvidenceQuery>
{ public DownloadPracticeLocationEvidenceQueryValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.SubmissionId).NotEmpty(); RuleFor(x => x.DocumentId).NotEmpty(); } }
public sealed class ListPracticeLocationsQueryHandler(IPracticeLocationService service) : IRequestHandler<ListPracticeLocationsQuery, Result<PracticeLocationPage>>
{ public Task<Result<PracticeLocationPage>> Handle(ListPracticeLocationsQuery r, CancellationToken ct) => service.ListAsync(r.Admin, r.Page, r.PageSize, r.Status, ct); }
public sealed class GetPracticeLocationQueryHandler(IPracticeLocationService service) : IRequestHandler<GetPracticeLocationQuery, Result<PracticeLocationDetail>>
{ public Task<Result<PracticeLocationDetail>> Handle(GetPracticeLocationQuery r, CancellationToken ct) => service.GetAsync(r.Id, r.Admin, ct); }
public sealed class ListPracticeLocationSubmissionsQueryHandler(IPracticeLocationService service) : IRequestHandler<ListPracticeLocationSubmissionsQuery, Result<PracticeLocationSubmissionPage>>
{ public Task<Result<PracticeLocationSubmissionPage>> Handle(ListPracticeLocationSubmissionsQuery r, CancellationToken ct) => service.ListSubmissionsAsync(r.Id, r.Admin, r.Page, r.PageSize, ct); }
public sealed class GetPracticeLocationSubmissionQueryHandler(IPracticeLocationService service) : IRequestHandler<GetPracticeLocationSubmissionQuery, Result<PracticeLocationSubmissionDto>>
{ public Task<Result<PracticeLocationSubmissionDto>> Handle(GetPracticeLocationSubmissionQuery r, CancellationToken ct) => service.GetSubmissionAsync(r.Id, r.SubmissionId, r.Admin, ct); }
public sealed class DownloadPracticeLocationEvidenceQueryHandler(IPracticeLocationService service) : IRequestHandler<DownloadPracticeLocationEvidenceQuery, Result<PracticeLocationEvidenceDownload>>
{ public Task<Result<PracticeLocationEvidenceDownload>> Handle(DownloadPracticeLocationEvidenceQuery r, CancellationToken ct) => service.DownloadAsync(r.Id, r.SubmissionId, r.DocumentId, r.Admin, ct); }
