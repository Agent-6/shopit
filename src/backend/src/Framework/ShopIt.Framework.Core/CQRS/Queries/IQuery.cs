using ShopIt.Framework.Core.CQRS.Abstractions;

namespace ShopIt.Framework.Core.CQRS.Queries;

/// <summary>
/// Represents a query that can be executed to retrieve data.
/// </summary>
/// <typeparam name="TResult">The type of the result that will be returned when the query is executed.</typeparam>
public interface IQuery<out TResult> : IRequest<TResult>;
