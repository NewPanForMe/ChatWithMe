namespace ChatWithMe.Domain.Common;

public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected Entity() => Id = Guid.NewGuid();

    protected Entity(Guid id) => Id = id;

    public Guid Id { get; protected set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
