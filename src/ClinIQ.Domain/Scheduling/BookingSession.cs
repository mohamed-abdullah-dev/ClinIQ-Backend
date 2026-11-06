using ClinIQ.Domain.Common;
using ClinIQ.Domain.Common.Results;

namespace ClinIQ.Domain.Scheduling;

public enum BookingSessionStatus { Published = 1, Cancelled = 2 }
public sealed record SessionWindow(DateOnly Date, TimeOnly StartsLocal, TimeOnly EndsLocal, DateTimeOffset StartsAtUtc, DateTimeOffset EndsAtUtc);

public sealed class BookingSession : AuditableEntity
{
    public Guid LocationId { get; private set; }
    public Guid DoctorProfileId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? OperationalBranchId { get; private set; }
    public Guid? SourceRuleId { get; private set; }
    public byte[]? SourceRuleVersion { get; private set; }
    public DateOnly LocalDate { get; private set; }
    public TimeOnly StartsLocal { get; private set; }
    public TimeOnly EndsLocal { get; private set; }
    public string TimeZoneId { get; private set; } = "Africa/Cairo";
    public DateTimeOffset StartsAtUtc { get; private set; }
    public DateTimeOffset EndsAtUtc { get; private set; }
    public DateTimeOffset BookingClosesAtUtc { get; private set; }
    public int MaxCapacity { get; private set; }
    public int AllocatedCount { get; private set; }
    public int LastQueueNumber { get; private set; }
    public int BookingCutoffMinutes { get; private set; }
    public bool BookingPaused { get; private set; }
    public BookingSessionStatus Status { get; private set; } = BookingSessionStatus.Published;
    public Guid? CancelledByUserId { get; private set; }
    public DateTimeOffset? CancelledAtUtc { get; private set; }
    public string? CancellationReason { get; private set; }
    public byte[] Version { get; private set; } = [];
    private BookingSession() { }
    private BookingSession(Guid id) : base(id) { }

