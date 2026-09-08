namespace OrderFlow.Domain.Exceptions;

public class DomainException : Exception
{
    public string Code { get; }

    public DomainException(string code, string message)
        : base(message)
    {
        Code = code;
    }
}

public sealed class EntityNotFoundException : DomainException
{
    public EntityNotFoundException(string entityName, Guid id)
        : base("entity_not_found", $"{entityName} '{id}' was not found.")
    {
    }

    public EntityNotFoundException(string message)
        : base("entity_not_found", message)
    {
    }
}

public sealed class InvalidOrderTransitionException : DomainException
{
    public InvalidOrderTransitionException(string from, string to)
        : base("invalid_order_transition", $"Cannot transition an order from '{from}' to '{to}'.")
    {
    }
}

public sealed class InsufficientStockException : DomainException
{
    public InsufficientStockException(string sku, int available, int requested)
        : base("insufficient_stock", $"Product '{sku}' has {available} units in stock, but {requested} were requested.")
    {
    }
}

public sealed class BusinessRuleException : DomainException
{
    public BusinessRuleException(string code, string message)
        : base(code, message)
    {
    }
}

public sealed class ConflictException : DomainException
{
    public ConflictException(string message)
        : base("conflict", message)
    {
    }
}
