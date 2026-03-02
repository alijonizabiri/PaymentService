using Microsoft.EntityFrameworkCore;
using Npgsql;
using PaymentService.Data;
using PaymentService.DTOs.Payments;
using PaymentService.Entities;
using PaymentService.Enums;
using PaymentService.Interfaces;
using PaymentService.Responses;
using Polly.CircuitBreaker;

namespace PaymentService.Services;

public class PaymentService(AppDbContext dbContext, IPaymentProviderService provider, ILogger<PaymentService> logger) : IPaymentService
{
    public async Task<Result<PaymentResponse>> CreateAsync(int userId, string idempotencyKey, CreatePaymentRequest request)
    {
        logger.LogInformation("CreatePayment requested. UserId={UserId}, OrderId={OrderId}, IdempotencyKey={Key}", userId, request.OrderId, idempotencyKey);

        try
        {
            var order = await dbContext.Orders.FirstOrDefaultAsync(order => order.Id == request.OrderId);
            if (order is null)
            {
                logger.LogWarning("CreatePayment failed: order not found. OrderId={OrderId}", request.OrderId);
                return Result<PaymentResponse>.Fail("Order not found", ErrorType.NotFound);
            }
            
            var existing = await dbContext.Payments.FirstOrDefaultAsync(payment => payment.IdempotencyKey == idempotencyKey);
            if (existing is not null)
            {
                if (existing.OrderId == request.OrderId) 
                    return Result<PaymentResponse>.Ok(MapToResponse(existing));
               
                logger.LogWarning("Idempotency key reuse with different order. Key={Key}, ExistingOrder={Existing}, RequestedOrder={Requested}", idempotencyKey, existing.OrderId, request.OrderId);
                return Result<PaymentResponse>.Fail("Idempotency key reuse with different request parameters", ErrorType.Conflict);

            }

            if (order.UserId != userId)
            {
                logger.LogWarning("CreatePayment forbidden. OrderId={OrderId}, UserId={UserId}, OwnerId={OwnerId}", order.Id, userId, order.UserId);
                return Result<PaymentResponse>.Fail("Access denied", ErrorType.Forbidden);
            }

            if (order.Status != OrderStatus.Created)
            {
                logger.LogWarning("CreatePayment conflict. OrderId={OrderId}, Status={Status}", order.Id, order.Status);
                return Result<PaymentResponse>.Fail("Order is not payable", ErrorType.Conflict);
            }

            var payment = new Payment
            {
                OrderId = order.Id,
                UserId = userId,
                Amount = order.Amount,
                Status = PaymentStatus.Pending,
                IdempotencyKey = idempotencyKey,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Payments.Add(payment);
            await dbContext.SaveChangesAsync();

            logger.LogInformation("Payment created. PaymentId={PaymentId}, OrderId={OrderId}, Status=Pending", payment.Id, payment.OrderId);
            return Result<PaymentResponse>.Ok(MapToResponse(payment));
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            logger.LogWarning("CreatePayment unique violation. IdempotencyKey={Key}", idempotencyKey);
            
            var existing = await dbContext.Payments.FirstAsync(payment => payment.IdempotencyKey == idempotencyKey);
            return Result<PaymentResponse>.Ok(MapToResponse(existing));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CreatePayment failed. UserId={UserId}, OrderId={OrderId}", userId, request.OrderId);
            return Result<PaymentResponse>.Fail("An unexpected error occurred", ErrorType.Unexpected);
        }
    }

    public async Task<Result<PaymentResponse>> ConfirmAsync(int userId, int paymentId)
    {
        logger.LogInformation("ConfirmPayment requested. PaymentId={PaymentId}, UserId={UserId}", paymentId, userId);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted);

        try
        {
            var payment = await dbContext.Payments
                .FromSqlRaw("""
                    SELECT * FROM "Payments"
                    WHERE "Id" = @paymentId
                    FOR UPDATE
                """,
                new NpgsqlParameter("paymentId", paymentId))
                .FirstOrDefaultAsync();

            if (payment is null)
            {
                logger.LogWarning("ConfirmPayment failed: payment not found. PaymentId={PaymentId}", paymentId);
                return Result<PaymentResponse>.Fail("Payment not found", ErrorType.NotFound);
            }

            if (payment.UserId != userId)
            {
                logger.LogWarning("ConfirmPayment forbidden. PaymentId={PaymentId}, UserId={UserId}, OwnerId={OwnerId}", payment.Id, userId, payment.UserId);
                return Result<PaymentResponse>.Fail("Access denied", ErrorType.Forbidden);
            }

            if (payment.Status == PaymentStatus.Successful)
            {
                logger.LogInformation("ConfirmPayment idempotent success. PaymentId={PaymentId}", payment.Id);
                return Result<PaymentResponse>.Ok(MapToResponse(payment));
            }

            if (payment.Status != PaymentStatus.Pending)
            {
                logger.LogWarning("ConfirmPayment conflict. PaymentId={PaymentId}, Status={Status}", payment.Id, payment.Status);
                return Result<PaymentResponse>.Fail("Payment already processed", ErrorType.Conflict);
            }

            bool providerSuccess;
            try
            {
                providerSuccess = await provider.ProcessPaymentAsync(payment.Amount);
            }
            catch (BrokenCircuitException)
            {
                logger.LogWarning("Payment provider unavailable. PaymentId={PaymentId}", payment.Id);
                return Result<PaymentResponse>.Fail("Payment provider unavailable", ErrorType.Unexpected);
            }

            if (!providerSuccess)
            {
                payment.Status = PaymentStatus.Failed;
                await dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                logger.LogWarning("Payment failed by provider. PaymentId={PaymentId}", payment.Id);
                return Result<PaymentResponse>.Fail("Payment failed", ErrorType.Conflict);
            }

            var affected = await dbContext.Orders
                .Where(order => order.Id == payment.OrderId && order.Status == OrderStatus.Created)
                .ExecuteUpdateAsync(s => s.SetProperty(order => order.Status, OrderStatus.Paid));

            if (affected == 0)
            {
                logger.LogWarning("Order already paid. OrderId={OrderId}, PaymentId={PaymentId}", payment.OrderId, payment.Id);
                return Result<PaymentResponse>.Fail("Order already paid", ErrorType.Conflict);
            }

            payment.Status = PaymentStatus.Successful;
            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            logger.LogInformation("Payment confirmed successfully. PaymentId={PaymentId}, OrderId={OrderId}", payment.Id, payment.OrderId);
            return Result<PaymentResponse>.Ok(MapToResponse(payment));
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            logger.LogError(ex, "ConfirmPayment failed. PaymentId={PaymentId}, UserId={UserId}", paymentId, userId);
            return Result<PaymentResponse>.Fail("Unexpected error", ErrorType.Unexpected);
        }
    }

