using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Application.Commands.Groups;
using NotificationService.Application.Queries.Groups;
using TaskHandler.Shared.Notifications.DTOs.Groups;
using TaskHandler.Shared.Notifications.DTOs.Groups.Responses;

namespace NotificationService.Api.Controllers.Groups;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class NotificationGroupController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NotificationGroupController> _logger;

    public NotificationGroupController(IMediator mediator, IHttpClientFactory factory, IConfiguration configuration, ILogger<NotificationGroupController> logger)
    {
        _mediator = mediator;
        _configuration = configuration;
        _logger = logger;
        
        _httpClient = factory.CreateClient("AuthService");
        _configuration = configuration;
        
        var baseUrl = _configuration["AuthSettings:BaseUrl"];
        if (string.IsNullOrEmpty(baseUrl))
        {
            throw new InvalidOperationException("AuthSettings:BaseUrl is not configured.");
        }

        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    #region Get
    
    [HttpGet("get-groups-for-user")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetGroupsForUser([FromQuery] string? userId)
    {
        _logger.LogInformation("Getting all notification groups for user {Id}", userId);
        
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogError("User id is not provided");
            return BadRequest("User id is not provided");
        }
        
        var command = new GetGroupsForUserQuery(userId);
        
        var response = await _mediator.Send(command);
        
        if (!response.Success)
        {
            _logger.LogError("Error while getting notification groups for user {Id}", userId);
            return BadRequest("Can't get notification groups for user");
        }
        
        _logger.LogInformation("Notification groups for user {Id} retrieved successfully", userId);
        return Ok(new  { response.Groups });
    }
    
    [HttpGet("get-groups-for-user-cookie")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetGroupsForUser()
    {
        var userId = GetCurrentUserId();
        
        if (userId is null)
        {
            return Unauthorized();
        }
        
        var command = new GetGroupsForUserQuery(userId);
        
        var response = await _mediator.Send(command);
        
        if (!response.Success)
        {
            return BadRequest("Can't get notification groups for user");
        }
        
        return Ok(new  { response.Groups });
    }
    
    #endregion
    
    #region Post

    [HttpPost("create-group")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateGroup([FromBody] CreateNotificationGroupDto dto)
    {
        _logger.LogInformation("Creating notification group for user {Id}", dto.Name);
        var userId = GetCurrentUserId();

        if (userId is null)
        {
            _logger.LogError("User is not authorized");
            return Unauthorized(new CreateNotificationGroupResponse(false, "User is not authorized"));
        }
        
        var command = new CreateNotificationGroupCommand(dto.Name, userId, dto.Description, dto.Members!);
        
        var response = await _mediator.Send(command);
        
        if (!response.Success)
        {
            _logger.LogError("Error while creating notification group for user {Id}", dto.Name);
            return BadRequest(new CreateNotificationGroupResponse(false, response.Message));
        }
        
        _logger.LogInformation("Notification group for user {Id} created successfully", dto.Name);
        return Ok(new CreateNotificationGroupResponse(true, "Group created successfully"));
    }

    [HttpPost("update-group")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateGroup([FromBody] UpdateGroupDto dto)
    {
        _logger.LogInformation("Updating notification group for user {Id}", dto.GroupId);
        
        var userId = GetCurrentUserId();

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogError("User is not authorized");
            return Unauthorized(new UpdateGroupResponseDto()
            {
                Success = false,
                Message = "User is not authorized"
            });
        }

        var command = new UpdateGroupCommand(dto.GroupId, userId, dto.Name, dto.Description, null!);

        var response = await _mediator.Send(command);

        if (!response.Success)
        {
            _logger.LogError("Error while updating notification group for user {Id}", dto.GroupId);
            return BadRequest(new UpdateGroupResponseDto()
            {
                Success = false,
                Message = response.Message
            });
        }
        
        _logger.LogInformation("Notification group for user {Id} updated successfully", dto.GroupId);
        return Ok(new UpdateGroupResponseDto()
        {
            Success = true,
            Message = "Group updated successfully"
        });
    }

    [HttpPost("remove-user")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RemoveUserFromGroup([FromBody] RemoveUserFromGroupDto dto)
    {
        _logger.LogInformation("Removing user {Id} from notification group {Id}", dto.UserId, dto.GroupId);
        
        var command = new RemoveUserFromGroupCommand(dto.GroupId, dto.UserId);
        var response = await _mediator.Send(command);
        
        if (!response.Success)
        {
            _logger.LogError("Error while removing user {Id} from notification group {Id}", dto.UserId, dto.GroupId);
            return BadRequest(new RemoveUserFromGroupResponseDto
            {
                Success = false,
                Message = response.Message
            });
        }

        _logger.LogInformation("User {Id} removed from notification group {Id}", dto.UserId, dto.GroupId);
        return Ok(new RemoveUserFromGroupResponseDto
        {
            Success = true,
            Message = "User removed from group successfully"
        });
    }
    
    #endregion
    
    #region Delete

    [HttpDelete("delete-group")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteGroup([FromBody] DeleteGroupDto dto)
    {
        _logger.LogInformation("Deleting notification group for user {Id}", dto.GroupId);
        var command = new DeleteGroupCommand(dto.GroupId);

        var response = await _mediator.Send(command);

        if (!response.Success)
        {
            _logger.LogError("Error while deleting notification group for user {Id}", dto.GroupId);
            return BadRequest(new DeleteGroupResponseDto
            {
                Success = false,
                Message = response.Message
            });
        }
        
        _logger.LogInformation("Notification group for user {Id} deleted successfully", dto.GroupId);
        return Ok(new DeleteGroupResponseDto
        {
            Success = true,
            Message = "Group deleted successfully"
        });
    }
    
    #endregion

    private string? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ??
                          User.FindFirst("sub") ?? User.FindFirst("userId");
        
        if (userIdClaim != null)
        {
            return userIdClaim.Value;
        }

        return null;
    }
}