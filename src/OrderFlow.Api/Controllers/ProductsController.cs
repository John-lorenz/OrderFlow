using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderFlow.Application.Products.Commands;
using OrderFlow.Application.Products.Dtos;
using OrderFlow.Application.Products.Queries;

namespace OrderFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] bool? activeOnly,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => Ok(await _mediator.Send(new ListProductsQuery(search, activeOnly, page, pageSize), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetProductByIdQuery(id), cancellationToken));

    [HttpPost]
    [Authorize(Policy = "CatalogWrite")]
    public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductCommand command, CancellationToken cancellationToken)
    {
        var created = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "CatalogWrite")]
    public async Task<ActionResult<ProductDto>> Update(Guid id, [FromBody] UpdateProductCommand command, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(command with { Id = id }, cancellationToken));

    [HttpPatch("{id:guid}/stock")]
    [Authorize(Policy = "CatalogWrite")]
    public async Task<ActionResult<ProductDto>> AdjustStock(Guid id, [FromBody] AdjustStockCommand command, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(command with { Id = id }, cancellationToken));

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "CatalogWrite")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeactivateProductCommand(id), cancellationToken);
        return NoContent();
    }
}
