using ClinIQ.Domain.Common.Results;
using ClinIQ.Domain.Specialties;
using FluentValidation;
using MediatR;

namespace ClinIQ.Application.Features.Specialties.Commands.CreateSpecialty;

public sealed record CreateSpecialtyCommand(string NameAr, string NameEn) : IRequest<Result<AdminSpecialtyDto>>;
public sealed class CreateSpecialtyCommandValidator : AbstractValidator<CreateSpecialtyCommand>
{
    public CreateSpecialtyCommandValidator()
    {
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(Specialty.NameMaxLength);
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(Specialty.NameMaxLength);
    }
}
public sealed class CreateSpecialtyCommandHandler(ISpecialtyAdministrationService specialties)
    : IRequestHandler<CreateSpecialtyCommand, Result<AdminSpecialtyDto>>
{
    public Task<Result<AdminSpecialtyDto>> Handle(CreateSpecialtyCommand request, CancellationToken ct) =>
        specialties.CreateAsync(request.NameAr, request.NameEn, ct);
}
