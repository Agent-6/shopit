using ShopIt.Framework.Core.CQRS.Queries;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;

namespace ShopIt.Identity.Application.Users.Queries.GetMyPermissions;

/// <summary>
/// Returns the effective permissions of the authenticated caller. The caller's identity
/// is resolved from the token via <see cref="ShopIt.Framework.Domain.Users.ICurrentUser"/>.
/// </summary>
public record GetMyPermissionsQuery : IQuery<Result<GetMyPermissionsResult, Error>>;
