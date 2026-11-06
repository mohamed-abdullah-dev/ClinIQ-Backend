using ClinIQ.Domain.Common;
using ClinIQ.Domain.Common.Results;

namespace ClinIQ.Domain.Scheduling;

public sealed record ScheduleRuleData(DayOfWeek Day, TimeOnly StartsLocal, TimeOnly EndsLocal, int MaxCapacity,
    int BookingCutoffMinutes, DateOnly EffectiveFrom, DateOnly? EffectiveUntil, bool IsActive = true)
{
    public Result<Success> Validate()
    {
        if (!Enum.IsDefined(Day) || StartsLocal.Ticks % TimeSpan.TicksPerMinute != 0 || EndsLocal.Ticks % TimeSpan.TicksPerMinute != 0 || EndsLocal <= StartsLocal)
            return ScheduleErrors.LocalTimes;
        if (MaxCapacity <= 0) return ScheduleErrors.Capacity;
        if (BookingCutoffMinutes < 0) return ScheduleErrors.Cutoff;
        if (EffectiveUntil < EffectiveFrom) return ScheduleErrors.DateRange;
        var first = EffectiveFrom.DayNumber + ((int)Day - (int)EffectiveFrom.DayOfWeek + 7) % 7;
        if (first > (EffectiveUntil ?? DateOnly.MaxValue).DayNumber) return ScheduleErrors.DateRange;
        return Result.Success;
    }
    public bool OccursOn(DateOnly date) => IsActive && date >= EffectiveFrom && (EffectiveUntil is null || date <= EffectiveUntil) && date.DayOfWeek == Day;
    public bool Overlaps(ScheduleRuleData other)
    {
        if (!IsActive || !other.IsActive || Day != other.Day || StartsLocal >= other.EndsLocal || EndsLocal <= other.StartsLocal) return false;
        var from = EffectiveFrom > other.EffectiveFrom ? EffectiveFrom : other.EffectiveFrom;
        var until = (EffectiveUntil ?? DateOnly.MaxValue) < (other.EffectiveUntil ?? DateOnly.MaxValue) ? EffectiveUntil ?? DateOnly.MaxValue : other.EffectiveUntil ?? DateOnly.MaxValue;
        var first = from.DayNumber + ((int)Day - (int)from.DayOfWeek + 7) % 7;
        return first <= until.DayNumber;
    }
}

public sealed class ScheduleRule : AuditableEntity
{
    public Guid LocationId { get; private set; }
    public Guid DoctorProfileId { get; private set; }
    public Guid UserId { get; private set; }
    public DayOfWeek Day { get; private set; }
    public TimeOnly StartsLocal { get; private set; }
    public TimeOnly EndsLocal { get; private set; }
    public int MaxCapacity { get; private set; }
    public int BookingCutoffMinutes { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveUntil { get; private set; }
    public bool IsActive { get; private set; }
    public byte[] Version { get; private set; } = [];
    public ScheduleRuleData Data => new(Day, StartsLocal, EndsLocal, MaxCapacity, BookingCutoffMinutes, EffectiveFrom, EffectiveUntil, IsActive);
    private ScheduleRule() { }
    private ScheduleRule(Guid id) : base(id) { }
    public static Result<ScheduleRule> Create(Guid location, Guid doctor, Guid user, ScheduleRuleData data)
    {
        if (location == Guid.Empty || doctor == Guid.Empty || user == Guid.Empty) throw new ArgumentException("Schedule ownership is required.");
        var valid = data.Validate(); if (valid.IsError) return Result<ScheduleRule>.Failure(valid.Errors);
        var row = new ScheduleRule(Guid.NewGuid()) { LocationId = location, DoctorProfileId = doctor, UserId = user };
        row.Apply(data); return row;
    }
    public Result<Success> Update(ScheduleRuleData data)
    {
        var valid = data.Validate(); if (valid.IsError) return valid;
        Apply(data); return Result.Success;
    }
    private void Apply(ScheduleRuleData d)
    { Day = d.Day; StartsLocal = d.StartsLocal; EndsLocal = d.EndsLocal; MaxCapacity = d.MaxCapacity; BookingCutoffMinutes = d.BookingCutoffMinutes; EffectiveFrom = d.EffectiveFrom; EffectiveUntil = d.EffectiveUntil; IsActive = d.IsActive; }
}

public static class ScheduleErrors
{
    public static readonly Error Unauthorized = Error.Unauthorized("Schedule.Unauthorized", "Sign in before managing a schedule.");
    public static readonly Error Forbidden = Error.Forbidden("Schedule.Forbidden", "Only the confirmed, professionally eligible responsible doctor can manage this schedule.");
    public static readonly Error NotFound = Error.NotFound("Schedule.NotFound", "The authorized location, rule, exception or session was not found.");
    public static readonly Error Unavailable = Error.Conflict("Schedule.Unavailable", "The doctor/location association is not currently available.");
    public static readonly Error LocalTimes = Error.Validation("Schedule.LocalTimes", "Use minute-precision times with the end after the start on the same day; overnight periods are not supported.", "LocalTimes");
    public static readonly Error Capacity = Error.Validation("Schedule.Capacity", "Capacity must be positive and at least the number of allocations still consumed.", "MaxCapacity");
    public static readonly Error Cutoff = Error.Validation("Schedule.Cutoff", "Use a nonnegative cutoff whose calculated closing instant can be represented.", "BookingCutoffMinutes");
    public static readonly Error DateRange = Error.Validation("Schedule.DateRange", "Provide a valid effective date range containing a matching weekday.", "Dates");
    public static readonly Error Horizon = Error.Validation("Schedule.Horizon", "Use an inclusive publication window between today and today plus 27 days in Africa/Cairo.", "Dates");
    public static readonly Error TimeConversion = Error.Validation("Schedule.TimeConversion", "The local time is invalid or ambiguous in Africa/Cairo. Choose an unambiguous local time.", "LocalTimes");
    public static readonly Error Overlap = Error.Conflict("Schedule.Overlap", "This period overlaps another period for this doctor, including other locations.");
    public static readonly Error Stale = Error.Conflict("Schedule.Stale", "The schedule or session changed. Reload before continuing.");
    public static readonly Error Busy = Error.Conflict("Schedule.Busy", "Another scheduling operation is in progress. Try again.");
    public static readonly Error Started = Error.Conflict("Schedule.Started", "Published times can only be changed before the session starts.");
    public static readonly Error Ended = Error.Conflict("Schedule.Ended", "An ended or cancelled session cannot be edited or resumed.");
    public static readonly Error Allocations = Error.Conflict("Schedule.Allocations", "This operation requires explicit booking cancellation/rebooking handling; existing allocations cannot be moved or cancelled here.");
    public static readonly Error Holiday = Error.Conflict("Schedule.Holiday", "An active holiday exception suppresses this doctor/location/date.");
    public static readonly Error Closed = Error.Conflict("Schedule.Closed", "The session is paused, full, past its cutoff or otherwise unavailable for new reservations.");
    public static readonly Error DuplicateException = Error.Conflict("Schedule.ExceptionExists", "An active exception already exists for this scope and date.");
    public static readonly Error Reason = Error.Validation("Schedule.Reason", "Provide a reason of 1 to 2000 characters.", "Reason");
}
