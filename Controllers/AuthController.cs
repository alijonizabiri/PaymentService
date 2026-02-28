using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentService.DTOs.Auth;
using PaymentService.Enums;
using PaymentService.Interfaces;
using PaymentService.Responses;

namespace PaymentService.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : BaseController
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var result = await authService.RegisterAsync(request);
        return result.IsSuccess
            ? Ok(result.Data)
            : MapError(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await authService.LoginAsync(request);

        return result.IsSuccess
            ? Ok(result.Data)
            : MapError(result);
    }
}
