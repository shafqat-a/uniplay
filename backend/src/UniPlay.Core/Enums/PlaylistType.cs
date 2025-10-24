namespace UniPlay.Core.Enums;

/// <summary>
/// Type of playlist (Platform or Service)
/// </summary>
public enum PlaylistType
{
    /// <summary>
    /// UniPlay platform playlist (can contain tracks from multiple services)
    /// </summary>
    Platform,

    /// <summary>
    /// Service playlist (Spotify, Apple Music, etc.)
    /// </summary>
    Service
}
