using MediatR;
using OrderFlow.Application.Common;
using OrderFlow.Application.Customers.Dtos;
using OrderFlow.Domain.Exceptions;
using OrderFlow.Domain.Repositories;

namespace OrderFlow.Application.Customers.Queries;

public sealed record GetCustomerByIdQuery(Guid Id) : IRequest<CustomerDto>;

public sealed class GetCustomerByIdQueryHandler : IRequestHandler<GetCustomerByIdQuery, CustomerDto>
{
    private readonly ICustomerRepository _customers;

    public GetCustomerByIdQueryHandler(ICustomerRepository customers)
    {
        _customers = customers;
    }

    public async Task<CustomerDto> Handle(GetCustomerByIdQuery request, CancellationToken cancellationToken)
    {
        var customer = await _customers.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new EntityNotFoundException("Customer", request.Id);

        return CustomerDto.From(customer);
    }
}

public sealed record ListCustomersQuery(string? Search, bool? ActiveOnly, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<CustomerDto>>;

public sealed class ListCustomersQueryHandler : IRequestHandler<ListCustomersQuery, PagedResult<CustomerDto>>
{
    private readonly ICustomerRepository _customers;

    public ListCustomersQueryHandler(ICustomerRepository customers)
    {
        _customers = customers;
    }

    public async Task<PagedResult<CustomerDto>> Handle(ListCustomersQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var (items, total) = await _customers.ListAsync(request.Search, request.ActiveOnly, page, pageSize, cancellationToken);
        return new PagedResult<CustomerDto>(items.Select(CustomerDto.From).ToList(), total, page, pageSize);
    }
}
