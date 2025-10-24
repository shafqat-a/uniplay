using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniPlay.Core.DTOs;
using UniPlay.Core.Enums;
using UniPlay.Core.Services;

namespace UniPlay.Api.Controllers;

/// <summary>
/// API endpoints for managing music service connections
/// </summary>
[ApiController]
[Route("api/v1/services")]
[Authorize]
public class ServiceConnectionsController : ControllerBase
{
    private readonly IServiceConnectionService _serviceConnectionService;
    private readonly ILogger<ServiceConnectionsController> _logger;

    public ServiceConnectionsController(
        IServiceConnectionService serviceConnectionService,
        ILogger<ServiceConnectionsController> logger)
    {
        _serviceConnectionService = serviceConnectionService;
        _logger = logger;
    }

    /// <summary>
    /// Get all service connections for the current user
    /// </summary>
    [HttpGet("connections")]
    [ProducesResponseType(typeof(List<ServiceConnectionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ServiceConnectionDto>>> GetConnections()
    {
        var userId = GetCurrentUserId();
        var connections = await _serviceConnectionService.GetUserConnectionsAsync(userId);
        return Ok(connections);
    }

    /// <summary>
    /// Get a specific service connection
    /// </summary>
    [HttpGet("connections/{connectionId}")]
    [ProducesResponseType(typeof(ServiceConnectionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceConnectionDto>> GetConnection(Guid connectionId)
    {
        var userId = GetCurrentUserId();
        var connection = await _serviceConnectionService.GetConnectionAsync(userId, connectionId);

        if (connection == null)
        {
            return NotFound(new { error = "Connection not found" });
        }

        return Ok(connection);
    }

    /// <summary>
    /// Get all connections for a specific service type
    /// </summary>
    [HttpGet("{serviceType}/connections")]
    [ProducesResponseType(typeof(List<ServiceConnectionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ServiceConnectionDto>>> GetConnectionsByService(ServiceType serviceType)
    {
        var userId = GetCurrentUserId();
        var connections = await _serviceConnectionService.GetConnectionsByServiceAsync(userId, serviceType);
        return Ok(connections);
    }

    /// <summary>
    /// Initiate OAuth flow for a music service
    /// </summary>
    [HttpPost("connect")]
    [ProducesResponseType(typeof(OAuthUrlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OAuthUrlResponse>> InitiateOAuth([FromBody] InitiateOAuthRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _serviceConnectionService.InitiateOAuthAsync(userId, request);
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
    /// Complete OAuth flow (callback endpoint)
    /// </summary>
    [HttpPost("callback")]
    [ProducesResponseType(typeof(CompleteOAuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CompleteOAuthResponse>> CompleteOAuth([FromBody] CompleteOAuthRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            var response = await _serviceConnectionService.CompleteOAuthAsync(userId, request);
            _logger.LogInformation("User {UserId} connected {ServiceType} account", userId, request.ServiceType);
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
    /// Disconnect a service
    /// </summary>
    [HttpDelete("connections/{connectionId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DisconnectService(Guid connectionId)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _serviceConnectionService.DisconnectServiceAsync(userId, connectionId);
            _logger.LogInformation("User {UserId} disconnected connection {ConnectionId}", userId, connectionId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Manually refresh access token for a connection
    /// </summary>
    [HttpPost("connections/{connectionId}/refresh")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RefreshToken(Guid connectionId)
    {
        try
        {
            await _serviceConnectionService.RefreshConnectionTokenAsync(connectionId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (NotSupportedException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
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
