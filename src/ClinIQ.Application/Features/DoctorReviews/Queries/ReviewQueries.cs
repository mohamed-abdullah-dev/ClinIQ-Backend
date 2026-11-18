using ClinIQ.Domain.Common.Results;
using ClinIQ.Domain.Doctors;
using FluentValidation;
using MediatR;

namespace ClinIQ.Application.Features.DoctorReviews.Queries;

public sealed record ListDoctorRegistrationsQuery(int Page = 1, int PageSize = 20,
    DoctorRegistrationStatus? Status = DoctorRegistrationStatus.PendingReview) : IRequest<Result<DoctorRegistrationPage>>;
public sealed class ListDoctorRegistrationsQueryValidator : AbstractValidator<ListDoctorRegistrationsQuery>
{
    public ListDoctorRegistrationsQueryValidator()
    {
        RuleFor(query => query.Page).InclusiveBetween(1, 1000000);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.Status).Must(status => status is null || Enum.IsDefined(status.Value));
    }
}
public sealed class ListDoctorRegistrationsQueryHandler(IDoctorReviewService service)
    : IRequestHandler<ListDoctorRegistrationsQuery, Result<DoctorRegistrationPage>>
{
    public Task<Result<DoctorRegistrationPage>> Handle(ListDoctorRegistrationsQuery request, CancellationToken ct) =>
        service.ListAsync(request.Page, request.PageSize, request.Status, ct);
}

public sealed record GetDoctorRegistrationQuery(Guid Id) : IRequest<Result<DoctorRegistrationDetail>>;
public sealed class GetDoctorRegistrationQueryValidator : AbstractValidator<GetDoctorRegistrationQuery>
{
    public GetDoctorRegistrationQueryValidator() => RuleFor(query => query.Id).NotEmpty();
}
public sealed class GetDoctorRegistrationQueryHandler(IDoctorReviewService service)
    : IRequestHandler<GetDoctorRegistrationQuery, Result<DoctorRegistrationDetail>>
{
    public Task<Result<DoctorRegistrationDetail>> Handle(GetDoctorRegistrationQuery request, CancellationToken ct) => service.GetAsync(request.Id, ct);
}

public sealed record DownloadDoctorEvidenceQuery(Guid RegistrationId, Guid DocumentId) : IRequest<Result<DoctorEvidenceDownload>>;
public sealed class DownloadDoctorEvidenceQueryValidator : AbstractValidator<DownloadDoctorEvidenceQuery>
{
    public DownloadDoctorEvidenceQueryValidator()
    {
        RuleFor(query => query.RegistrationId).NotEmpty(); RuleFor(query => query.DocumentId).NotEmpty();
    }
}
public sealed class DownloadDoctorEvidenceQueryHandler(IDoctorReviewService service)
    : IRequestHandler<DownloadDoctorEvidenceQuery, Result<DoctorEvidenceDownload>>
{
    public Task<Result<DoctorEvidenceDownload>> Handle(DownloadDoctorEvidenceQuery request, CancellationToken ct) =>
        service.DownloadAsync(request.RegistrationId, request.DocumentId, ct);
}
