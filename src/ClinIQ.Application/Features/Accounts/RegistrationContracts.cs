using ClinIQ.Domain.Common.Results;
using ClinIQ.Domain.Doctors;

namespace ClinIQ.Application.Features.Accounts;

public sealed record RegistrationUpload(DoctorEvidenceKind Kind, string FileName, string ContentType, long Length, Stream Content);

public sealed record DoctorRegistrationReceipt(Guid UserId, Guid RegistrationId, string Status,
    bool EmailConfirmationRequired, string EmailDeliveryStatus);

public sealed record PatientRegistrationReceipt(Guid UserId, bool EmailConfirmationRequired, string EmailDeliveryStatus);

public interface IPatientRegistrationService
{
    Task<Result<PatientRegistrationReceipt>> RegisterAsync(Commands.RegisterPatient.RegisterPatientCommand command,
        CancellationToken cancellationToken);
}

public interface IDoctorRegistrationService
{
    Task<Result<DoctorRegistrationReceipt>> RegisterAsync(Commands.RegisterDoctor.RegisterDoctorCommand command,
        CancellationToken cancellationToken);
}

public interface IEmailConfirmationService
{
    Task<Result<Success>> ConfirmAsync(Guid userId, string token, CancellationToken cancellationToken);
    Task ResendAsync(string email, CancellationToken cancellationToken);
}

public static class AccountErrors
{
    public static readonly Error EmailTaken = Error.Conflict("Account.EmailTaken", "This email is already registered.");
    public static readonly Error RegistrationBusy = Error.Conflict("Account.RegistrationBusy", "Registration is busy. Please retry.");
    public static readonly Error InvalidConfirmation = Error.Validation("Account.InvalidConfirmation",
        "The email confirmation link is invalid or expired.");
    public static readonly Error UnknownSpecialty = Error.Validation("DoctorRegistration.UnknownSpecialty",
        "Choose a specialty from the current catalog.", "SpecialtyId");
    public static Error InvalidDocument(string description) =>
        Error.Validation("DoctorRegistration.InvalidDocument", description, "Documents");
}
