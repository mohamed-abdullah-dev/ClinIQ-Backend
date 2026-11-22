using ClinIQ.Application.Features.DoctorReviews;
using ClinIQ.Domain.Common.Results;
using FluentValidation;
using MediatR;

namespace ClinIQ.Application.Features.DoctorResubmissions.Queries;

public sealed record GetDoctorResubmissionQuery(string Token) : IRequest<Result<DoctorResubmissionPreview>>;
public sealed class GetDoctorResubmissionQueryHandler(IDoctorResubmissionService service)
    : IRequestHandler<GetDoctorResubmissionQuery, Result<DoctorResubmissionPreview>>
{
    public Task<Result<DoctorResubmissionPreview>> Handle(GetDoctorResubmissionQuery request, CancellationToken ct) => service.GetAsync(request.Token, ct);
}
public sealed record DownloadResubmissionEvidenceQuery(string Token, Guid DocumentId) : IRequest<Result<DoctorEvidenceDownload>>;
public sealed class DownloadResubmissionEvidenceQueryValidator : AbstractValidator<DownloadResubmissionEvidenceQuery>
{
    public DownloadResubmissionEvidenceQueryValidator() => RuleFor(x => x.DocumentId).NotEmpty();
}
public sealed class DownloadResubmissionEvidenceQueryHandler(IDoctorResubmissionService service)
    : IRequestHandler<DownloadResubmissionEvidenceQuery, Result<DoctorEvidenceDownload>>
{
    public Task<Result<DoctorEvidenceDownload>> Handle(DownloadResubmissionEvidenceQuery request, CancellationToken ct) => service.DownloadAsync(request.Token, request.DocumentId, ct);
}
