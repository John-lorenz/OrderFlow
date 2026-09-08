using MediatR;
using OrderFlow.Application.Common;
using OrderFlow.Application.Products.Dtos;
using OrderFlow.Domain.Exceptions;
using OrderFlow.Domain.Repositories;

namespace OrderFlow.Application.Products.Queries;

public sealed record GetProductByIdQuery(Guid Id) : IRequest<ProductDto>;

public sealed class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto>
{
    private readonly IProductRepository _products;

    public GetProductByIdQueryHandler(IProductRepository products)
    {
        _products = products;
    }

    public async Task<ProductDto> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _products.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new EntityNotFoundException("Product", request.Id);

        return ProductDto.From(product);
    }
}

public sealed record ListProductsQuery(string? Search, bool? ActiveOnly, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<ProductDto>>;

public sealed class ListProductsQueryHandler : IRequestHandler<ListProductsQuery, PagedResult<ProductDto>>
{
    private readonly IProductRepository _products;

    public ListProductsQueryHandler(IProductRepository products)
    {
        _products = products;
    }

    public async Task<PagedResult<ProductDto>> Handle(ListProductsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var (items, total) = await _products.ListAsync(request.Search, request.ActiveOnly, page, pageSize, cancellationToken);
        return new PagedResult<ProductDto>(items.Select(ProductDto.From).ToList(), total, page, pageSize);
    }
}
