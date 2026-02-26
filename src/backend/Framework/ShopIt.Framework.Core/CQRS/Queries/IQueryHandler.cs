using ShopIt.Framework.Core.CQRS.Abstractions;

namespace ShopIt.Framework.Core.CQRS.Queries;

/// <summary>
/// Represents a handler for a specific query type.
/// </summary>
/// <typeparam name="TQuery">The type of the query that this handler can process.</typeparam>
/// <typeparam name="TResult">The type of the result that will be returned when the query is handled.</typeparam>
public interface IQueryHandler<in TQuery, TResult> : IRequestHandler<TQuery, TResult>
    where TQuery : IQuery<TResult>;
