using Microsoft.AspNetCore.Http;
using ShopIt.Framework.Domain.Errors;
using ShopIt.Framework.Domain.Result;
using ShopIt.Framework.Presentation.Results;
using ShopIt.Identity.Domain.Roles;
using Xunit;

namespace ShopIt.ArchitectureTests;

/// <summary>
/// Guards the Identity role slice of #32: expected role failures are <c>Result</c> values whose
/// category decides the HTTP status, so the endpoint needs no exception filter and a client gets a
/// 4xx it can act on instead of a 500.
/// </summary>
public class IdentityRoleResultTests
{
    [Fact]
    public void A_missing_role_maps_to_404()
    {
        Result<Unit, Error> result = RoleErrors.NotFound(Guid.NewGuid());

        Assert.Equal(StatusCodes.Status404NotFound, StatusOf(result.ToHttpResult()));
    }

    [Fact]
    public void A_role_still_assigned_maps_to_409()
    {
        Result<Unit, Error> result = RoleErrors.InUse("Admin", 3);

        Assert.Equal(StatusCodes.Status409Conflict, StatusOf(result.ToHttpResult()));
    }

    [Fact]
    public void Identity_validation_failures_map_to_400_and_carry_every_message()
    {
        // What a duplicate role name looks like coming back from RoleManager.CreateAsync.
        var error = RoleErrors.Invalid("Role.Create", ["Role name 'Admin' is already taken.", "Second failure."]);
        Result<string, Error> result = error;

        var http = result.ToHttpResult();

        Assert.Equal(StatusCodes.Status400BadRequest, StatusOf(http));
        Assert.IsType<ValidationError>(error);
        Assert.Equal(2, ((ValidationError)error).Errors.Count);
    }

    [Fact]
    public void A_side_mismatch_is_validation()
    {
        var error = RoleErrors.NotAvailableOnSide("HostOnly", ShopIt.Framework.Domain.Permissions.PermissionMultiTenancySide.Tenant);

        Assert.Equal(ErrorType.Validation, error.Type);
    }

    [Fact]
    public void A_successful_result_maps_to_200()
    {
        Result<string, Error> result = "created";

        Assert.Equal(StatusCodes.Status200OK, StatusOf(result.ToHttpResult()));
    }

    private static int? StatusOf(IResult result) => ((IStatusCodeHttpResult)result).StatusCode;
}
