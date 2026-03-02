using System.ComponentModel.DataAnnotations;

namespace PaymentService.DTOs.Orders;

public class CreateOrderRequest
{
    [Range(0.01, 1_000_000)]
    public decimal Amount { get; set; }
    [Required, StringLength(3, MinimumLength = 3)]
    public string Currency { get; set; } = null!;
}
