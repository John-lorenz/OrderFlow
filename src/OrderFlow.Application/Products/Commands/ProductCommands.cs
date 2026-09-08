using FluentValidation;
using MediatR;
using OrderFlow.Application.Products.Dtos;
using OrderFlow.Domain.Entities;
using OrderFlow.Domain.Exceptions;
using OrderFlow.Domain.Repositories;

namespace OrderFlow.Application.Products.Commands;

public sealed record CreateProductCommand(
    string Sku,
    string Name,
    string? Description,
    decimal UnitPrice,
    int StockQuantity) : IRequest<ProductDto>;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.UnitPrice).GreaterThan(0);
        RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0);
    }
}

public sealed class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IProductRepository _products;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductCommandHandler(IProductRepository products, IUnitOfWork unitOfWork)
    {
        _products = products;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var existing = await _products.GetBySkuAsync(request.Sku, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException($"A product with SKU '{request.Sku}' already exists.");
        }

        var product = Product.Create(request.Sku, request.Name, request.Description, request.UnitPrice, request.StockQuantity);
        await _products.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ProductDto.From(product);
    }
}

public sealed record UpdateProductCommand(Guid Id, string Name, string? Description, decimal UnitPrice) : IRequest<ProductDto>;

public sealed class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ProductDto>
{
    private readonly IProductRepository _products;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductCommandHandler(IProductRepository products, IUnitOfWork unitOfWork)
    {
        _products = products;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _products.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new EntityNotFoundException("Product", request.Id);

        product.Update(request.Name, request.Description, request.UnitPrice);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ProductDto.From(product);
    }
}

public sealed record AdjustStockCommand(Guid Id, int StockQuantity) : IRequest<ProductDto>;

public sealed class AdjustStockCommandHandler : IRequestHandler<AdjustStockCommand, ProductDto>
{
    private readonly IProductRepository _products;
    private readonly IUnitOfWork _unitOfWork;

    public AdjustStockCommandHandler(IProductRepository products, IUnitOfWork unitOfWork)
    {
        _products = products;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductDto> Handle(AdjustStockCommand request, CancellationToken cancellationToken)
    {
        var product = await _products.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new EntityNotFoundException("Product", request.Id);

        product.AdjustStock(request.StockQuantity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ProductDto.From(product);
    }
}

public sealed record DeactivateProductCommand(Guid Id) : IRequest;

public sealed class DeactivateProductCommandHandler : IRequestHandler<DeactivateProductCommand>
{
    private readonly IProductRepository _products;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateProductCommandHandler(IProductRepository products, IUnitOfWork unitOfWork)
    {
        _products = products;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeactivateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _products.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new EntityNotFoundException("Product", request.Id);

        product.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
