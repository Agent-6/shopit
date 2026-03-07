using ShopIt.Framework.Core.CQRS.Abstractions;

namespace ShopIt.Framework.Core.CQRS.Commands;

/// <summary>
/// Represents a handler for a specific command type.
/// </summary>
/// <typeparam name="TCommand">The type of the command that this handler can process.</typeparam>
/// <typeparam name="TResult">The type of the result that will be returned when the command is handled.</typeparam>
public interface ICommandHandler<in TCommand, TResult> : IRequestHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>;
