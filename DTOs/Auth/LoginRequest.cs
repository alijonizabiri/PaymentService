using System.ComponentModel.DataAnnotations;

namespace PaymentService.DTOs.Auth;

public class LoginRequest
{
    [Required, EmailAddress, MaxLength(255)]
    public string Email { get; set; } = null!;

    [Required, MinLength(8), MaxLength(64)]
    public string Password { get; set; } = null!;
}
