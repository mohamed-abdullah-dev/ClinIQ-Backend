using ClinIQ.Domain.Common.Results;

namespace ClinIQ.Domain.PracticeLifecycle;

public enum OperationalScopeKind { Location = 1, Branch = 2 }
public static class LifecycleErrors
{
    public static readonly Error Unauthorized = Error.Unauthorized("Lifecycle.Unauthorized", "Please log in.");
    public static readonly Error Forbidden = Error.Forbidden("Lifecycle.Forbidden", "The current Web role cannot perform this operation.");
    public static readonly Error NotFound = Error.NotFound("Lifecycle.NotFound", "The authorized location, branch or request is unavailable.");
    public static readonly Error Input = Error.Validation("Lifecycle.Input", "Provide a valid scope, row version and reason up to 2000 characters.");
    public static readonly Error Unavailable = Error.Conflict("Lifecycle.Unavailable", "Only an active approved location or branch can enter this workflow.");
    public static readonly Error Closed = Error.Conflict("Lifecycle.Closed", "The operational scope is closed.");
    public static readonly Error NotPending = Error.Conflict("Lifecycle.NotPending", "Begin departure or closure before finalizing or requesting an exception.");
    public static readonly Error Outstanding = Error.Conflict("Lifecycle.Outstanding", "Resolve every Confirmed or CheckedIn visit, including future visits, before finalizing.");
    public static readonly Error EmergencyRequired = Error.Conflict("Lifecycle.AdminReviewRequired", "Outstanding commitments in a pending departure or closure require Admin-reviewed emergency cancellation.");
    public static readonly Error Stale = Error.Conflict("Lifecycle.StaleVersion", "The scope, request or affected-booking preview changed. Reload before deciding.");
    public static readonly Error Busy = Error.Conflict("Lifecycle.Busy", "The operational scope is changing. Retry shortly.");
    public static readonly Error PendingExists = Error.Conflict("Lifecycle.PendingExists", "This scope already has a pending emergency request.");
    public static readonly Error NoAffectedVisits = Error.Conflict("Lifecycle.NoAffectedVisits", "There are no outstanding visits to cancel exceptionally.");
    public static readonly Error SelfReview = Error.Conflict("Lifecycle.SelfReview", "An Admin cannot review their own request.");
    public static readonly Error State = Error.Conflict("Lifecycle.State", "This operation is incompatible with the current state.");
}
