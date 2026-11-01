using ClinIQ.Domain.Common.Results;

namespace ClinIQ.Domain.Doctors;

public static class DoctorResubmissionErrors
{
    public static readonly Error InvalidLink = Error.Unauthorized("DoctorResubmission.InvalidLink", "The resubmission link is invalid or expired. Request a new link.");
    public static readonly Error NotRejected = Error.Conflict("DoctorResubmission.NotRejected", "Only the current rejected registration can be resubmitted.");
    public static readonly Error Busy = Error.Conflict("DoctorResubmission.Busy", "Another account operation is in progress. Try again.");
}
