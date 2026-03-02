using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentService.DTOs.Orders;
using PaymentService.Interfaces;

namespace PaymentService.Controllers;

[Authorize]
[Route("api/orders")]
public class OrdersController(IOrderService orderService) : BaseController
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateOrderRequest request)
    {
        var userId = GetUserId();
        var result = await orderService.CreateAsync(userId, request);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data)
            : MapError(result);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = GetUserId();
        var result = await orderService.GetByIdAsync(userId, id);
        return result.IsSuccess ? Ok(result.Data) : MapError(result);
    }
}
