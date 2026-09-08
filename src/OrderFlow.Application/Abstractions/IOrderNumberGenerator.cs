namespace OrderFlow.Application.Abstractions;

public interface IOrderNumberGenerator
{
    Task<string> NextAsync(CancellationToken cancellationToken = default);
}
