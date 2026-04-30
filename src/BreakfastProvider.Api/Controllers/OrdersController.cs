using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;
using BreakfastProvider.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BreakfastProvider.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
[Consumes("application/json")]
public class OrdersController(IOrderService orderService) : ControllerBase
{
    [HttpPost]
    [EnableRateLimiting("OrderCreation")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<OrderResponse>> CreateOrder([FromBody] OrderRequest request, CancellationToken cancellationToken)
    {
        var response = await orderService.CreateOrderAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetOrder), new { orderId = response.OrderId }, response);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<OrderResponse>>> ListOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 50) pageSize = 50;

        return await orderService.ListOrdersAsync(page, pageSize, cancellationToken);
    }

    [HttpGet("{orderId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrderResponse>> GetOrder(Guid orderId, CancellationToken cancellationToken)
    {
        var response = await orderService.GetOrderAsync(orderId, cancellationToken);
        if (response == null) return NotFound();
        return response;
    }

    [HttpPatch("{orderId:guid}/status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrderResponse>> UpdateOrderStatus(Guid orderId, [FromBody] UpdateOrderStatusRequest request, CancellationToken cancellationToken)
    {
        var (order, error) = await orderService.UpdateOrderStatusAsync(orderId, request.Status!, cancellationToken);

        if (order == null && error == null) return NotFound();
        if (error != null)
            return Conflict(new ProblemDetails { Title = "Invalid State Transition", Detail = error, Status = 409 });

        return order!;
    }
}
