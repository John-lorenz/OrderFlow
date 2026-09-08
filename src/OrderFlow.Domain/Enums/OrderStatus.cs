namespace OrderFlow.Domain.Enums;

public enum OrderStatus
{
    Draft = 0,
    Confirmed = 1,
    Paid = 2,
    Shipped = 3,
    Completed = 4,
    Cancelled = 5
}
