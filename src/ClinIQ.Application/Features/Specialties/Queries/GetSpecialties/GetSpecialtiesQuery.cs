using ClinIQ.Application.Features.Specialties.Dtos;
using ClinIQ.Domain.Common.Results;
using MediatR;

namespace ClinIQ.Application.Features.Specialties.Queries.GetSpecialties;

public sealed record GetSpecialtiesQuery : IRequest<Result<IReadOnlyList<SpecialtyDto>>>;
