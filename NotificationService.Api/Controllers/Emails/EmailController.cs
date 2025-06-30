using System.Security.Claims;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotificationService.Application.Commands.Emails;
using TaskHandler.Application.DTOs;

namespace NotificationService.Api.Controllers.Emails;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class EmailController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly HttpClient _httpClient;

    public EmailController(IMediator mediator, IHttpClientFactory factory)
    {
        _mediator = mediator;
        _httpClient = factory.CreateClient("AuthService");
        _httpClient.BaseAddress = new Uri("https://localhost:9500/");
    }

    [HttpPost("send")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SendEmail([FromBody] EmailRequestDTO emailRequestDto)
    {
        var userId = GetCurrentUserId();

        if (userId is null)
        {
            return Unauthorized();
        }
        
        var user = await GetUser(userId);

        if (user is null)
        {
            return Unauthorized();       
        }
        
        var command = new SendEmailCommand(
            user.Email,
            emailRequestDto.Subject,
            emailRequestDto.Message,
            emailRequestDto.HtmlMessage,
            null);
        
        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest("Can't send email");
        }

        return Ok("Email sent successfully to " + user.Email + "");
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