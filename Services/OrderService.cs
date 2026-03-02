using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentService.Data;
using PaymentService.DTOs.Orders;
using PaymentService.Entities;
using PaymentService.Enums;
using PaymentService.Interfaces;
using PaymentService.Responses;

namespace PaymentService.Services;

public class OrderService(AppDbContext dbContext, ILogger<OrderService> logger) : IOrderService
{
    // Currency is validated against a predefined whitelist.
    // In a production system, this could be replaced with an enum.
    private static readonly HashSet<string> AllowedCurrencies =
    [
        "USD", "EUR", "RUB", "TJS"
    ];
    
    public async Task<Result<OrderResponse>> CreateAsync(int userId, CreateOrderRequest request)
    {
        logger.LogInformation("Creating order for UserId={UserId}, Amount={Amount}, Currency={Currency}", userId, request.Amount, request.Currency);

        try
        {
            if (!AllowedCurrencies.Contains(request.Currency))
                return Result<OrderResponse>.Fail("Unsupported currency", ErrorType.Validation);
            
            var order = new Order
            {
                UserId = userId,
                Amount = request.Amount,
                Currency = request.Currency,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Orders.Add(order);
            await dbContext.SaveChangesAsync();

            logger.LogInformation("Order created successfully. OrderId={OrderId}, UserId={UserId}", order.Id, userId);
            return Result<OrderResponse>.Ok(MapToResponse(order));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create order for UserId={UserId}", userId);
            return Result<OrderResponse>.Fail("An unexpected error occurred", ErrorType.Unexpected);
        }
    }

    public async Task<Result<OrderResponse>> GetByIdAsync(int userId, int orderId)
    {
        logger.LogInformation("Fetching order OrderId={OrderId} for UserId={UserId}", orderId, userId);

        var order = await dbContext.Orders.FirstOrDefaultAsync(o => o.Id == orderId);

        if (order is null)
        {
            logger.LogWarning("Order not found. OrderId={OrderId}", orderId);
            return Result<OrderResponse>.Fail("Order not found", ErrorType.NotFound);
        }

        if (order.UserId != userId)
        {
            logger.LogWarning("Access denied to OrderId={OrderId}. Requested by UserId={UserId}, OwnerId={OwnerId}", orderId, userId, order.UserId);
            return Result<OrderResponse>.Fail("Access denied", ErrorType.Forbidden);
        }

        logger.LogInformation("Order fetched successfully. OrderId={OrderId}, UserId={UserId}", order.Id, userId);
        return Result<OrderResponse>.Ok(MapToResponse(order));
    }

    private static OrderResponse MapToResponse(Order order) => new()
    {
        Id = order.Id,
        UserId = order.UserId,
        Amount = order.Amount,
        Currency = order.Currency,
        Status = order.Status.ToString(),
        CreatedAt = order.CreatedAt
    };
}