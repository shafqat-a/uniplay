namespace UniPlay.Core.Enums;

/// <summary>
/// Connection status for service connections
/// </summary>
public enum ConnectionStatus
{
    /// <summary>
    /// Connection is active and tokens are valid
    /// </summary>
    Active,

    /// <summary>
    /// Access token has expired and refresh failed
    /// </summary>
    Expired,

    /// <summary>
    /// User revoked access on the service
    /// </summary>
    Revoked,

    /// <summary>
    /// Service API returned persistent error
    /// </summary>
    Error
}
