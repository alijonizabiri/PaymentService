using System.ComponentModel.DataAnnotations;

namespace PaymentService.DTOs.Auth;

public class RegisterRequest
{
    [Required]
    public string FirstName { get; set; } = null!;
    [Required]
    public string LastName { get; set; } = null!;
    [Required, MaxLength(255)]
    public string Email { get; set; } = null!;
    [Required]
    public string Password { get; set; } = null!;
}
