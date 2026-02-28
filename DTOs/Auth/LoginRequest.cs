using System.ComponentModel.DataAnnotations;

namespace PaymentService.DTOs.Auth;

public class LoginRequest
{
    [Required, MaxLength(255)]
    public string Email { get; set; } = null!;
    [Required]
    public string Password { get; set; } = null!;
}
