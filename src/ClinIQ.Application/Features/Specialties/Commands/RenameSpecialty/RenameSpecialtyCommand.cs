using ClinIQ.Domain.Common.Results;
using ClinIQ.Domain.Specialties;
using FluentValidation;
using MediatR;

namespace ClinIQ.Application.Features.Specialties.Commands.RenameSpecialty;

public sealed record RenameSpecialtyCommand(Guid Id, string NameAr, string NameEn, string Version) : IRequest<Result<AdminSpecialtyDto>>;
public sealed class RenameSpecialtyCommandValidator : AbstractValidator<RenameSpecialtyCommand>
{
    public RenameSpecialtyCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.NameAr).NotEmpty().MaximumLength(Specialty.NameMaxLength);
        RuleFor(x => x.NameEn).NotEmpty().MaximumLength(Specialty.NameMaxLength);
        RuleFor(x => x.Version).Must(SpecialtyVersion.IsValid).WithMessage("Provide the current specialty version.");
    }
}
public sealed class RenameSpecialtyCommandHandler(ISpecialtyAdministrationService specialties)
    : IRequestHandler<RenameSpecialtyCommand, Result<AdminSpecialtyDto>>
{
    public Task<Result<AdminSpecialtyDto>> Handle(RenameSpecialtyCommand request, CancellationToken ct) =>
        specialties.RenameAsync(request.Id, request.NameAr, request.NameEn, request.Version, ct);
}
