using System.Linq.Expressions;
using ClinIQ.Application.Features.Specialties.Dtos;
using ClinIQ.Domain.Specialties;

namespace ClinIQ.Application.Features.Specialties.Mappers;

public static class SpecialtyMapper
{
    public static readonly Expression<Func<Specialty, SpecialtyDto>> ToDto =
        specialty => new SpecialtyDto(specialty.Id, specialty.NameAr, specialty.NameEn);
}
