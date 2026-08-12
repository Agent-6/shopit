using ShopIt.Framework.Core.CQRS.Queries;

namespace ShopIt.Identity.Application.Users.Queries.GetMyPermissions;

/// <summary>
/// Returns the effective permissions of the authenticated caller. The caller's identity
/// is resolved from the token via <see cref="Domain.Users.ICurrentUser"/>.
/// </summary>
public record GetMyPermissionsQuery : IQuery<GetMyPermissionsResult>;
