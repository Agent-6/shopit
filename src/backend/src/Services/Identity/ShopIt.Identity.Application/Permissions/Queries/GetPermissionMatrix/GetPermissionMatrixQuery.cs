using ShopIt.Framework.Core.CQRS.Queries;

namespace ShopIt.Identity.Application.Permissions.Queries.GetPermissionMatrix;

/// <summary>
/// Returns the full permission catalog alongside every role and its claims, so permission
/// management UIs can render a roles × permissions matrix in a single request.
/// </summary>
public record GetPermissionMatrixQuery : IQuery<GetPermissionMatrixResult>;
