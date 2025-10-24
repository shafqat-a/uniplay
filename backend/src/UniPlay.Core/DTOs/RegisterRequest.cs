using System.ComponentModel.DataAnnotations;

namespace UniPlay.Core.DTOs;

/// <summary>
/// User registration request
/// </summary>
public class RegisterRequest
{
    /// <summary>
    /// User email address
    /// </summary>
    [Required]
    [EmailAddress]
    [MaxLength(320)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User password (min 8 chars, requires uppercase, lowercase, digit, special char)
    /// </summary>
    [Required]
    [MinLength(8)]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Password confirmation
    /// </summary>
    [Required]
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = string.Empty;
}