    public async Task<Result<List<PaymentResponse>>> GetByOrderIdAsync(int userId, int orderId)
    {
        logger.LogInformation("GetPaymentsByOrder requested. OrderId={OrderId}, UserId={UserId}", orderId, userId);

        var order = await dbContext.Orders.FirstOrDefaultAsync(order => order.Id == orderId);
        if (order is null)
        {
            logger.LogWarning("Order not found. OrderId={OrderId}", orderId);
            return Result<List<PaymentResponse>>.Fail("Order not found", ErrorType.NotFound);
        }

        if (order.UserId != userId)
        {
            logger.LogWarning("Access denied to payments. OrderId={OrderId}, UserId={UserId}, OwnerId={OwnerId}", orderId, userId, order.UserId);
            return Result<List<PaymentResponse>>.Fail("Access denied", ErrorType.Forbidden);
        }

        var payments = await dbContext.Payments
            .Where(payment => payment.OrderId == orderId)
            .ToListAsync();

        logger.LogInformation("Payments fetched. OrderId={OrderId}, Count={Count}", orderId, payments.Count);
        return Result<List<PaymentResponse>>.Ok(payments.Select(MapToResponse).ToList());
    }

    private static PaymentResponse MapToResponse(Payment payment) => new()
    {
        Id = payment.Id,
        OrderId = payment.OrderId,
        UserId = payment.UserId,
        Amount = payment.Amount,
        Status = payment.Status.ToString(),
        CreatedAt = payment.CreatedAt
    };
}