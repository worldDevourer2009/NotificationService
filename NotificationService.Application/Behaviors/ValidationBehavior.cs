using FluentValidation;
using FluentValidation.Results;
using MediatR;
using NotificationService.Application.Commands;

namespace NotificationService.Application.Behaviors;

public class ValidationBehavior<TRequest, TCommand> : IPipelineBehavior<TRequest, TCommand>
    where TRequest : ICommand<TCommand>
    where TCommand : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TCommand> Handle(TRequest request, RequestHandlerDelegate<TCommand> next,
        CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var validationResult =
                await Task.WhenAll(_validators.Select(x => x.ValidateAsync(context, cancellationToken)));

            if (validationResult.SelectMany(x => x.Errors) 
                    is List<ValidationFailure> failures && failures.Any())
            {
                throw new ValidationException(failures);
            }
        }
        
        return await next(cancellationToken);
    }
}