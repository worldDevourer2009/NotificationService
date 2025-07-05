using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NotificationService.Api.Controllers.Groups;

[ApiController]
[Authorize(Policy = "OnlyServices", AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme + ",ServiceScheme")]
[Route("api/[controller]")]
public class NotificationGroupController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<NotificationGroupController> _logger;

    public NotificationGroupController(IMediator mediator, ILogger<NotificationGroupController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }
    
    
}