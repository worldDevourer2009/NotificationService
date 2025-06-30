using MediatR;

namespace NotificationService.Application.Commands;

public interface ICommand : ICommand<Unit>
{
}

public interface ICommand<out TResponse> : IRequest<TResponse> 
{
}