namespace ClinIQ.Domain.Common;

public abstract class Entity
{
    public Guid Id {get; private set;}
    // what is the difference between Guid Id { get; } and Guid Id { get; private set; }?
    // https://chatgpt.com/s/t_6ad3946530b08191b7ca64ca560ee65a
    // https://chatgpt.com/s/t_6ad39493946c8191b09d187d51755a30 ايوا المشروع بتاعنا شغال ب get فقط وزي الفل ومفيش اخطاء ؟ايه الي حصل ؟ ومين الي بيعمل الحاجات الي انت قلت عليها زي Backing Field وكدا ؟ يعني لما النظام وجد ان ال id عنده get فقط ف النظام اتصرف ازاي وحل المشكله دي ازاي ؟
    // https://chatgpt.com/s/t_6ad394ddbb848191a3d2b8a0da58b55e اممم طيب مين الافضل في الطرقتين كدا ؟  مين ال best practice ومين الصح ؟ عادي لو انا عملته get فقط وخليت ال EF يتعامل هو  ولا اني اعمل prviert set علشان يقدر يوصل لل id ؟ اي رايك ك seniero وايه الصح ؟
    // https://chatgpt.com/s/t_6ad39616bcb881918edb9a0314c8b6d0 طب لو كشمروع جديد , مين الافضل  ومين ال best practicea ؟

    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected Entity() {}

    protected Entity(Guid id)
    {
        if(id == Guid.Empty)
            throw new ArgumentException("Entity ID cannot be empty.", nameof(id));

        Id = id;
        // TODO: we need to know and understand why we don't write the assignment like this
        //       Id = id == Guid.Empty ? Guid.NewGuid() : id;

    }

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}
