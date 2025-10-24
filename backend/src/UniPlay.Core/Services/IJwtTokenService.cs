using UniPlay.Core.Entities;

namespace UniPlay.Core.Services;

/// <summary>
/// Service for generating and validating JWT tokens
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generate JWT token for user
    /// </summary>
    /// <param name="user">User account</param>
    /// <returns>JWT token string</returns>
    string GenerateToken(UserAccount user);

    /// <summary>
    /// Validate JWT token
    /// </summary>
    /// <param name="token">JWT token string</param>
    /// <returns>User ID if valid, null otherwise</returns>
    Guid? ValidateToken(string token);
}
