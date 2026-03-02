using PaymentService.DTOs.Orders;
using PaymentService.Responses;

namespace PaymentService.Interfaces;

public interface IOrderService
{
    Task<Result<OrderResponse>> CreateAsync(int userId, CreateOrderRequest request);
    Task<Result<OrderResponse>> GetByIdAsync(int userId, int orderId);
}
