using ClinIQ.Application.Common.Interfaces;
using ClinIQ.Application.Features.Specialties.Dtos;
using ClinIQ.Application.Features.Specialties.Mappers;
using ClinIQ.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinIQ.Application.Features.Specialties.Queries.GetSpecialties;

public sealed class GetSpecialtiesQueryHandler(IAppDbContext context)
    : IRequestHandler<GetSpecialtiesQuery, Result<IReadOnlyList<SpecialtyDto>>>
{
    public async Task<Result<IReadOnlyList<SpecialtyDto>>> Handle(
        GetSpecialtiesQuery request, CancellationToken cancellationToken)
    {
        var specialties = await context.Specialties
            .OrderBy(specialty => specialty.NormalizedNameEn)
            .ThenBy(specialty => specialty.Id)
            .Select(SpecialtyMapper.ToDto)
            .ToArrayAsync(cancellationToken);

        return Result<IReadOnlyList<SpecialtyDto>>.Success(specialties);
    }
}
