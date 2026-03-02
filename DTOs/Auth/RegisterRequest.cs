using System.ComponentModel.DataAnnotations;

namespace PaymentService.DTOs.Auth;

public class RegisterRequest
{
    [Required, MaxLength(100)]
    public string FirstName { get; set; } = null!;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = null!;

    [Required, EmailAddress, MaxLength(255)]
    public string Email { get; set; } = null!;

    [Required, MinLength(8), MaxLength(64)]
    public string Password { get; set; } = null!;
}
