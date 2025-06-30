using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NotificationService.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class EmailController : ControllerBase
{
    private readonly IMediator _mediator;

    public EmailController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    
}