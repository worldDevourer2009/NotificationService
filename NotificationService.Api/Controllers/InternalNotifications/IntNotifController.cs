using System.Security.Claims;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NotificationService.Api.Controllers.Tg;
using NotificationService.Application.Commands.InternalNotificationsCommandHandlers;
using NotificationService.Application.Configurations;

namespace NotificationService.Api.Controllers.InternalNotifications;

public record IntNotifDto(string Message, string Title = "Internal Notification");

[ApiController]
[Authorize(Policy = "OnlyServices")]
[Route("api/[controller]")]
public class IntNotifController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly HttpClient _httpClient;

    public IntNotifController(IMediator mediator, IHttpClientFactory httpClientFactory, IOptions<InternalAuthSettings> internalAuthOptions)
    {
        _mediator = mediator;
        var clientName = internalAuthOptions.Value.ServiceClientId!;
        _httpClient = httpClientFactory.CreateClient(clientName);
    }

    [HttpPost("int-notif-send")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendNotification([FromBody] IntNotifDto request)
    {
        var userid = GetCurrentUserId();

        if (userid is null)
        {
            return Unauthorized();
        }
        
        var user = await GetUser(userid);

        if (user is null)
        {
            return Unauthorized();
        }
        
        var command = new InternalNotificationCommand(user.Id, request.Message, request.Title);
        
        var response = await _mediator.Send(command);

        if (!response.Success)
        {
            return BadRequest(response.Message);
        }
        
        return Ok(response.Message);
    }
    
    private async Task<UserDto?> GetUser(string id)
    {
        var response = await _httpClient.GetAsync($"api/user/{id}");
        
        if (response.IsSuccessStatusCode)
        {
            var userJson = await response.Content.ReadAsStringAsync();
            var user = JsonSerializer.Deserialize<UserDto>(userJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (user is null)
            {
                return null;
            }
            
            return new UserDto(user.Id, user.FirstName, user.LastName, user.Email, user.CreatedAt);
        }
        
        return null;
    }

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