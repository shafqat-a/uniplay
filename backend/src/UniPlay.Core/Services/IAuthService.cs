using UniPlay.Core.DTOs;

namespace UniPlay.Core.Services;

/// <summary>
/// Authentication service for user registration and login
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Register a new user
    /// </summary>
    Task<AuthResponse> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Authenticate user and generate token
    /// </summary>
    Task<AuthResponse> LoginAsync(LoginRequest request);

    /// <summary>
    /// Get user by ID
    /// </summary>
    Task<Entities.UserAccount?> GetUserByIdAsync(Guid userId);
}
