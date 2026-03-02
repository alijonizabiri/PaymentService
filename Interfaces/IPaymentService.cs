using PaymentService.DTOs.Payments;
using PaymentService.Responses;

namespace PaymentService.Interfaces;

public interface IPaymentService
{
    Task<Result<PaymentResponse>> CreateAsync(int userId, string idempotencyKey, CreatePaymentRequest request);
    Task<Result<PaymentResponse>> ConfirmAsync(int userId, int paymentId);
    Task<Result<List<PaymentResponse>>> GetByOrderIdAsync(int userId, int orderId);
}
