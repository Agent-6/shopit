namespace ShopIt.Framework.Core.CQRS.Abstractions;

public delegate Task<TResponse> RequestHandlerDelegate<TResponse>();
