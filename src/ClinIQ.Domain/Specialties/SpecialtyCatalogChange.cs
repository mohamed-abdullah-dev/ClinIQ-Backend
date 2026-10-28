using ClinIQ.Domain.Common;

namespace ClinIQ.Domain.Specialties;

public enum SpecialtyCatalogOperation { Created = 1, Renamed = 2, Deleted = 3 }

public sealed class SpecialtyCatalogChange : Entity
{
    // Historical identity, intentionally not a FK: deletion must retain its audit record.
    public Guid SpecialtyId { get; private set; }
    public Guid ActorUserId { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public SpecialtyCatalogOperation Operation { get; private set; }
    public string? PreviousNameAr { get; private set; }
    public string? PreviousNameEn { get; private set; }
    public string? NameAr { get; private set; }
    public string? NameEn { get; private set; }

    private SpecialtyCatalogChange() { }
    private SpecialtyCatalogChange(Specialty specialty, Guid actor, DateTimeOffset now, SpecialtyCatalogOperation operation,
        string? previousAr, string? previousEn) : base(Guid.NewGuid())
    {
        if (actor == Guid.Empty) throw new ArgumentException("A catalog change requires an actor.", nameof(actor));
        SpecialtyId = specialty.Id; ActorUserId = actor; OccurredAtUtc = now; Operation = operation;
        PreviousNameAr = previousAr; PreviousNameEn = previousEn;
        NameAr = operation == SpecialtyCatalogOperation.Deleted ? null : specialty.NameAr;
        NameEn = operation == SpecialtyCatalogOperation.Deleted ? null : specialty.NameEn;
    }

    public static SpecialtyCatalogChange Created(Specialty specialty, Guid actor, DateTimeOffset now) =>
        new(specialty, actor, now, SpecialtyCatalogOperation.Created, null, null);
    public static SpecialtyCatalogChange Renamed(Specialty specialty, string previousAr, string previousEn, Guid actor, DateTimeOffset now) =>
        new(specialty, actor, now, SpecialtyCatalogOperation.Renamed, previousAr, previousEn);
    public static SpecialtyCatalogChange Deleted(Specialty specialty, Guid actor, DateTimeOffset now) =>
        new(specialty, actor, now, SpecialtyCatalogOperation.Deleted, specialty.NameAr, specialty.NameEn);
}
