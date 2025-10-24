using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using UniPlay.Core.DTOs;
using UniPlay.Core.Entities;
using UniPlay.Core.Services;
using UniPlay.Infrastructure.Data;

namespace UniPlay.Infrastructure.Services;

/// <summary>
/// Authentication service implementation
/// </summary>
public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHasher<UserAccount> _passwordHasher;

    public AuthService(
        ApplicationDbContext context,
        IJwtTokenService jwtTokenService,
        IPasswordHasher<UserAccount> passwordHasher)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        // Check if user already exists
        var existingUser = await _context.UserAccounts
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (existingUser != null)
        {
            throw new InvalidOperationException("User with this email already exists");
        }

        // Create new user
        var user = new UserAccount
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            EmailConfirmed = false,
            SecurityStamp = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Hash password
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        // Save to database
        _context.UserAccounts.Add(user);
        await _context.SaveChangesAsync();

        // Generate JWT token
        var token = _jwtTokenService.GenerateToken(user);
        var expiresAt = DateTime.UtcNow.AddMinutes(60); // Should match JwtSettings.ExpirationMinutes

        return new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            UserId = user.Id,
            Email = user.Email
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        // Find user by email
        var user = await _context.UserAccounts
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        // Verify password
        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            throw new UnauthorizedAccessException("Invalid email or password");
        }

        // Update last login timestamp
        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Generate JWT token
        var token = _jwtTokenService.GenerateToken(user);
        var expiresAt = DateTime.UtcNow.AddMinutes(60); // Should match JwtSettings.ExpirationMinutes

        return new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            UserId = user.Id,
            Email = user.Email
        };
    }

    public async Task<UserAccount?> GetUserByIdAsync(Guid userId)
    {
        return await _context.UserAccounts
            .FirstOrDefaultAsync(u => u.Id == userId);
    }
}
