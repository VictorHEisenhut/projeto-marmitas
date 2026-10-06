using Marmitas.Application.Orders;
using Marmitas.Application.Orders.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Marmitas.Api.Controllers;

[Route("api/pedidos")]
[ApiController]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly ICreateOrderService _createOrderService;

    public OrdersController(ICreateOrderService createOrderService)
    {
        _createOrderService = createOrderService;
    }

    [HttpGet] public IActionResult GetMyOrders() => StatusCode(501);
    [HttpGet("{id}")] public IActionResult GetById(Guid id) => StatusCode(501);

    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(subClaim, out var customerId))
            return Unauthorized();

        var result = await _createOrderService.ExecuteAsync(customerId, request);
        return StatusCode(201, result);
    }

    [HttpPatch("{id}/cancelar")] public IActionResult Cancel(Guid id) => StatusCode(501);
    [HttpGet("{id}/itens")] public IActionResult GetItems(Guid id) => StatusCode(501);
    [HttpGet("{id}/historico-status")] public IActionResult GetStatusHistory(Guid id) => StatusCode(501);
    [HttpPost("{id}/historico-status")] public IActionResult AddStatusHistory(Guid id) => StatusCode(501);
}
