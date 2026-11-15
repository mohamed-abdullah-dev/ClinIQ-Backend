using ClinIQ.Domain.Common.Results;
using MediatR;

namespace ClinIQ.Application.Features.Patients.Queries.GetOwnProfile;

public sealed record GetOwnPatientProfileQuery : IRequest<Result<PatientProfileDto>>;

public sealed class GetOwnPatientProfileQueryHandler(IPatientProfileService profiles)
    : IRequestHandler<GetOwnPatientProfileQuery, Result<PatientProfileDto>>
{
    public Task<Result<PatientProfileDto>> Handle(GetOwnPatientProfileQuery request, CancellationToken ct) => profiles.GetOwnAsync(ct);
}
