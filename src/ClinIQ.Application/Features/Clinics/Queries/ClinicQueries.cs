using ClinIQ.Domain.Clinics;
using ClinIQ.Domain.Common.Results;
using FluentValidation;
using MediatR;

namespace ClinIQ.Application.Features.Clinics.Queries;

public sealed record ListClinicApplicationsQuery(bool Admin, int Page, int PageSize, ClinicApplicationStatus? Status) : IRequest<Result<ClinicApplicationPage>>;
public sealed record GetClinicApplicationQuery(Guid Id, bool Admin) : IRequest<Result<ClinicApplicationDetail>>;
public sealed record ListClinicSubmissionsQuery(Guid Id, bool Admin, int Page, int PageSize) : IRequest<Result<ClinicSubmissionPage>>;
public sealed record GetClinicSubmissionQuery(Guid Id, Guid SubmissionId, bool Admin) : IRequest<Result<ClinicSubmissionDto>>;
public sealed record DownloadClinicEvidenceQuery(Guid Id, Guid SubmissionId, Guid DocumentId, bool Admin) : IRequest<Result<ClinicEvidenceDownload>>;
public sealed record ListManagedClinicsQuery(int Page, int PageSize) : IRequest<Result<ManagedClinicPage>>;
public sealed record GetManagedClinicQuery(Guid Id) : IRequest<Result<ManagedClinicDto>>;
public sealed record ListManagedBranchesQuery(Guid Id, int Page, int PageSize) : IRequest<Result<ManagedBranchPage>>;
public sealed record GetManagedBranchQuery(Guid Id, Guid BranchId) : IRequest<Result<ManagedBranchDto>>;
public sealed class ListClinicApplicationsQueryValidator : AbstractValidator<ListClinicApplicationsQuery>
{ public ListClinicApplicationsQueryValidator() { RuleFor(x => x.Page).InclusiveBetween(1,100000); RuleFor(x => x.PageSize).InclusiveBetween(1,100); RuleFor(x => x.Status).IsInEnum().When(x => x.Status is not null); } }
public sealed class GetClinicApplicationQueryValidator : AbstractValidator<GetClinicApplicationQuery>
{ public GetClinicApplicationQueryValidator() => RuleFor(x => x.Id).NotEmpty(); }
public sealed class ListClinicSubmissionsQueryValidator : AbstractValidator<ListClinicSubmissionsQuery>
{ public ListClinicSubmissionsQueryValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Page).InclusiveBetween(1,100000); RuleFor(x => x.PageSize).InclusiveBetween(1,100); } }
public sealed class GetClinicSubmissionQueryValidator : AbstractValidator<GetClinicSubmissionQuery>
{ public GetClinicSubmissionQueryValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.SubmissionId).NotEmpty(); } }
public sealed class DownloadClinicEvidenceQueryValidator : AbstractValidator<DownloadClinicEvidenceQuery>
{ public DownloadClinicEvidenceQueryValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.SubmissionId).NotEmpty(); RuleFor(x => x.DocumentId).NotEmpty(); } }
public sealed class ListManagedClinicsQueryValidator : AbstractValidator<ListManagedClinicsQuery>
{ public ListManagedClinicsQueryValidator() { RuleFor(x => x.Page).InclusiveBetween(1,100000); RuleFor(x => x.PageSize).InclusiveBetween(1,100); } }
public sealed class GetManagedClinicQueryValidator : AbstractValidator<GetManagedClinicQuery>
{ public GetManagedClinicQueryValidator() => RuleFor(x => x.Id).NotEmpty(); }
public sealed class ListManagedBranchesQueryValidator : AbstractValidator<ListManagedBranchesQuery>
{ public ListManagedBranchesQueryValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Page).InclusiveBetween(1,100000); RuleFor(x => x.PageSize).InclusiveBetween(1,100); } }
public sealed class GetManagedBranchQueryValidator : AbstractValidator<GetManagedBranchQuery>
{ public GetManagedBranchQueryValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.BranchId).NotEmpty(); } }
public sealed class ListClinicApplicationsQueryHandler(IClinicService s) : IRequestHandler<ListClinicApplicationsQuery, Result<ClinicApplicationPage>>
{ public Task<Result<ClinicApplicationPage>> Handle(ListClinicApplicationsQuery r,CancellationToken ct)=>s.ListAsync(r.Admin,r.Page,r.PageSize,r.Status,ct); }
public sealed class GetClinicApplicationQueryHandler(IClinicService s) : IRequestHandler<GetClinicApplicationQuery, Result<ClinicApplicationDetail>>
{ public Task<Result<ClinicApplicationDetail>> Handle(GetClinicApplicationQuery r,CancellationToken ct)=>s.GetAsync(r.Id,r.Admin,ct); }
public sealed class ListClinicSubmissionsQueryHandler(IClinicService s) : IRequestHandler<ListClinicSubmissionsQuery, Result<ClinicSubmissionPage>>
{ public Task<Result<ClinicSubmissionPage>> Handle(ListClinicSubmissionsQuery r,CancellationToken ct)=>s.ListSubmissionsAsync(r.Id,r.Admin,r.Page,r.PageSize,ct); }
public sealed class GetClinicSubmissionQueryHandler(IClinicService s) : IRequestHandler<GetClinicSubmissionQuery, Result<ClinicSubmissionDto>>
{ public Task<Result<ClinicSubmissionDto>> Handle(GetClinicSubmissionQuery r,CancellationToken ct)=>s.GetSubmissionAsync(r.Id,r.SubmissionId,r.Admin,ct); }
public sealed class DownloadClinicEvidenceQueryHandler(IClinicService s) : IRequestHandler<DownloadClinicEvidenceQuery, Result<ClinicEvidenceDownload>>
{ public Task<Result<ClinicEvidenceDownload>> Handle(DownloadClinicEvidenceQuery r,CancellationToken ct)=>s.DownloadAsync(r.Id,r.SubmissionId,r.DocumentId,r.Admin,ct); }
public sealed class ListManagedClinicsQueryHandler(IClinicService s) : IRequestHandler<ListManagedClinicsQuery, Result<ManagedClinicPage>>
{ public Task<Result<ManagedClinicPage>> Handle(ListManagedClinicsQuery r,CancellationToken ct)=>s.ListManagedAsync(r.Page,r.PageSize,ct); }
public sealed class GetManagedClinicQueryHandler(IClinicService s) : IRequestHandler<GetManagedClinicQuery, Result<ManagedClinicDto>>
{ public Task<Result<ManagedClinicDto>> Handle(GetManagedClinicQuery r,CancellationToken ct)=>s.GetManagedAsync(r.Id,ct); }
public sealed class ListManagedBranchesQueryHandler(IClinicService s) : IRequestHandler<ListManagedBranchesQuery, Result<ManagedBranchPage>>
{ public Task<Result<ManagedBranchPage>> Handle(ListManagedBranchesQuery r,CancellationToken ct)=>s.ListBranchesAsync(r.Id,r.Page,r.PageSize,ct); }
public sealed class GetManagedBranchQueryHandler(IClinicService s) : IRequestHandler<GetManagedBranchQuery, Result<ManagedBranchDto>>
{ public Task<Result<ManagedBranchDto>> Handle(GetManagedBranchQuery r,CancellationToken ct)=>s.GetBranchAsync(r.Id,r.BranchId,ct); }
