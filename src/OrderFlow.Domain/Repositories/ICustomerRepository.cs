using OrderFlow.Domain.Entities;

namespace OrderFlow.Domain.Repositories;

public interface ICustomerRepository
{
    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);
    Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Customer?> GetByDocumentAsync(string document, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Customer> Items, int TotalCount)> ListAsync(
        string? search,
        bool? activeOnly,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