    public static Result<BookingSession> Create(Guid location, Guid doctor, Guid user, SessionWindow window, int capacity, int cutoff, DateTimeOffset now, Guid? ruleId = null, byte[]? ruleVersion = null, Guid? operationalBranchId = null)
    {
        if (location == Guid.Empty || doctor == Guid.Empty || user == Guid.Empty || ruleId == Guid.Empty || operationalBranchId == Guid.Empty ||
            (ruleId.HasValue ? ruleVersion is not { Length: 8 } : ruleVersion is not null))
            throw new ArgumentException("A location owner and valid optional rule provenance are required.");
        var valid = ValidateWindow(window); if (valid.IsError) return Result<BookingSession>.Failure(valid.Errors);
        if (window.StartsAtUtc <= now) return ScheduleErrors.Started;
        if (capacity <= 0) return ScheduleErrors.Capacity;
        var closes = Closing(window.EndsAtUtc, cutoff); if (closes.IsError) return Result<BookingSession>.Failure(closes.Errors);
        return new BookingSession(Guid.NewGuid())
        {
            LocationId = location,
            DoctorProfileId = doctor,
            UserId = user,
            OperationalBranchId = operationalBranchId,
            SourceRuleId = ruleId,
            SourceRuleVersion = ruleVersion?.ToArray(),
            LocalDate = window.Date,
            StartsLocal = window.StartsLocal,
            EndsLocal = window.EndsLocal,
            StartsAtUtc = window.StartsAtUtc,
            EndsAtUtc = window.EndsAtUtc,
            MaxCapacity = capacity,
            BookingCutoffMinutes = cutoff,
            BookingClosesAtUtc = closes.Value
        };
    }
    public Result<Success> ChangeTimes(SessionWindow window, DateTimeOffset now)
    {
        if (Status != BookingSessionStatus.Published || now >= EndsAtUtc) return ScheduleErrors.Ended;
        if (now >= StartsAtUtc || window.StartsAtUtc <= now) return ScheduleErrors.Started;
        if (AllocatedCount > 0) return ScheduleErrors.Allocations;
        if (window.Date != LocalDate) return ScheduleErrors.DateRange;
        var valid = ValidateWindow(window); if (valid.IsError) return valid;
        var closes = Closing(window.EndsAtUtc, BookingCutoffMinutes); if (closes.IsError) return Result<Success>.Failure(closes.Errors);
        StartsLocal = window.StartsLocal; EndsLocal = window.EndsLocal; StartsAtUtc = window.StartsAtUtc; EndsAtUtc = window.EndsAtUtc; BookingClosesAtUtc = closes.Value;
        return Result.Success;
    }
    public Result<Success> ChangeCapacity(int capacity, DateTimeOffset now)
    {
        if (Status != BookingSessionStatus.Published || now >= EndsAtUtc) return ScheduleErrors.Ended;
        if (capacity <= 0 || capacity < AllocatedCount) return ScheduleErrors.Capacity;
        MaxCapacity = capacity; return Result.Success;
    }
    public Result<Success> ChangeCutoff(int cutoff, DateTimeOffset now)
    {
        if (Status != BookingSessionStatus.Published || now >= EndsAtUtc) return ScheduleErrors.Ended;
        var closes = Closing(EndsAtUtc, cutoff); if (closes.IsError) return Result<Success>.Failure(closes.Errors);
        BookingCutoffMinutes = cutoff; BookingClosesAtUtc = closes.Value; return Result.Success;
    }
    public Result<Success> Pause(bool paused, bool resourcesAvailable, DateTimeOffset now)
    {
        if (Status != BookingSessionStatus.Published || now >= EndsAtUtc) return ScheduleErrors.Ended;
        if (!paused && (!resourcesAvailable || now >= BookingClosesAtUtc || AllocatedCount >= MaxCapacity)) return ScheduleErrors.Closed;
        BookingPaused = paused; return Result.Success;
    }
    public bool CanAllocate(DateTimeOffset now, bool resourcesAvailable) => resourcesAvailable && Status == BookingSessionStatus.Published && !BookingPaused && now < BookingClosesAtUtc && AllocatedCount < MaxCapacity;
    // P08 must persist this counter with the actual appointment under the same session transaction lock.
    public Result<Success> Allocate(DateTimeOffset now, bool resourcesAvailable)
    {
        if (!CanAllocate(now, resourcesAvailable)) return ScheduleErrors.Closed;
        AllocatedCount = checked(AllocatedCount + 1); return Result.Success;
    }
    public void ReleaseAllocation()
    {
        if (AllocatedCount <= 0) throw new InvalidOperationException("An allocation can only be released once by its booking transition.");
        AllocatedCount--;
    }
    public int IssueQueueNumber() => LastQueueNumber = checked(LastQueueNumber + 1);
    public Result<Success> Cancel(Guid actor, string reason, DateTimeOffset now)
    {
        if (actor == Guid.Empty) throw new ArgumentException("A cancelling actor is required.");
        if (Status != BookingSessionStatus.Published || now >= EndsAtUtc) return ScheduleErrors.Ended;
        if (AllocatedCount > 0) return ScheduleErrors.Allocations;
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 2000) return ScheduleErrors.Reason;
        Status = BookingSessionStatus.Cancelled; CancelledByUserId = actor; CancelledAtUtc = now; CancellationReason = reason.Trim(); return Result.Success;
    }
    // The appointment workflow resolves uncompleted bookings first and retains completed allocations.
    public Result<Success> CancelForDisruption(Guid actor, string reason, DateTimeOffset now)
    {
        if (actor == Guid.Empty) throw new ArgumentException("A cancelling actor is required.");
        if (Status != BookingSessionStatus.Published || now >= EndsAtUtc) return ScheduleErrors.Ended;
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 2000) return ScheduleErrors.Reason;
        Status = BookingSessionStatus.Cancelled; CancelledByUserId = actor; CancelledAtUtc = now; CancellationReason = reason.Trim(); return Result.Success;
    }
    public static Result<DateTimeOffset> Closing(DateTimeOffset end, int cutoff)
    {
        if (cutoff < 0) return ScheduleErrors.Cutoff;
        try { return end.AddMinutes(-cutoff); } catch (ArgumentOutOfRangeException) { return ScheduleErrors.Cutoff; }
    }
    public Result<bool> RetireForLifecycle(Guid actor, string reason, DateTimeOffset now, int outstanding)
    {
        if (actor == Guid.Empty || outstanding < 0) throw new ArgumentException("An actor and authoritative outstanding count are required.");
        if (outstanding != 0) return PracticeLifecycle.LifecycleErrors.Outstanding;
        if (Status == BookingSessionStatus.Cancelled) return false;
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 2000) return PracticeLifecycle.LifecycleErrors.Input;
        // Completed/not-seen allocations and the issued queue are historical facts, not outstanding visits.
        Status = BookingSessionStatus.Cancelled;
        CancelledByUserId = actor;
        CancelledAtUtc = now;
        CancellationReason = reason.Trim();
        BookingPaused = true;
        return true;
    }
    private static Result<Success> ValidateWindow(SessionWindow w)
    {
        if (w.StartsLocal.Ticks % TimeSpan.TicksPerMinute != 0 || w.EndsLocal.Ticks % TimeSpan.TicksPerMinute != 0 || w.EndsLocal <= w.StartsLocal ||
            w.StartsAtUtc.Offset != TimeSpan.Zero || w.EndsAtUtc.Offset != TimeSpan.Zero || w.EndsAtUtc <= w.StartsAtUtc) return ScheduleErrors.LocalTimes;
        return Result.Success;
    }
}
