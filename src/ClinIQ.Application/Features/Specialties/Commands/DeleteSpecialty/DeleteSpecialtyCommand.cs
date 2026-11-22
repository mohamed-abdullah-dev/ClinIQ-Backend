using ClinIQ.Domain.Common.Results;
using FluentValidation;
using MediatR;

namespace ClinIQ.Application.Features.Specialties.Commands.DeleteSpecialty;

public sealed record DeleteSpecialtyCommand(Guid Id, string Version) : IRequest<Result<Success>>;
public sealed class DeleteSpecialtyCommandValidator : AbstractValidator<DeleteSpecialtyCommand>
{
    public DeleteSpecialtyCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Version).Must(SpecialtyVersion.IsValid).WithMessage("Provide the current specialty version.");
    }
}
public sealed class DeleteSpecialtyCommandHandler(ISpecialtyAdministrationService specialties)
    : IRequestHandler<DeleteSpecialtyCommand, Result<Success>>
{
    public Task<Result<Success>> Handle(DeleteSpecialtyCommand request, CancellationToken ct) =>
        specialties.DeleteAsync(request.Id, request.Version, ct);
}
