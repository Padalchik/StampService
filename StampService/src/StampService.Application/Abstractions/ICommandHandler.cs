using FluentResults;

namespace StampService.Application.Abstractions;

public interface ICommandHandler<TResponse, in TCommand>
    where TCommand : ICommand
{
    Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken);
}
