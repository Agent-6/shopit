using ShopIt.Framework.Core.CQRS.Abstractions;

namespace ShopIt.Framework.Core.CQRS.Commands;

/// <summary>
/// Represents a command that can be executed to perform an action.
/// </summary>
/// <typeparam name="TResult">The type of the result that will be returned when the command is executed.</typeparam>
public interface ICommand<out TResult> : IRequest<TResult>;
