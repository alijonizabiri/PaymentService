using PaymentService.Enums;

namespace PaymentService.Entities;

public class Payment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime CreatedAt { get; set; }
    public string? IdempotencyKey { get; set; }
    
    public Order Order { get; set; } = null!;
    public User User { get; set; } = null!;
}
