using NotificationService.Domain.Entities;
using NotificationService.Domain.Services;

namespace NotificationService.Application.Queries.Groups;

public record GetGroupsForUserQuery(string? Id) : IQuery<GetGroupsForUserQueryResponse>;

public record GetGroupsForUserQueryResponse(bool Success, List<NotificationGroupEntity>? Groups = null);

public class GetGroupsForUserQueryHandler : IQueryHandler<GetGroupsForUserQuery, GetGroupsForUserQueryResponse>
{
    private readonly INotificationGroupService _notificationGroupService;
    private readonly ILogger<GetGroupsForUserQueryHandler> _logger;

    public GetGroupsForUserQueryHandler(INotificationGroupService notificationGroupService,
        ILogger<GetGroupsForUserQueryHandler> logger)
    {
        _notificationGroupService = notificationGroupService;
        _logger = logger;
    }

    public async Task<GetGroupsForUserQueryResponse> Handle(GetGroupsForUserQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all notification groups for user {Id}", request.Id);

        try
        {
            var result = await _notificationGroupService.GetGroupsForUserAsync(request.Id!, cancellationToken);
            
            if (result == null)
            {
                _logger.LogWarning("No notification groups found for user {Id}", request.Id);
                return new GetGroupsForUserQueryResponse(false);
            }
            
            return new GetGroupsForUserQueryResponse(true, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting all notification groups for user {Id}", request.Id);
            return new GetGroupsForUserQueryResponse(false);
        }
    }
}