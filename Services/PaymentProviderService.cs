using PaymentService.Interfaces;

namespace PaymentService.Services;

public class PaymentProviderService(HttpClient httpClient) : IPaymentProviderService
{
    public async Task<bool> ProcessPaymentAsync(decimal amount)
    {
        var response = await httpClient.PostAsJsonAsync("https://payment-provider/process", new { amount });
        return response.IsSuccessStatusCode;
    }
}