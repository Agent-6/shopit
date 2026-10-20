using Microsoft.AspNetCore.Http;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Framework.Presentation.Results;
using ShopIt.Identity.Domain.Users;
using Xunit;

namespace ShopIt.ArchitectureTests;

/// <summary>
/// Guards the Identity user slice of #32: expected user failures are <c>Result</c> values whose
/// category decides the status, so a client gets a 4xx it can act on instead of a 500.
/// </summary>
public class IdentityUserResultTests
{
    [Fact]
    public void A_missing_user_maps_to_404()
    {
        Result<Unit, Error> result = UserErrors.NotFound(Guid.NewGuid());

        Assert.Equal(StatusCodes.Status404NotFound, StatusOf(result.ToHttpResult()));
    }

    [Fact]
    public void An_unauthenticated_principal_maps_to_401()
    {
        Result<Unit, Error> result = UserErrors.Unauthenticated();

        Assert.Equal(StatusCodes.Status401Unauthorized, StatusOf(result.ToHttpResult()));
    }

    [Fact]
    public void Identity_validation_failures_map_to_400_and_keep_every_message()
    {
        // What a duplicate email looks like coming back from UserManager.CreateAsync.
        var error = UserErrors.Invalid(
            "User.Create",
            ["Email 'mock@user.com' is already taken.", "Username 'mock@user.com' is already taken."]);

        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal(2, ((ValidationError)error).Errors.Count);
        Assert.Equal(StatusCodes.Status400BadRequest, StatusOf(((Result<Unit, Error>)error).ToHttpResult()));
    }

    [Fact]
    public void A_role_on_the_wrong_tenancy_side_is_validation()
    {
        var error = UserErrors.RoleNotAvailableOnSide(
            "Admin",
            ShopIt.Framework.Domain.Permissions.PermissionMultiTenancySide.Host,
            ShopIt.Framework.Domain.Permissions.PermissionMultiTenancySide.Tenant);

        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal(StatusCodes.Status400BadRequest, StatusOf(((Result<Unit, Error>)error).ToHttpResult()));
    }

    private static int? StatusOf(IResult result) => ((IStatusCodeHttpResult)result).StatusCode;
}
