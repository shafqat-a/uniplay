using UniPlay.Core.Enums;

namespace UniPlay.Core.DTOs;

/// <summary>
/// DTO for service connection information
/// </summary>
public class ServiceConnectionDto
{
    public Guid Id { get; set; }
    public ServiceType ServiceType { get; set; }
    public string ServiceAccountId { get; set; } = string.Empty;
    public string? ServiceAccountEmail { get; set; }
    public string? ServiceAccountDisplayName { get; set; }
    public string? ServiceAccountProfileImageUrl { get; set; }
    public ConnectionStatus ConnectionStatus { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime AccessTokenExpiresAt { get; set; }
}

/// <summary>
/// Request to initiate OAuth flow for a music service
/// </summary>
public class InitiateOAuthRequest
{
    public ServiceType ServiceType { get; set; }
    public string? RedirectUri { get; set; }
}

/// <summary>
/// Response with OAuth authorization URL
/// </summary>
public class OAuthUrlResponse
{
    public string AuthorizationUrl { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}

/// <summary>
/// Request to complete OAuth flow after callback
/// </summary>
public class CompleteOAuthRequest
{
    public ServiceType ServiceType { get; set; }
    public string Code { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string? RedirectUri { get; set; }
}

/// <summary>
/// Response after successful OAuth completion
/// </summary>
public class CompleteOAuthResponse
{
    public Guid ConnectionId { get; set; }
    public ServiceConnectionDto Connection { get; set; } = null!;
}
