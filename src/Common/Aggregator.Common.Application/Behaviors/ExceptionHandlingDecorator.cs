using Microsoft.Extensions.Logging;
using Aggregator.Common.Application.Exceptions;
using Aggregator.Common.Application.Messaging;
using Aggregator.Common.Domain;

namespace Aggregator.Common.Application.Behaviors;

internal static class ExceptionHandlingDecorator
{
    internal sealed class CommandHandler<TCommand>(
        ICommandHandler<TCommand> innerHandler,
        ILogger<CommandHandler<TCommand>> logger)
        : ICommandHandler<TCommand>
        where TCommand : ICommand
    {
        public async Task<Result> Handle(TCommand command, CancellationToken cancellationToken)
        {
            try
            {
                return await innerHandler.Handle(command, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Unhandled exception for {CommandName}", typeof(TCommand).Name);

                throw new AggregatorException(typeof(TCommand).Name, innerException: exception);
            }
        }
    }

    internal sealed class CommandHandlerT<TCommand, TResponse>(
        ICommandHandler<TCommand, TResponse> innerHandler,
        ILogger<CommandHandlerT<TCommand, TResponse>> logger)
        : ICommandHandler<TCommand, TResponse>
        where TCommand : ICommand<TResponse>
    {
        public async Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken)
        {
            try
            {
                return await innerHandler.Handle(command, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Unhandled exception for {RequestName}", typeof(TCommand).Name);

                throw new AggregatorException(typeof(TCommand).Name, innerException: exception);
            }
        }
    }

    internal sealed class QueryHandler<TQuery, TResponse>(
        IQueryHandler<TQuery, TResponse> innerHandler,
        ILogger<QueryHandler<TQuery, TResponse>> logger)
        : IQueryHandler<TQuery, TResponse>
        where TQuery : IQuery<TResponse>
    {
        public async Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken)
        {
            try
            {
                return await innerHandler.Handle(query, cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Unhandled exception for {RequestName}", typeof(TQuery).Name);

                throw new AggregatorException(typeof(TQuery).Name, innerException: exception);
            }
        }
    }
}
