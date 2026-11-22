using ClinIQ.Domain.Common.Results;
using FluentValidation;
using MediatR;

namespace ClinIQ.Application.Features.Specialties.Queries.Admin;

public sealed record ListAdminSpecialtiesQuery : IRequest<Result<IReadOnlyList<AdminSpecialtyDto>>>;
public sealed record GetAdminSpecialtyQuery(Guid Id) : IRequest<Result<AdminSpecialtyDto>>;
public sealed class GetAdminSpecialtyQueryValidator : AbstractValidator<GetAdminSpecialtyQuery>
{
    public GetAdminSpecialtyQueryValidator() => RuleFor(x => x.Id).NotEmpty();
}
public sealed class ListAdminSpecialtiesQueryHandler(ISpecialtyAdministrationService specialties)
    : IRequestHandler<ListAdminSpecialtiesQuery, Result<IReadOnlyList<AdminSpecialtyDto>>>
{
    public Task<Result<IReadOnlyList<AdminSpecialtyDto>>> Handle(ListAdminSpecialtiesQuery request, CancellationToken ct) => specialties.ListAsync(ct);
}
public sealed class GetAdminSpecialtyQueryHandler(ISpecialtyAdministrationService specialties)
    : IRequestHandler<GetAdminSpecialtyQuery, Result<AdminSpecialtyDto>>
{
    public Task<Result<AdminSpecialtyDto>> Handle(GetAdminSpecialtyQuery request, CancellationToken ct) => specialties.GetAsync(request.Id, ct);
}
