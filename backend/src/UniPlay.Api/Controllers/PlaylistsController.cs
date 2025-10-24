using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniPlay.Core.DTOs;
using UniPlay.Core.Services;

namespace UniPlay.Api.Controllers;

/// <summary>
/// API endpoints for managing playlists
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class PlaylistsController : ControllerBase
{
    private readonly IPlaylistService _playlistService;
    private readonly ILogger<PlaylistsController> _logger;

    public PlaylistsController(
        IPlaylistService playlistService,
        ILogger<PlaylistsController> logger)
    {
        _playlistService = playlistService;
        _logger = logger;
    }

    /// <summary>
    /// Get all playlists for the current user across all services
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<PlaylistWithConnectionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PlaylistWithConnectionDto>>> GetPlaylists()
    {
        var userId = GetCurrentUserId();
        var playlists = await _playlistService.GetUserPlaylistsAsync(userId);
        return Ok(playlists);
    }

    /// <summary>
    /// Get playlists for a specific connection
    /// </summary>
    [HttpGet("connection/{connectionId}")]
    [ProducesResponseType(typeof(List<ServicePlaylistDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ServicePlaylistDto>>> GetConnectionPlaylists(Guid connectionId)
    {
        var userId = GetCurrentUserId();
        var playlists = await _playlistService.GetConnectionPlaylistsAsync(userId, connectionId);
        return Ok(playlists);
    }

    /// <summary>
    /// Sync playlists from a specific connection
    /// </summary>
    [HttpPost("sync")]
    [ProducesResponseType(typeof(SyncPlaylistsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SyncPlaylistsResponse>> SyncPlaylists([FromBody] SyncPlaylistsRequest request)
    {
        try
        {
            var response = await _playlistService.SyncPlaylistsAsync(request.ConnectionId);
            _logger.LogInformation("Synced playlists for connection {ConnectionId}", request.ConnectionId);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (NotSupportedException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Sync playlists for all user connections
    /// </summary>
    [HttpPost("sync/all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SyncAllPlaylists()
    {
        var userId = GetCurrentUserId();
        await _playlistService.SyncAllUserPlaylistsAsync(userId);
        _logger.LogInformation("Synced all playlists for user {UserId}", userId);
        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }
}
