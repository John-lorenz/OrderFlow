namespace OrderFlow.Domain.Abstractions;

public abstract class AggregateRoot : Entity, IAggregateRoot
{
    protected AggregateRoot()
    {
    }

    protected AggregateRoot(Guid id)
        : base(id)
    {
    }
}
