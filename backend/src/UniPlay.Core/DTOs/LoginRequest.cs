using System.ComponentModel.DataAnnotations;

namespace UniPlay.Core.DTOs;

/// <summary>
/// User login request
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// User email address
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User password
    /// </summary>
    [Required]
    public string Password { get; set; } = string.Empty;
}
