namespace PaymentService.Interfaces;

public interface IPaymentProviderService
{
    Task<bool> ProcessPaymentAsync(decimal amount);
}