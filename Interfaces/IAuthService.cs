using PaymentService.DTOs.Auth;
using PaymentService.Responses;

namespace PaymentService.Interfaces;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request);
}