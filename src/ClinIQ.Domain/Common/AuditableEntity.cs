namespace ClinIQ.Domain.Common;

public abstract class AuditableEntity : Entity
{
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? LastModifiedAtUtc { get; set; }


    protected AuditableEntity() { }

    protected AuditableEntity(Guid id) : base(id) { }
}
