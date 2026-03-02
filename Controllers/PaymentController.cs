using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentService.DTOs.Payments;
using PaymentService.Interfaces;

namespace PaymentService.Controllers;

[Authorize]
[Route("api/payments")]
public class PaymentsController(IPaymentService paymentService) : BaseController
{
    [HttpPost]
    public async Task<IActionResult> Create(CreatePaymentRequest request)
    {
        if (!Request.Headers.TryGetValue("Idempotency-Key", out var key))
            return BadRequest(new { error = "Idempotency-Key header is required" });

        var userId = GetUserId();
        var result = await paymentService.CreateAsync(userId, key.ToString(), request);
        
        return result.IsSuccess
            ? CreatedAtAction(nameof(Confirm), new { id = result.Data!.Id }, result.Data)
            : MapError(result);
    }

    [HttpPost("{id:int}/confirm")]
    public async Task<IActionResult> Confirm(int id)
    {
        var userId = GetUserId();
        var result = await paymentService.ConfirmAsync(userId, id);
        
        return result.IsSuccess ? Ok(result.Data) : MapError(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetByOrderId([FromQuery] int orderId)
    {
        var userId = GetUserId();
        var result = await paymentService.GetByOrderIdAsync(userId, orderId);
        return result.IsSuccess ? Ok(result.Data) : MapError(result);
    }
}
