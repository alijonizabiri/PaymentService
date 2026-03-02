using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using PaymentService.Data;
using PaymentService.DTOs.Auth;
using PaymentService.Entities;
using PaymentService.Enums;
using PaymentService.Interfaces;
using PaymentService.Responses;

namespace PaymentService.Services;

public class AuthService(AppDbContext dbContext, IConfiguration config, ILogger<AuthService> logger) : IAuthService
{
    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        logger.LogInformation("Register attempt for email {Email}", request.Email);

        try
        {
            var exists = await dbContext.Users.AnyAsync(user => user.Email == request.Email);
            if (exists)
            {
                logger.LogWarning("Registration failed: email {Email} already registered", request.Email);
                return Result<AuthResponse>.Fail("Email already registered", ErrorType.Conflict);
            }

            var user = new User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();

            logger.LogInformation("User registered successfully. UserId={UserId}, Email={Email}", user.Id, user.Email);
            return Result<AuthResponse>.Ok(GenerateToken(user));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during registration for email {Email}", request.Email);
            return Result<AuthResponse>.Fail("An unexpected error occurred", ErrorType.Unexpected);
        }
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request)
    {
        logger.LogInformation("Login attempt for email {Email}", request.Email);
        
        var user = await dbContext.Users.FirstOrDefaultAsync(user => user.Email == request.Email);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            logger.LogWarning("Login failed for email {Email}: invalid credentials", request.Email);
            return Result<AuthResponse>.Fail("Invalid credentials", ErrorType.Unauthorized);
        }

        logger.LogInformation("Login successful. UserId={UserId}, Email={Email}", user.Id, user.Email);
        return Result<AuthResponse>.Ok(GenerateToken(user));
    }

    private AuthResponse GenerateToken(User user)
    {
        logger.LogDebug("Generating JWT token for UserId={UserId}", user.Id);

        var secret = config["Jwt:Secret"]!;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiryMinutes = int.Parse(config["Jwt:ExpiryMinutes"]!);
        var expiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds
        );

        logger.LogDebug("JWT token generated for UserId={UserId}, expires at {ExpiresAt}", user.Id, expiresAt);
        return new AuthResponse
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = expiresAt
        };
    }
}