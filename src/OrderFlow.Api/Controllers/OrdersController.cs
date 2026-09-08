using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderFlow.Application.Orders.Commands;
using OrderFlow.Application.Orders.Dtos;
using OrderFlow.Application.Orders.Queries;
using OrderFlow.Domain.Enums;

namespace OrderFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] OrderStatus? status,
        [FromQuery] Guid? customerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => Ok(await _mediator.Send(new ListOrdersQuery(status, customerId, page, pageSize), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> GetById(Guid id, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetOrderByIdQuery(id), cancellationToken));

    [HttpPost]
    [Authorize(Policy = "OrdersWrite")]
    public async Task<ActionResult<OrderDto>> Create([FromBody] CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var created = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPost("{id:guid}/items")]
    [Authorize(Policy = "OrdersWrite")]
    public async Task<ActionResult<OrderDto>> AddItem(Guid id, [FromBody] AddOrderItemCommand command, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(command with { OrderId = id }, cancellationToken));

    [HttpDelete("{id:guid}/items/{itemId:guid}")]
    [Authorize(Policy = "OrdersWrite")]
    public async Task<ActionResult<OrderDto>> RemoveItem(Guid id, Guid itemId, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new RemoveOrderItemCommand(id, itemId), cancellationToken));

    [HttpPost("{id:guid}/confirm")]
    [Authorize(Policy = "OrdersWrite")]
    public async Task<ActionResult<OrderDto>> Confirm(Guid id, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new ConfirmOrderCommand(id), cancellationToken));

    [HttpPost("{id:guid}/pay")]
    [Authorize(Policy = "OrdersWrite")]
    public async Task<ActionResult<OrderDto>> Pay(Guid id, [FromBody] PayOrderCommand command, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(command with { OrderId = id }, cancellationToken));

    [HttpPost("{id:guid}/ship")]
    [Authorize(Policy = "OrdersWrite")]
    public async Task<ActionResult<OrderDto>> Ship(Guid id, [FromBody] ShipOrderCommand command, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(command with { OrderId = id }, cancellationToken));

    [HttpPost("{id:guid}/complete")]
    [Authorize(Policy = "OrdersWrite")]
    public async Task<ActionResult<OrderDto>> Complete(Guid id, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new CompleteOrderCommand(id), cancellationToken));

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = "OrdersWrite")]
    public async Task<ActionResult<OrderDto>> Cancel(Guid id, [FromBody] CancelOrderCommand command, CancellationToken cancellationToken)
        => Ok(await _mediator.Send(command with { OrderId = id }, cancellationToken));
}
