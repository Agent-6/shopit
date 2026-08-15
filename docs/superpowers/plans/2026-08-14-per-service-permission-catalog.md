# Per-Service Permission Catalog + Event-Driven Admin Grants Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Let every microservice own and publish its own permission definitions to the Identity service (persisted in Identity's database) so new permissions reach Identity and the Admin role without redeploying Identity, while removing the "new role → all admins" auto-assignment.

**Architecture:** Permission-definition types (`IPermissionDefinitionProvider`, value objects, fluent context) move from `ShopIt.Identity.Domain` into shared `ShopIt.Framework.Domain` so any service can define a provider without referencing Identity. Each service publishes its catalog via a new `PermissionCatalogPublishedIntegrationEvent` (outbox → Kafka → inbox). Identity upserts definitions into a new `PermissionCatalogEntries` table and grants *newly added* permission names to the Admin role in every tenant. Identity seeds its own catalog in-process at startup (no Kafka round-trip) and its existing role seeding grants Admin the full catalog at boot, so event/role-creation races converge. The `RoleCreatedEventHandler` (new role → all admin users) is deleted.

**Tech Stack:** .NET 10, ASP.NET Core minimal APIs, EF Core 10 + Npgsql, ASP.NET Core Identity (claims-as-permissions), Kafka (Confluent) outbox/inbox, Aspire, xUnit + EF Core InMemory for tests, central package management.

## Global Constraints

- Target framework `net10.0`, `Nullable` + `ImplicitUsings` enabled, `LangVersion latest` (set in `src/backend/Directory.Build.props`).
- Central package management: new packages MUST add a `<PackageVersion>` to `src/backend/Directory.Packages.props`; csproj references use `<PackageReference Include="X" />` with NO `Version` attribute.
- Layering: no service may reference another service's project (Tenancy must NOT reference Identity, and vice versa). Shared permission types live in `ShopIt.Framework.Domain`; the shared event lives in `ShopIt.Framework.Core`.
- Permission claims: a granted permission is a `RoleClaim` row with `ClaimType` = permission name and `ClaimValue` = `"true"` (existing convention — do not change).
- Admin role detection: `NormalizedName == "ADMIN"` queried with `IgnoreQueryFilters()` so host + all tenant Admin roles match (role names are tenant-scoped).
- Identity's role definitions may reference Tenancy-owned permission names only as literal `PermissionName` values (e.g. `new PermissionName("tenant.view")`) — never via a Tenancy type.
- Kafka topic = event type name (class name). Identity's inbox must subscribe by adding `nameof(...)` to `InboxOptions.Topics` in `ShopIt.Identity.API/Program.cs`.
- The permission catalog table is SYSTEM-WIDE: `PermissionCatalogEntry` must NOT implement `ITenantEntity` (no tenant filter).
- Catalog sync is additive: removal semantics are out of scope.
- Work on branch `mfaour/definition-providers` (or an isolated worktree per superpowers:using-git-worktrees). Commit after every green test run.
- Out of scope by explicit spec decision: Authentication and Notifications publish no catalog (they define no permissions today). The pattern to add them later is identical to Tenancy's (Tasks 10).

## File Structure

New files first, then modified. Each file has one responsibility.

**Framework (shared):**
- `src/backend/src/Framework/ShopIt.Framework.Domain/Permissions/IPermissionDefinitionProvider.cs` — provider contract (ABP-style `Define` + `GetGroups`/`GetAll`). *Moved from Identity.Domain.*
- `src/backend/src/Framework/ShopIt.Framework.Domain/Permissions/IPermissionDefinitionContext.cs` — accumulation contract used during `Define`.
- `src/backend/src/Framework/ShopIt.Framework.Domain/Permissions/PermissionDefinitionContext.cs` — default context implementation.
- `src/backend/src/Framework/ShopIt.Framework.Domain/Permissions/PermissionDefinition.cs` — record `(PermissionName Name, string DisplayName, string? Description)`.
- `src/backend/src/Framework/ShopIt.Framework.Domain/Permissions/PermissionGroupDefinition.cs` — mutable group holding a list of `PermissionDefinition`.
- `src/backend/src/Framework/ShopIt.Framework.Domain/Permissions/PermissionName.cs` — string-backed value object (implicit `string`).
- `src/backend/src/Framework/ShopIt.Framework.Domain/Permissions/PermissionGroupName.cs` — string-backed value object.
- `src/backend/src/Framework/ShopIt.Framework.Domain/Permissions/PermissionDefinitionContextExtensions.cs` — fluent `AddGroup(...).AddPermission(...)` chain API.
- `src/backend/src/Framework/ShopIt.Framework.Core/Events/Integration/PermissionCatalogPublishedIntegrationEvent.cs` — event + `PermissionGroupDto`/`PermissionDefinitionDto` wire DTOs.

**Identity Domain:**
- `src/backend/src/Services/Identity/ShopIt.Identity.Domain/Entities/PermissionCatalogEntry.cs` — persisted catalog row (id, group name/display, name, display, description, source service, timestamps).
- Modify `.../Permissions/ShopItIdentityPermissions.cs` — drop `Tenants` class + `Groups.TenantManagement`.
- Modify `.../Permissions/ShopItIdentityPermissionDefinitionProvider.cs` — drop Tenant Management group; add `SourceService = "Identity"` const.
- Modify `.../Roles/ShopItIdentityRoleDefinitionProvider.cs` — tenant permissions become literal `PermissionName` fields.
- Modify `.../Roles/RoleDefinition.cs` — re-document `GrantsAllPermissions`.

**Identity Persistence:**
- `.../Configurations/PermissionCatalogEntryConfiguration.cs` — table mapping, unique index on `Name`.
- Modify `.../Data/ApplicationDbContext.cs` — add `DbSet<PermissionCatalogEntry>`.
- `.../Permissions/DatabasePermissionDefinitionProvider.cs` — DB-backed `IPermissionDefinitionProvider`.
- `.../Permissions/IPermissionCatalogSynchronizer.cs` — sync contract.
- `.../Permissions/PermissionCatalogSynchronizer.cs` — upsert + grant-new-to-admin implementation.
- `.../Permissions/PermissionCatalogPublishedIntegrationEventHandler.cs` — inbox handler.
- Modify `.../DependencyInjection.cs` — register the three new services.
- Migration `.../Migrations/<timestamp>_AddPermissionCatalog.cs` (generated).

**Identity Application / API:**
- Delete `.../ShopIt.Identity.Application/Roles/EventHandlers/RoleCreatedEventHandler.cs`.
- Modify `.../ShopIt.Identity.Application/DependencyInjection.cs` — remove `IPermissionDefinitionProvider` singleton registration.
- Modify `.../ShopIt.Identity.API/Program.cs` — inbox topic, `SeedPermissionCatalog` startup step, `using` updates.

**Tenancy:**
- `src/backend/src/Services/Tenancy/ShopIt.Tenancy.Domain/Permissions/ShopItTenancyPermissions.cs` — canonical `PermissionName` constants + `TenantManagement` group name.
- `.../Permissions/ShopItTenancyPermissionDefinitionProvider.cs` — the 5 `tenant.*` permissions.
- Modify `.../ShopIt.Tenancy.Application/DependencyInjection.cs` — register the provider.
- Modify `.../ShopIt.Tenancy.Presentation/Tenants/TenantsModule.cs` — `using` swap.
- Delete `.../ShopIt.Tenancy.Presentation/Authorization/ShopItTenancyPermissions.cs` (string constants superseded).
- Modify `.../ShopIt.Tenancy.API/Program.cs` — publish catalog to outbox after migrations.

**Tests (new):**
- `src/backend/tests/ShopIt.Framework.Tests/` — xUnit tests for framework permission types + event round-trip.
- `src/backend/tests/ShopIt.Identity.Tests/` — xUnit tests for provider, synchronizer, handler, EF model.

---

### Task 1: Test infrastructure

**Files:**
- Modify: `src/backend/Directory.Packages.props`
- Create: `src/backend/tests/ShopIt.Framework.Tests/ShopIt.Framework.Tests.csproj`
- Create: `src/backend/tests/ShopIt.Framework.Tests/Permissions/PermissionNameTests.cs`
- Create: `src/backend/tests/ShopIt.Identity.Tests/ShopIt.Identity.Tests.csproj`
- Create: `src/backend/tests/ShopIt.Identity.Tests/Permissions/SmokeTests.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: two runnable xUnit projects; the framework project references `ShopIt.Framework.Domain`; the identity project references `ShopIt.Identity.Persistence` (+ transitively Domain/Framework) and will host later tasks' tests.

- [ ] **Step 1: Add central package versions**

Append to `src/backend/Directory.Packages.props` (inside the existing `<ItemGroup>`):

```xml
    <PackageVersion Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.0.10" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageVersion Include="xunit" Version="2.9.3" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="3.1.1" />
```

- [ ] **Step 2: Create the framework test project**

`src/backend/tests/ShopIt.Framework.Tests/ShopIt.Framework.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Framework\ShopIt.Framework.Domain\ShopIt.Framework.Domain.csproj" />
    <ProjectReference Include="..\..\src\Framework\ShopIt.Framework.Core\ShopIt.Framework.Core.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Write the failing smoke test**

`src/backend/tests/ShopIt.Framework.Tests/Permissions/PermissionNameTests.cs`:

```csharp
using ShopIt.Framework.Domain.Permissions;

namespace ShopIt.Framework.Tests.Permissions;

public class PermissionNameTests
{
    [Fact]
    public void PermissionName_WithWhitespace_Throws()
    {
        Assert.Throws<ArgumentException>(() => new PermissionName("   "));
    }

    [Fact]
    public void PermissionName_ImplicitlyConverts_To_String()
    {
        var name = new PermissionName("user.view");
        string value = name;
        Assert.Equal("user.view", value);
    }
}
```

- [ ] **Step 4: Create the identity test project**

`src/backend/tests/ShopIt.Identity.Tests/ShopIt.Identity.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\Services\Identity\ShopIt.Identity.Persistence\ShopIt.Identity.Persistence.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 5: Write the failing smoke test**

`src/backend/tests/ShopIt.Identity.Tests/Permissions/SmokeTests.cs`:

```csharp
using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Tests.Permissions;

public class SmokeTests
{
    [Fact]
    public void PermissionCatalogEntry_Create_RejectsBlankName()
    {
        Assert.Throws<ArgumentException>(() => PermissionCatalogEntry.Create(
            Guid.NewGuid(), "Group", "Group", "  ", "Display", null, "Test"));
    }
}
```

- [ ] **Step 6: Run tests to verify the smoke tests fail**

Run (from `src/backend`):

```bash
dotnet test tests/ShopIt.Framework.Tests/ShopIt.Framework.Tests.csproj -v q --nologo
```

Expected: build fails — `PermissionName` does not exist in `ShopIt.Framework.Domain.Permissions` yet (types still live in `ShopIt.Identity.Domain`). That is the intended red state.

- [ ] **Step 7: Commit**

```bash
git add src/backend/Directory.Packages.props src/backend/tests
git commit -m "chore(tests): scaffold xunit test projects for framework and identity"
```

---

### Task 2: Move permission definition types into `ShopIt.Framework.Domain`

**Files:**
- Create (8): `src/backend/src/Framework/ShopIt.Framework.Domain/Permissions/{IPermissionDefinitionProvider,IPermissionDefinitionContext,PermissionDefinitionContext,PermissionDefinition,PermissionGroupDefinition,PermissionName,PermissionGroupName,PermissionDefinitionContextExtensions}.cs`
- Delete (8): the same 8 filenames under `src/backend/src/Services/Identity/ShopIt.Identity.Domain/Permissions/`
- Modify (9, `using` swaps): `TenantDataSeeder.cs`, `GetPermissionMatrixQueryHandler.cs`, `UpdateRolePermissionsCommandHandler.cs`, `PermissionsModule.cs`, `Program.cs` (Identity.API), `RoleDefinition.cs`, `ShopItIdentityPermissions.cs`, `ShopItIdentityPermissionDefinitionProvider.cs`, `ShopItIdentityRoleDefinitionProvider.cs`
- Test: `src/backend/tests/ShopIt.Framework.Tests/Permissions/PermissionDefinitionContextTests.cs`, `.../PermissionDefinitionContextExtensionsTests.cs`

**Interfaces:**
- Consumes: Task 1 test projects.
- Produces (the exact API every later task uses):
  - `PermissionName(string value)` — record; `string Value`; `static implicit operator string(PermissionName)`.
  - `PermissionGroupName(string value)` — record; `string Value`.
  - `PermissionDefinition(PermissionName Name, string DisplayName, string? Description = null)` — record.
  - `PermissionGroupDefinition(PermissionGroupName Name, string DisplayName, IEnumerable<PermissionDefinition>? Permissions = null)` — class with `IReadOnlyList<PermissionDefinition> Permissions`.
  - `interface IPermissionDefinitionProvider` — `void Define(IPermissionDefinitionContext context)`, `IReadOnlyList<PermissionGroupDefinition> GetGroups()`, `IEnumerable<PermissionDefinition> GetAll()`.
  - `interface IPermissionDefinitionContext` — `PermissionGroupDefinition AddGroup(PermissionGroupName name, string displayName)`, `PermissionDefinition AddPermission(PermissionGroupName groupName, PermissionName name, string displayName, string? description = null)`, `IReadOnlyList<PermissionGroupDefinition> GetGroups()`.
  - `class PermissionDefinitionContext : IPermissionDefinitionContext`.
  - Extensions (all return `PermissionGroupDefinition` for chaining): `AddGroup(this PermissionGroupName, IPermissionDefinitionContext, string)`, `AddPermission(this PermissionGroupDefinition, PermissionName, string, string? = null)`, `AddPermission(this PermissionName, PermissionGroupDefinition, string, string? = null)`.

- [ ] **Step 1: Write the failing framework behavior tests**

`src/backend/tests/ShopIt.Framework.Tests/Permissions/PermissionDefinitionContextTests.cs`:

```csharp
using ShopIt.Framework.Domain.Permissions;

namespace ShopIt.Framework.Tests.Permissions;

public class PermissionDefinitionContextTests
{
    [Fact]
    public void AddGroup_SameName_ReturnsSameInstance_AndRegistersOnce()
    {
        var context = new PermissionDefinitionContext();

        var first = context.AddGroup(new PermissionGroupName("UserManagement"), "User Management");
        var second = context.AddGroup(new PermissionGroupName("UserManagement"), "User Management");

        Assert.Same(first, second);
        Assert.Single(context.GetGroups());
    }

    [Fact]
    public void AddPermission_WithoutGroup_Throws()
    {
        var context = new PermissionDefinitionContext();

        Assert.Throws<InvalidOperationException>(() =>
            context.AddPermission(new PermissionGroupName("Missing"), new PermissionName("user.view"), "View users"));
    }

    [Fact]
    public void AddPermission_AppendsToGroup()
    {
        var context = new PermissionDefinitionContext();
        context.AddGroup(new PermissionGroupName("UserManagement"), "User Management");
        context.AddPermission(new PermissionGroupName("UserManagement"), new PermissionName("user.view"), "View users");
        context.AddPermission(new PermissionGroupName("UserManagement"), new PermissionName("user.create"), "Create users");

        Assert.Equal(2, context.GetGroups()[0].Permissions.Count);
    }
}
```

`src/backend/tests/ShopIt.Framework.Tests/Permissions/PermissionDefinitionContextExtensionsTests.cs`:

```csharp
using ShopIt.Framework.Domain.Permissions;

namespace ShopIt.Framework.Tests.Permissions;

public class PermissionDefinitionContextExtensionsTests
{
    [Fact]
    public void AddGroup_Extension_RegistersGroup()
    {
        var context = new PermissionDefinitionContext();

        var group = new PermissionGroupName("UserManagement").AddGroup(context, "User Management");

        Assert.Same(group, context.GetGroups()[0]);
    }

    [Fact]
    public void AddPermission_Extension_ReturnsGroup_ForChaining()
    {
        var context = new PermissionDefinitionContext();

        new PermissionGroupName("UserManagement").AddGroup(context, "User Management")
            .AddPermission(new PermissionName("user.view"), "View users")
            .AddPermission(new PermissionName("user.create"), "Create users");

        Assert.Equal(2, context.GetGroups()[0].Permissions.Count);
    }

    [Fact]
    public void AddPermission_FromPermissionNameReceiver_Works()
    {
        var context = new PermissionDefinitionContext();
        var group = new PermissionGroupName("UserManagement").AddGroup(context, "User Management");

        var result = new PermissionName("user.view").AddPermission(group, "View users");

        Assert.Same(group, result);
        Assert.Single(group.Permissions);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/ShopIt.Framework.Tests/ShopIt.Framework.Tests.csproj -v q --nologo
```

Expected: FAIL — types cannot be found in `ShopIt.Framework.Domain.Permissions`.

- [ ] **Step 3: Create the framework files**

Each file below goes in `src/backend/src/Framework/ShopIt.Framework.Domain/Permissions/` with namespace `ShopIt.Framework.Domain.Permissions`.

`PermissionName.cs`:

```csharp
namespace ShopIt.Framework.Domain.Permissions;

/// <summary>
/// Value object representing a permission key (e.g. <c>user.create</c>). Used in place of
/// raw strings so permission identities are typed, compared by value, and self-documenting.
/// Implicitly converts to <see cref="string"/> for the claim/API boundary.
/// </summary>
public record PermissionName
{
    public string Value { get; }

    public PermissionName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Permission name cannot be empty or whitespace.", nameof(value));

        Value = value;
    }

    public static implicit operator string(PermissionName name) => name.Value;

    public override string ToString() => Value;
}
```

`PermissionGroupName.cs`:

```csharp
namespace ShopIt.Framework.Domain.Permissions;

/// <summary>
/// Value object representing a permission group key (e.g. <c>UserManagement</c>). Used in
/// place of raw strings so groups are typed and compared by value.
/// </summary>
public record PermissionGroupName
{
    public string Value { get; }

    public PermissionGroupName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Permission group name cannot be empty or whitespace.", nameof(value));

        Value = value;
    }

    public static implicit operator string(PermissionGroupName name) => name.Value;

    public override string ToString() => Value;
}
```

`PermissionDefinition.cs`:

```csharp
namespace ShopIt.Framework.Domain.Permissions;

/// <summary>
/// Defines a single permission in the system.
/// </summary>
/// <param name="Name">The unique permission identifier, as a value object (also used as the claim type when granted).</param>
/// <param name="DisplayName">A human-readable name shown in the UI.</param>
/// <param name="Description">An optional longer description.</param>
public record PermissionDefinition(PermissionName Name, string DisplayName, string? Description = null);
```

`PermissionGroupDefinition.cs`:

```csharp
namespace ShopIt.Framework.Domain.Permissions;

/// <summary>
/// Groups related permissions together so they can be presented as a section
/// (e.g. "User Management") in permission management UIs. Keyed by a
/// <see cref="PermissionGroupName"/> value object.
/// </summary>
public class PermissionGroupDefinition
{
    public PermissionGroupName Name { get; }
    public string DisplayName { get; }

    private readonly List<PermissionDefinition> _permissions = [];
    public IReadOnlyList<PermissionDefinition> Permissions => _permissions;

    public PermissionGroupDefinition(
        PermissionGroupName name,
        string displayName,
        IEnumerable<PermissionDefinition>? permissions = null)
    {
        Name = name;
        DisplayName = displayName;
        if (permissions is not null)
            _permissions.AddRange(permissions);
    }

    /// <summary>
    /// Appends a permission to the group. Mutations flow through the fluent
    /// <c>AddPermission</c> extension methods on <see cref="PermissionGroupDefinition"/>
    /// and <see cref="PermissionName"/>.
    /// </summary>
    internal PermissionDefinition Append(PermissionDefinition permission)
    {
        _permissions.Add(permission);
        return permission;
    }
}
```

`IPermissionDefinitionProvider.cs`:

```csharp
namespace ShopIt.Framework.Domain.Permissions;

/// <summary>
/// Supplies a catalog of permissions known to a service, organized into groups.
/// Each microservice implements this provider for its own permissions. Providers
/// declare their catalog through <see cref="Define"/> (ABP-style), and the result is
/// exposed via <see cref="GetGroups"/> / <see cref="GetAll"/>. The Identity service
/// collects these catalogs (via integration events) into its persisted permission
/// catalog, which is what authorization and the permission management UIs read from.
/// </summary>
public interface IPermissionDefinitionProvider
{
    /// <summary>
    /// Registers permission groups and permissions, keyed by value-object records.
    /// Called once at construction time.
    /// </summary>
    void Define(IPermissionDefinitionContext context);

    /// <summary>
    /// Returns all permission groups with their permissions.
    /// </summary>
    IReadOnlyList<PermissionGroupDefinition> GetGroups();

    /// <summary>
    /// Returns every permission across all groups.
    /// </summary>
    IEnumerable<PermissionDefinition> GetAll();
}
```

`IPermissionDefinitionContext.cs`:

```csharp
namespace ShopIt.Framework.Domain.Permissions;

/// <summary>
/// Accumulates permission groups and permissions while a
/// <see cref="IPermissionDefinitionProvider"/> runs its <c>Define</c> step.
/// </summary>
public interface IPermissionDefinitionContext
{
    /// <summary>
    /// Adds (or returns an existing) group keyed by <paramref name="name"/>.
    /// </summary>
    PermissionGroupDefinition AddGroup(PermissionGroupName name, string displayName);

    /// <summary>
    /// Adds a permission to the group keyed by <paramref name="groupName"/>.
    /// </summary>
    PermissionDefinition AddPermission(
        PermissionGroupName groupName,
        PermissionName name,
        string displayName,
        string? description = null);

    /// <summary>
    /// Returns all groups registered so far, in registration order.
    /// </summary>
    IReadOnlyList<PermissionGroupDefinition> GetGroups();
}
```

`PermissionDefinitionContext.cs`:

```csharp
namespace ShopIt.Framework.Domain.Permissions;

/// <summary>
/// Default implementation of <see cref="IPermissionDefinitionContext"/> used by the
/// permission definition providers.
/// </summary>
public class PermissionDefinitionContext : IPermissionDefinitionContext
{
    private readonly List<PermissionGroupDefinition> _groups = [];
    private readonly Dictionary<PermissionGroupName, PermissionGroupDefinition> _byName = new();

    public PermissionGroupDefinition AddGroup(PermissionGroupName name, string displayName)
    {
        if (_byName.TryGetValue(name, out var existing))
            return existing;

        var group = new PermissionGroupDefinition(name, displayName);
        _groups.Add(group);
        _byName[name] = group;
        return group;
    }

    public PermissionDefinition AddPermission(
        PermissionGroupName groupName,
        PermissionName name,
        string displayName,
        string? description = null)
    {
        if (!_byName.TryGetValue(groupName, out var group))
            throw new InvalidOperationException(
                $"Permission group '{groupName}' must be added before adding permissions to it.");

        return group.Append(new PermissionDefinition(name, displayName, description));
    }

    public IReadOnlyList<PermissionGroupDefinition> GetGroups() => _groups;
}
```

`PermissionDefinitionContextExtensions.cs`:

```csharp
namespace ShopIt.Framework.Domain.Permissions;

/// <summary>
/// Fluent registration API for permission definitions. <c>AddGroup</c> and
/// <c>AddPermission</c> are extension methods on the value-object records
/// (<see cref="PermissionGroupName"/>, <see cref="PermissionGroupDefinition"/>),
/// enabling chainable, typed definition blocks. Both return the group so the chain
/// can continue:
/// <code>
/// Groups.UserManagement.AddGroup(context, "User Management")
///     .AddPermission(Permissions.Users.View, "View users", "View user accounts.")
///     .AddPermission(Permissions.Users.Create, "Create users");
/// </code>
/// </summary>
public static class PermissionDefinitionContextExtensions
{
    /// <summary>
    /// Registers a permission group, keyed by the <see cref="PermissionGroupName"/> value object.
    /// </summary>
    public static PermissionGroupDefinition AddGroup(
        this PermissionGroupName groupName,
        IPermissionDefinitionContext context,
        string displayName)
        => context.AddGroup(groupName, displayName);

    /// <summary>
    /// Adds a permission to the group, keyed by the <see cref="PermissionName"/> value object.
    /// Returns the group so further permissions can be chained.
    /// </summary>
    public static PermissionGroupDefinition AddPermission(
        this PermissionGroupDefinition group,
        PermissionName permissionName,
        string displayName,
        string? description = null)
    {
        group.Append(new PermissionDefinition(permissionName, displayName, description));
        return group;
    }

    /// <summary>
    /// Alternative receiver: adds a permission, invoking it from the <see cref="PermissionName"/>
    /// value object itself. Returns the group so further permissions can be chained.
    /// </summary>
    public static PermissionGroupDefinition AddPermission(
        this PermissionName permissionName,
        PermissionGroupDefinition group,
        string displayName,
        string? description = null)
        => group.AddPermission(permissionName, displayName, description);
}
```

- [ ] **Step 4: Delete the old copies**

```bash
cd src/backend/src/Services/Identity/ShopIt.Identity.Domain/Permissions
rm IPermissionDefinitionProvider.cs IPermissionDefinitionContext.cs PermissionDefinitionContext.cs PermissionDefinition.cs PermissionGroupDefinition.cs PermissionName.cs PermissionGroupName.cs PermissionDefinitionContextExtensions.cs
```

- [ ] **Step 5: Update the nine consumer files**

For each file, replace `using ShopIt.Identity.Domain.Permissions;` with `using ShopIt.Framework.Domain.Permissions;` EXCEPT where the file also needs Identity's own constants (`ShopItIdentityPermissions`, `ShopItIdentityPermissionDefinitionProvider`), in which case ADD the framework using alongside:

1. `src/backend/src/Services/Identity/ShopIt.Identity.Application/DataSeeding/TenantDataSeeder.cs` — replace.
2. `src/backend/src/Services/Identity/ShopIt.Identity.Application/Permissions/Queries/GetPermissionMatrix/GetPermissionMatrixQueryHandler.cs` — replace (keep alphabetical order: `ShopIt.Framework.Domain.Permissions` before `ShopIt.Identity.Domain.Entities`).
3. `src/backend/src/Services/Identity/ShopIt.Identity.Application/Roles/Commands/UpdateRolePermissions/UpdateRolePermissionsCommandHandler.cs` — replace.
4. `src/backend/src/Services/Identity/ShopIt.Identity.Presentation/Permissions/PermissionsModule.cs` — add `using ShopIt.Framework.Domain.Permissions;` (it still needs `ShopIt.Identity.Domain.Permissions` for `ShopItIdentityPermissions`).
5. `src/backend/src/Services/Identity/ShopIt.Identity.API/Program.cs` — add `using ShopIt.Framework.Domain.Permissions;` (still needs Identity's).
6. `src/backend/src/Services/Identity/ShopIt.Identity.Domain/Roles/RoleDefinition.cs` — replace.
7. `src/backend/src/Services/Identity/ShopIt.Identity.Domain/Permissions/ShopItIdentityPermissions.cs` — add.
8. `src/backend/src/Services/Identity/ShopIt.Identity.Domain/Permissions/ShopItIdentityPermissionDefinitionProvider.cs` — add.
9. `src/backend/src/Services/Identity/ShopIt.Identity.Domain/Roles/ShopItIdentityRoleDefinitionProvider.cs` — add.

- [ ] **Step 6: Run tests to verify they pass**

```bash
dotnet test tests/ShopIt.Framework.Tests/ShopIt.Framework.Tests.csproj -v q --nologo
```

Expected: PASS (4 tests from Task 1 smoke + 7 behavior tests). Then build Identity to confirm consumers compile:

```bash
dotnet build src/Services/Identity/ShopIt.Identity.API/ShopIt.Identity.API.csproj -v q --nologo
```

Expected: `Build succeeded.`

- [ ] **Step 7: Commit**

```bash
git add src/backend/src/Framework/ShopIt.Framework.Domain/Permissions src/backend/src/Services/Identity/ShopIt.Identity.Domain/Permissions src/backend/src/Services/Identity/ShopIt.Identity.Domain/Roles src/backend/src/Services/Identity/ShopIt.Identity.Application src/backend/src/Services/Identity/ShopIt.Identity.Presentation src/backend/src/Services/Identity/ShopIt.Identity.API src/backend/tests/ShopIt.Framework.Tests
git commit -m "refactor(framework): move permission definition types into shared framework domain"
```

---

### Task 3: Shared `PermissionCatalogPublishedIntegrationEvent`

**Files:**
- Create: `src/backend/src/Framework/ShopIt.Framework.Core/Events/Integration/PermissionCatalogPublishedIntegrationEvent.cs`
- Test: `src/backend/tests/ShopIt.Framework.Tests/Events/PermissionCatalogPublishedIntegrationEventTests.cs`

**Interfaces:**
- Consumes: `IntegrationEvent` (abstract record in `ShopIt.Framework.Core.Events.Integration` with `EventId`, `OccurredOn`, `EventType`); the Task 2 permission types are NOT used by the DTOs (plain strings keep the wire format decoupled).
- Produces (used by Tasks 6, 7, 10):
  - `sealed record PermissionCatalogPublishedIntegrationEvent(string SourceService, IReadOnlyList<PermissionGroupDto> Groups) : IntegrationEvent`
  - `sealed record PermissionGroupDto(string Name, string DisplayName, IReadOnlyList<PermissionDefinitionDto> Permissions)`
  - `sealed record PermissionDefinitionDto(string Name, string DisplayName, string? Description)`

- [ ] **Step 1: Write the failing round-trip test**

`src/backend/tests/ShopIt.Framework.Tests/Events/PermissionCatalogPublishedIntegrationEventTests.cs`:

```csharp
using System.Text.Json;
using ShopIt.Framework.Core.Events.Integration;

namespace ShopIt.Framework.Tests.Events;

public class PermissionCatalogPublishedIntegrationEventTests
{
    [Fact]
    public void RoundTrips_Through_SystemTextJson()
    {
        var evt = new PermissionCatalogPublishedIntegrationEvent(
            "Tenancy",
            [
                new PermissionGroupDto("TenantManagement", "Tenant Management",
                [
                    new PermissionDefinitionDto("tenant.view", "View tenants", null),
                ]),
            ]);

        var json = JsonSerializer.Serialize(evt, evt.GetType());
        var deserialized = (PermissionCatalogPublishedIntegrationEvent?)JsonSerializer.Deserialize(
            json,
            typeof(PermissionCatalogPublishedIntegrationEvent),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(deserialized);
        Assert.Equal("Tenancy", deserialized.SourceService);
        var group = Assert.Single(deserialized.Groups);
        Assert.Equal("TenantManagement", group.Name);
        var permission = Assert.Single(group.Permissions);
        Assert.Equal("tenant.view", permission.Name);
        Assert.Equal("View tenants", permission.DisplayName);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/ShopIt.Framework.Tests/ShopIt.Framework.Tests.csproj -v q --nologo
```

Expected: FAIL — type not found.

- [ ] **Step 3: Write the event**

`src/backend/src/Framework/ShopIt.Framework.Core/Events/Integration/PermissionCatalogPublishedIntegrationEvent.cs`:

```csharp
namespace ShopIt.Framework.Core.Events.Integration;

/// <summary>
/// Published by every microservice to announce its permission catalog (the permission
/// groups and definitions the service exposes). The Identity service consumes it, upserts
/// the definitions into its persisted permission catalog, and grants any permissions the
/// Admin role does not already hold. Services republish their catalog whenever their
/// permission definitions change (on startup), so new permissions reach Identity without
/// redeploying the Identity project.
/// </summary>
/// <param name="SourceService">The name of the publishing service (e.g. "Tenancy").</param>
/// <param name="Groups">The permission groups and their permissions.</param>
public sealed record PermissionCatalogPublishedIntegrationEvent(
    string SourceService,
    IReadOnlyList<PermissionGroupDto> Groups) : IntegrationEvent;

/// <summary>Wire representation of a permission group in a catalog event.</summary>
public sealed record PermissionGroupDto(
    string Name,
    string DisplayName,
    IReadOnlyList<PermissionDefinitionDto> Permissions);

/// <summary>Wire representation of a single permission definition in a catalog event.</summary>
public sealed record PermissionDefinitionDto(
    string Name,
    string DisplayName,
    string? Description);
```

- [ ] **Step 4: Run test to verify it passes**

```bash
dotnet test tests/ShopIt.Framework.Tests/ShopIt.Framework.Tests.csproj -v q --nologo
```

Expected: PASS (all tests).

- [ ] **Step 5: Commit**

```bash
git add src/backend/src/Framework/ShopIt.Framework.Core/Events/Integration/PermissionCatalogPublishedIntegrationEvent.cs src/backend/tests/ShopIt.Framework.Tests
git commit -m "feat(framework): add permission catalog published integration event"
```

---

### Task 4: Persisted catalog — entity, configuration, migration

**Files:**
- Create: `src/backend/src/Services/Identity/ShopIt.Identity.Domain/Entities/PermissionCatalogEntry.cs`
- Create: `src/backend/src/Services/Identity/ShopIt.Identity.Persistence/Configurations/PermissionCatalogEntryConfiguration.cs`
- Modify: `src/backend/src/Services/Identity/ShopIt.Identity.Persistence/Data/ApplicationDbContext.cs` (add `DbSet`)
- Generated: `src/backend/src/Services/Identity/ShopIt.Identity.Persistence/Migrations/<timestamp>_AddPermissionCatalog.cs` + `.Designer.cs`, snapshot update
- Test: `src/backend/tests/ShopIt.Identity.Tests/Permissions/PermissionCatalogEntryTests.cs`

**Interfaces:**
- Consumes: `IEntity<Guid>` from `ShopIt.Framework.Domain.Entities` (Tasks 1–3 test infra).
- Produces (used by Tasks 5, 6):
  - `class PermissionCatalogEntry : IEntity<Guid>` with `Guid Id`, `string GroupName`, `string GroupDisplayName`, `string Name`, `string DisplayName`, `string? Description`, `string SourceService`, `DateTime CreatedAt`, `DateTime? UpdatedAt`.
  - `static PermissionCatalogEntry Create(Guid id, string groupName, string groupDisplayName, string name, string displayName, string? description, string sourceService)` — throws `ArgumentException` on blank `name`.
  - `void Update(string groupName, string groupDisplayName, string displayName, string? description, string sourceService)` — sets `UpdatedAt`.
  - DbSet: `ApplicationDbContext.PermissionCatalogEntries` (system-wide, NO tenant filter).

- [ ] **Step 1: Write the failing entity test**

`src/backend/tests/ShopIt.Identity.Tests/Permissions/PermissionCatalogEntryTests.cs`:

```csharp
using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Tests.Permissions;

public class PermissionCatalogEntryTests
{
    [Fact]
    public void Create_SetsAllProperties()
    {
        var entry = PermissionCatalogEntry.Create(
            Guid.NewGuid(), "TenantManagement", "Tenant Management",
            "tenant.view", "View tenants", "View tenants in the system.", "Tenancy");

        Assert.Equal("tenant.view", entry.Name);
        Assert.Equal("TenantManagement", entry.GroupName);
        Assert.Equal("Tenant Management", entry.GroupDisplayName);
        Assert.Equal("View tenants", entry.DisplayName);
        Assert.Equal("View tenants in the system.", entry.Description);
        Assert.Equal("Tenancy", entry.SourceService);
        Assert.Null(entry.UpdatedAt);
    }

    [Fact]
    public void Update_RefreshesMetadata_AndSetsUpdatedAt()
    {
        var entry = PermissionCatalogEntry.Create(
            Guid.NewGuid(), "TenantManagement", "Tenant Management",
            "tenant.view", "View tenants", null, "Tenancy");

        entry.Update("TenantManagement", "Tenant Management", "View all tenants", "Updated description", "Tenancy");

        Assert.Equal("View all tenants", entry.DisplayName);
        Assert.Equal("Updated description", entry.Description);
        Assert.NotNull(entry.UpdatedAt);
        Assert.Equal("tenant.view", entry.Name); // identity immutable
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/ShopIt.Identity.Tests/ShopIt.Identity.Tests.csproj -v q --nologo
```

Expected: FAIL — `PermissionCatalogEntry` not found (the smoke test from Task 1 also fails for the same reason).

- [ ] **Step 3: Write the entity**

`src/backend/src/Services/Identity/ShopIt.Identity.Domain/Entities/PermissionCatalogEntry.cs`:

```csharp
using ShopIt.Framework.Domain.Entities;

namespace ShopIt.Identity.Domain.Entities;

/// <summary>
/// A single permission definition persisted in the Identity service's permission catalog.
/// The catalog is the union of every microservice's permission definitions: each service
/// publishes its catalog via an integration event and Identity upserts it here, so new
/// permissions arrive without redeploying Identity. The catalog is system-wide (not
/// tenant-scoped) — every tenant sees the same grantable permissions.
/// </summary>
public class PermissionCatalogEntry : IEntity<Guid>
{
    public Guid Id { get; private set; } = default!;
    public string GroupName { get; private set; } = default!;
    public string GroupDisplayName { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string DisplayName { get; private set; } = default!;
    public string? Description { get; private set; }
    public string SourceService { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; } = default!;
    public DateTime? UpdatedAt { get; private set; }

    public object GetId() => Id;

    // Public parameterless constructor for EF Core.
    public PermissionCatalogEntry() { }

    private PermissionCatalogEntry(
        Guid id,
        string groupName,
        string groupDisplayName,
        string name,
        string displayName,
        string? description,
        string sourceService)
    {
        Id = id;
        GroupName = groupName;
        GroupDisplayName = groupDisplayName;
        Name = name;
        DisplayName = displayName;
        Description = description;
        SourceService = sourceService;
        CreatedAt = DateTime.UtcNow;
    }

    public static PermissionCatalogEntry Create(
        Guid id,
        string groupName,
        string groupDisplayName,
        string name,
        string displayName,
        string? description,
        string sourceService)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Permission name cannot be empty or whitespace.", nameof(name));

        return new PermissionCatalogEntry(
            id, groupName, groupDisplayName, name, displayName, description, sourceService);
    }

    /// <summary>
    /// Refreshes the display metadata from a republished definition. The permission
    /// identity (<see cref="Name"/>) is immutable once persisted.
    /// </summary>
    public void Update(
        string groupName,
        string groupDisplayName,
        string displayName,
        string? description,
        string sourceService)
    {
        GroupName = groupName;
        GroupDisplayName = groupDisplayName;
        DisplayName = displayName;
        Description = description;
        SourceService = sourceService;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

- [ ] **Step 4: Write the EF configuration**

`src/backend/src/Services/Identity/ShopIt.Identity.Persistence/Configurations/PermissionCatalogEntryConfiguration.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShopIt.Identity.Domain.Entities;

namespace ShopIt.Identity.Persistence.Configurations;

/// <summary>
/// Configures <see cref="PermissionCatalogEntry"/> — a permission definition persisted in
/// the Identity service's catalog. The catalog is system-wide (not tenant-scoped), and a
/// permission name is unique across the whole catalog regardless of which service it came
/// from, so grants resolve unambiguously to a single definition.
/// </summary>
public class PermissionCatalogEntryConfiguration : IEntityTypeConfiguration<PermissionCatalogEntry>
{
    public void Configure(EntityTypeBuilder<PermissionCatalogEntry> builder)
    {
        builder.ToTable("PermissionCatalogEntries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(e => e.GroupName)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(e => e.GroupDisplayName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.DisplayName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.Description)
            .HasMaxLength(1024);

        builder.Property(e => e.SourceService)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasIndex(e => e.Name)
            .IsUnique()
            .HasDatabaseName("IX_PermissionCatalogEntries_Name");

        builder.HasIndex(e => e.GroupName);
    }
}
```

- [ ] **Step 5: Add the DbSet to `ApplicationDbContext`**

In `src/backend/src/Services/Identity/ShopIt.Identity.Persistence/Data/ApplicationDbContext.cs`, inside the class body (after the `ICurrentTenant` field, before `OnModelCreating`):

```csharp
    /// <summary>
    /// Persisted permission catalog: the union of every service's permission definitions.
    /// System-wide (not tenant-scoped) — see <see cref="Entities.PermissionCatalogEntry"/>.
    /// </summary>
    public DbSet<PermissionCatalogEntry> PermissionCatalogEntries => Set<PermissionCatalogEntry>();
```

- [ ] **Step 6: Generate the migration**

Run (from `src/backend`; `dotnet ef` 10.0.5 is installed):

```bash
dotnet ef migrations add AddPermissionCatalog --project src/Services/Identity/ShopIt.Identity.Persistence --startup-project src/Services/Identity/ShopIt.Identity.API --output-dir Migrations
```

Expected: creates `src/backend/src/Services/Identity/ShopIt.Identity.Persistence/Migrations/<timestamp>_AddPermissionCatalog.cs` whose `Up()` creates table `PermissionCatalogEntries` with columns `Id`, `GroupName`, `GroupDisplayName`, `Name`, `DisplayName`, `Description`, `SourceService`, `CreatedAt`, `UpdatedAt` plus unique index `IX_PermissionCatalogEntries_Name` and non-unique index `IX_PermissionCatalogEntries_GroupName`. The model snapshot is updated automatically.

- [ ] **Step 7: Run tests to verify they pass + build**

```bash
dotnet test tests/ShopIt.Identity.Tests/ShopIt.Identity.Tests.csproj -v q --nologo
dotnet build src/Services/Identity/ShopIt.Identity.API/ShopIt.Identity.API.csproj -v q --nologo
```

Expected: PASS; `Build succeeded.`

- [ ] **Step 8: Commit**

```bash
git add src/backend/src/Services/Identity/ShopIt.Identity.Domain/Entities/PermissionCatalogEntry.cs src/backend/src/Services/Identity/ShopIt.Identity.Persistence
git commit -m "feat(identity): persist permission catalog entries with EF migration"
```

---

### Task 5: DB-backed `DatabasePermissionDefinitionProvider`

**Files:**
- Create: `src/backend/src/Services/Identity/ShopIt.Identity.Persistence/Permissions/DatabasePermissionDefinitionProvider.cs`
- Test: `src/backend/tests/ShopIt.Identity.Tests/Permissions/DatabasePermissionDefinitionProviderTests.cs`

**Interfaces:**
- Consumes: `ApplicationDbContext` (Task 4), `IPermissionDefinitionProvider` + `PermissionGroupDefinition`/`PermissionDefinition`/`PermissionName`/`PermissionGroupName` (Task 2).
- Produces: `class DatabasePermissionDefinitionProvider(ApplicationDbContext dbContext) : IPermissionDefinitionProvider` — `Define` throws `NotSupportedException`; `GetGroups()` returns groups ordered by `GroupName` then `Name`, keyed by `(GroupName, GroupDisplayName)`; `GetAll()` flattens.

- [ ] **Step 1: Write the failing test**

`src/backend/tests/ShopIt.Identity.Tests/Permissions/DatabasePermissionDefinitionProviderTests.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using ShopIt.Framework.Domain.Permissions;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Identity.Domain.Tenancy;
using ShopIt.Identity.Persistence.Data;
using ShopIt.Identity.Persistence.Permissions;

namespace ShopIt.Identity.Tests.Permissions;

public class DatabasePermissionDefinitionProviderTests
{
    [Fact]
    public void GetGroups_ReturnsGroupsFromCatalog_OrderedAlphabetically()
    {
        using var db = CreateContext();
        db.PermissionCatalogEntries.AddRange(
            PermissionCatalogEntry.Create(Guid.NewGuid(), "RoleManagement", "Role Management", "role.view", "View roles", null, "Identity"),
            PermissionCatalogEntry.Create(Guid.NewGuid(), "UserManagement", "User Management", "user.view", "View users", null, "Identity"),
            PermissionCatalogEntry.Create(Guid.NewGuid(), "UserManagement", "User Management", "user.create", "Create users", null, "Identity"));
        db.SaveChanges();

        var provider = new DatabasePermissionDefinitionProvider(db);
        var groups = provider.GetGroups();

        Assert.Equal(2, groups.Count);
        Assert.Equal("RoleManagement", groups[0].Name.Value); // alphabetical
        Assert.Equal("UserManagement", groups[1].Name.Value);
        Assert.Equal(2, groups[1].Permissions.Count);
        Assert.Equal("user.create", groups[1].Permissions[0].Name.Value); // alphabetical within group
        Assert.Equal(3, provider.GetAll().Count());
    }

    [Fact]
    public void Define_Throws_NotSupported()
    {
        using var db = CreateContext();
        var provider = new DatabasePermissionDefinitionProvider(db);

        Assert.Throws<NotSupportedException>(() =>
            provider.Define(new PermissionDefinitionContext()));
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"test-{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options, new StubCurrentTenant());
    }

    private sealed class StubCurrentTenant : ICurrentTenant
    {
        public Guid Id => Guid.Empty;
        public string? Name => "Host";
        public IDisposable Change(TenantInfo tenant) => new NoopDisposable();
        private sealed class NoopDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/ShopIt.Identity.Tests/ShopIt.Identity.Tests.csproj -v q --nologo
```

Expected: FAIL — `DatabasePermissionDefinitionProvider` not found.

- [ ] **Step 3: Write the implementation**

`src/backend/src/Services/Identity/ShopIt.Identity.Persistence/Permissions/DatabasePermissionDefinitionProvider.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using ShopIt.Framework.Domain.Permissions;
using ShopIt.Identity.Persistence.Data;

namespace ShopIt.Identity.Persistence.Permissions;

/// <summary>
/// <see cref="IPermissionDefinitionProvider"/> backed by the persisted permission catalog.
/// The catalog is the union of every service's definitions (Identity seeds its own at
/// startup; other services publish theirs via integration events), so the grantable
/// permission set updates without redeploying this service. Replaces the previous
/// in-memory provider, which could only see permissions hardcoded into the Identity
/// codebase.
/// </summary>
public class DatabasePermissionDefinitionProvider(ApplicationDbContext dbContext) : IPermissionDefinitionProvider
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public void Define(IPermissionDefinitionContext context)
    {
        throw new NotSupportedException(
            "The persisted permission catalog is read-only; it is populated from catalogs " +
            "published by each service. Use a service-specific provider (e.g. " +
            nameof(ShopIt.Identity.Domain.Permissions.ShopItIdentityPermissionDefinitionProvider) +
            ") for defining new permissions.");
    }

    public IReadOnlyList<PermissionGroupDefinition> GetGroups()
    {
        var entries = _dbContext.PermissionCatalogEntries
            .OrderBy(e => e.GroupName)
            .ThenBy(e => e.Name)
            .ToList();

        return entries
            .GroupBy(e => new { e.GroupName, e.GroupDisplayName })
            .Select(g => new PermissionGroupDefinition(
                new PermissionGroupName(g.Key.GroupName),
                g.Key.GroupDisplayName,
                g.Select(e => new PermissionDefinition(
                    new PermissionName(e.Name),
                    e.DisplayName,
                    e.Description)).ToList()))
            .ToList();
    }

    public IEnumerable<PermissionDefinition> GetAll() =>
        GetGroups().SelectMany(g => g.Permissions);
}
```

- [ ] **Step 4: Run test to verify it passes**

```bash
dotnet test tests/ShopIt.Identity.Tests/ShopIt.Identity.Tests.csproj -v q --nologo
```

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add src/backend/src/Services/Identity/ShopIt.Identity.Persistence/Permissions/DatabasePermissionDefinitionProvider.cs src/backend/tests/ShopIt.Identity.Tests
git commit -m "feat(identity): serve permission catalog from database-backed provider"
```

---

### Task 6: `PermissionCatalogSynchronizer` — upsert + grant new permissions to Admin

**Files:**
- Create: `src/backend/src/Services/Identity/ShopIt.Identity.Persistence/Permissions/IPermissionCatalogSynchronizer.cs`
- Create: `src/backend/src/Services/Identity/ShopIt.Identity.Persistence/Permissions/PermissionCatalogSynchronizer.cs`
- Test: `src/backend/tests/ShopIt.Identity.Tests/Permissions/PermissionCatalogSynchronizerTests.cs`

**Interfaces:**
- Consumes: `PermissionCatalogEntry` (Task 4), `Role`/`RoleClaim` entities (`Role.Create(Guid id, string name, Guid tenantId, string createdBy, string? description = null)`, `RoleClaim.Create(Role role, string claimType, string claimValue)`), `PermissionGroupDefinition` (Task 2), `ApplicationDbContext`.
- Produces:
  - `interface IPermissionCatalogSynchronizer` — `Task SynchronizeAsync(string sourceService, IReadOnlyList<PermissionGroupDefinition> groups, CancellationToken cancellationToken = default)`.
  - `class PermissionCatalogSynchronizer(ApplicationDbContext dbContext, ILogger<PermissionCatalogSynchronizer> logger) : IPermissionCatalogSynchronizer` — upserts definitions by `Name` (case-insensitive); new names are granted as `RoleClaim` rows (`ClaimType` = name, `ClaimValue` = `"true"`) to every role with `NormalizedName == "ADMIN"` across all tenants (`IgnoreQueryFilters`); idempotent.

- [ ] **Step 1: Write the failing tests**

`src/backend/tests/ShopIt.Identity.Tests/Permissions/PermissionCatalogSynchronizerTests.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ShopIt.Framework.Domain.Permissions;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Identity.Domain.Tenancy;
using ShopIt.Identity.Persistence.Data;
using ShopIt.Identity.Persistence.Permissions;

namespace ShopIt.Identity.Tests.Permissions;

public class PermissionCatalogSynchronizerTests
{
    private static readonly Guid TenantOne = Guid.Parse("B5D0C0E4-3A5B-4CDC-8D2A-7F1F6C9F5B4E");

    [Fact]
    public async Task SynchronizeAsync_AddsNewPermissions_AndGrantsThemToEveryAdminRole()
    {
        await using var db = CreateContext();
        await SeedRolesAsync(db, adminTenantIds: [Guid.Empty, TenantOne], managerTenantIds: [Guid.Empty]);
        var synchronizer = CreateSynchronizer(db);

        await synchronizer.SynchronizeAsync("Tenancy", SingleGroup("tenant.view", "View tenants"));

        var entry = Assert.Single(db.PermissionCatalogEntries);
        Assert.Equal("Tenancy", entry.SourceService);

        var claims = db.RoleClaims.IgnoreQueryFilters().ToList();
        Assert.Equal(2, claims.Count); // host Admin + tenant Admin
        Assert.All(claims, c => Assert.Equal("tenant.view", c.ClaimType));
        Assert.All(claims, c => Assert.Equal("true", c.ClaimValue));
    }

    [Fact]
    public async Task SynchronizeAsync_DoesNotGrantToNonAdminRoles()
    {
        await using var db = CreateContext();
        await SeedRolesAsync(db, adminTenantIds: [], managerTenantIds: [Guid.Empty]);
        var synchronizer = CreateSynchronizer(db);

        await synchronizer.SynchronizeAsync("Tenancy", SingleGroup("tenant.view", "View tenants"));

        Assert.Empty(db.RoleClaims.IgnoreQueryFilters());
    }

    [Fact]
    public async Task SynchronizeAsync_WhenNoAdminRole_StillPersistsCatalog()
    {
        await using var db = CreateContext();
        var synchronizer = CreateSynchronizer(db);

        await synchronizer.SynchronizeAsync("Tenancy", SingleGroup("tenant.view", "View tenants"));

        Assert.Single(db.PermissionCatalogEntries);
        Assert.Empty(db.RoleClaims.IgnoreQueryFilters());
    }

    [Fact]
    public async Task SynchronizeAsync_IsIdempotent()
    {
        await using var db = CreateContext();
        await SeedRolesAsync(db, adminTenantIds: [Guid.Empty], managerTenantIds: []);
        var synchronizer = CreateSynchronizer(db);

        await synchronizer.SynchronizeAsync("Tenancy", SingleGroup("tenant.view", "View tenants"));
        await synchronizer.SynchronizeAsync("Tenancy", SingleGroup("tenant.view", "View tenants"));

        Assert.Single(db.PermissionCatalogEntries);
        Assert.Single(db.RoleClaims.IgnoreQueryFilters());
    }

    [Fact]
    public async Task SynchronizeAsync_UpdatesMetadata_ButDoesNotRegrant()
    {
        await using var db = CreateContext();
        await SeedRolesAsync(db, adminTenantIds: [Guid.Empty], managerTenantIds: []);
        var synchronizer = CreateSynchronizer(db);

        await synchronizer.SynchronizeAsync("Tenancy", SingleGroup("tenant.view", "View tenants"));
        await synchronizer.SynchronizeAsync("Tenancy", SingleGroup("tenant.view", "View all tenants"));

        var entry = db.PermissionCatalogEntries.Single();
        Assert.Equal("View all tenants", entry.DisplayName);
        Assert.NotNull(entry.UpdatedAt);
        Assert.Single(db.RoleClaims.IgnoreQueryFilters()); // unchanged
    }

    private static IReadOnlyList<PermissionGroupDefinition> SingleGroup(string permissionName, string displayName) =>
    [
        new PermissionGroupDefinition(
            new PermissionGroupName("TenantManagement"),
            "Tenant Management",
            [new PermissionDefinition(new PermissionName(permissionName), displayName, null)]),
    ];

    private static async Task SeedRolesAsync(
        ApplicationDbContext db,
        IReadOnlyCollection<Guid> adminTenantIds,
        IReadOnlyCollection<Guid> managerTenantIds)
    {
        foreach (var tenantId in adminTenantIds)
            db.Roles.Add(Role.Create(Guid.NewGuid(), "Admin", tenantId, "system"));
        foreach (var tenantId in managerTenantIds)
            db.Roles.Add(Role.Create(Guid.NewGuid(), "Manager", tenantId, "system"));
        await db.SaveChangesAsync();
    }

    private static PermissionCatalogSynchronizer CreateSynchronizer(ApplicationDbContext db) =>
        new(db, NullLogger<PermissionCatalogSynchronizer>.Instance);

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"test-{Guid.NewGuid():N}")
            .Options;
        return new ApplicationDbContext(options, new StubCurrentTenant());
    }

    private sealed class StubCurrentTenant : ICurrentTenant
    {
        public Guid Id => Guid.Empty;
        public string? Name => "Host";
        public IDisposable Change(TenantInfo tenant) => new NoopDisposable();
        private sealed class NoopDisposable : IDisposable
        {
            public void Dispose() { }
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test tests/ShopIt.Identity.Tests/ShopIt.Identity.Tests.csproj -v q --nologo
```

Expected: FAIL — `IPermissionCatalogSynchronizer` not found.

- [ ] **Step 3: Write the interface**

`src/backend/src/Services/Identity/ShopIt.Identity.Persistence/Permissions/IPermissionCatalogSynchronizer.cs`:

```csharp
using ShopIt.Framework.Domain.Permissions;

namespace ShopIt.Identity.Persistence.Permissions;

/// <summary>
/// Merges a service's permission catalog into the persisted permission catalog and grants
/// newly added permissions to the Admin role in every tenant. Used both by the integration
/// event handler (catalogs published by other services) and by Identity's own startup
/// seeding (its own catalog).
/// </summary>
public interface IPermissionCatalogSynchronizer
{
    /// <summary>
    /// Upserts <paramref name="groups"/> into the catalog and grants any permission names
    /// that are new to the catalog to every Admin role. Idempotent — safe to call repeatedly
    /// (e.g. on every startup or on republished catalogs).
    /// </summary>
    Task SynchronizeAsync(
        string sourceService,
        IReadOnlyList<PermissionGroupDefinition> groups,
        CancellationToken cancellationToken = default);
}
```

- [ ] **Step 4: Write the implementation**

`src/backend/src/Services/Identity/ShopIt.Identity.Persistence/Permissions/PermissionCatalogSynchronizer.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShopIt.Framework.Domain.Permissions;
using ShopIt.Identity.Domain.Entities;
using ShopIt.Identity.Persistence.Data;

namespace ShopIt.Identity.Persistence.Permissions;

/// <inheritdoc cref="IPermissionCatalogSynchronizer" />
public class PermissionCatalogSynchronizer(
    ApplicationDbContext dbContext,
    ILogger<PermissionCatalogSynchronizer> logger) : IPermissionCatalogSynchronizer
{
    // The Admin role is granted every permission as it enters the catalog. Role names are
    // tenant-scoped, so this matches the host role and every tenant's Admin role.
    private const string AdminRoleNormalizedName = "ADMIN";

    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly ILogger<PermissionCatalogSynchronizer> _logger = logger;

    public async Task SynchronizeAsync(
        string sourceService,
        IReadOnlyList<PermissionGroupDefinition> groups,
        CancellationToken cancellationToken = default)
    {
        var existingByName = await _dbContext.PermissionCatalogEntries
            .ToDictionaryAsync(e => e.Name, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var addedNames = new List<string>();
        var updated = 0;

        foreach (var group in groups)
        {
            foreach (var permission in group.Permissions)
            {
                var name = permission.Name.Value;

                if (existingByName.TryGetValue(name, out var entry))
                {
                    // The permission identity (name) is immutable; refresh its metadata.
                    entry.Update(
                        group.Name,
                        group.DisplayName,
                        permission.DisplayName,
                        permission.Description,
                        sourceService);
                    updated++;
                }
                else
                {
                    _dbContext.PermissionCatalogEntries.Add(PermissionCatalogEntry.Create(
                        Guid.NewGuid(),
                        group.Name,
                        group.DisplayName,
                        name,
                        permission.DisplayName,
                        permission.Description,
                        sourceService));
                    addedNames.Add(name);
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var granted = addedNames.Count == 0
            ? 0
            : await GrantToAdminRolesAsync(addedNames, cancellationToken);

        _logger.LogInformation(
            "Permission catalog synchronized from '{SourceService}': {Added} added, {Updated} updated, " +
            "{Granted} new permission(s) granted to Admin role(s).",
            sourceService, addedNames.Count, updated, granted);
    }

    /// <summary>
    /// Grants the given permission names to the Admin role in every tenant (host + all
    /// tenants), skipping grants that already exist. Returns the number of claims added.
    /// </summary>
    private async Task<int> GrantToAdminRolesAsync(
        IReadOnlyCollection<string> permissionNames,
        CancellationToken cancellationToken)
    {
        var adminRoles = await _dbContext.Roles
            .IgnoreQueryFilters()
            .Where(r => r.NormalizedName == AdminRoleNormalizedName)
            .ToListAsync(cancellationToken);

        if (adminRoles.Count == 0)
        {
            _logger.LogDebug(
                "No Admin role exists yet; skipping grants for {Count} new permission(s). " +
                "Role seeding grants the full catalog when the role is created.",
                permissionNames.Count);
            return 0;
        }

        var existingClaims = await _dbContext.RoleClaims
            .IgnoreQueryFilters()
            .Where(c => permissionNames.Contains(c.ClaimType))
            .Select(c => new { c.RoleId, c.ClaimType })
            .ToListAsync(cancellationToken);

        var alreadyGranted = existingClaims
            .Select(c => (c.RoleId, c.ClaimType))
            .ToHashSet();

        var granted = 0;
        foreach (var role in adminRoles)
        {
            foreach (var name in permissionNames)
            {
                if (alreadyGranted.Contains((role.Id, name)))
                {
                    continue;
                }

                _dbContext.RoleClaims.Add(RoleClaim.Create(role, name, "true"));
                granted++;
            }
        }

        if (granted > 0)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return granted;
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test tests/ShopIt.Identity.Tests/ShopIt.Identity.Tests.csproj -v q --nologo
```

Expected: PASS (all 5 synchronizer tests + prior tests).

- [ ] **Step 6: Commit**

```bash
git add src/backend/src/Services/Identity/ShopIt.Identity.Persistence/Permissions/IPermissionCatalogSynchronizer.cs src/backend/src/Services/Identity/ShopIt.Identity.Persistence/Permissions/PermissionCatalogSynchronizer.cs src/backend/tests/ShopIt.Identity.Tests
git commit -m "feat(identity): synchronize permission catalog and grant new permissions to admin"
```

---

### Task 7: Inbox handler + DI wiring (drop the in-memory provider registration)

**Files:**
- Create: `src/backend/src/Services/Identity/ShopIt.Identity.Persistence/Permissions/PermissionCatalogPublishedIntegrationEventHandler.cs`
- Modify: `src/backend/src/Services/Identity/ShopIt.Identity.Persistence/DependencyInjection.cs` (register provider/synchronizer/handler dependencies)
- Modify: `src/backend/src/Services/Identity/ShopIt.Identity.Application/DependencyInjection.cs` (remove the old `IPermissionDefinitionProvider` singleton)
- Test: `src/backend/tests/ShopIt.Identity.Tests/Permissions/PermissionCatalogPublishedIntegrationEventHandlerTests.cs`

**Interfaces:**
- Consumes: `PermissionCatalogPublishedIntegrationEvent` + DTOs (Task 3), `IPermissionCatalogSynchronizer` (Task 6), `PermissionGroupDefinition`/`PermissionName`/`PermissionGroupName` (Task 2).
- Produces:
  - `class PermissionCatalogPublishedIntegrationEventHandler(IPermissionCatalogSynchronizer synchronizer, ILogger<PermissionCatalogPublishedIntegrationEventHandler> logger) : IIntegrationEventHandler<PermissionCatalogPublishedIntegrationEvent>` — converts DTOs to `PermissionGroupDefinition` and calls `SynchronizeAsync(event.SourceService, groups, ct)`.
  - DI: `services.AddScoped<IPermissionDefinitionProvider, DatabasePermissionDefinitionProvider>()`, `services.AddSingleton<ShopItIdentityPermissionDefinitionProvider>()`, `services.AddScoped<IPermissionCatalogSynchronizer, PermissionCatalogSynchronizer>()` in `ShopIt.Identity.Persistence.DependencyInjection.AddPersistence`.

- [ ] **Step 1: Write the failing handler test**

`src/backend/tests/ShopIt.Identity.Tests/Permissions/PermissionCatalogPublishedIntegrationEventHandlerTests.cs`:

```csharp
using Microsoft.Extensions.Logging.Abstractions;
using ShopIt.Framework.Core.Events.Integration;
using ShopIt.Framework.Domain.Permissions;
using ShopIt.Identity.Persistence.Permissions;

namespace ShopIt.Identity.Tests.Permissions;

public class PermissionCatalogPublishedIntegrationEventHandlerTests
{
    [Fact]
    public async Task HandleAsync_ConvertsDtos_AndSynchronizes()
    {
        var synchronizer = new RecordingSynchronizer();
        var handler = new PermissionCatalogPublishedIntegrationEventHandler(
            synchronizer, NullLogger<PermissionCatalogPublishedIntegrationEventHandler>.Instance);

        var evt = new PermissionCatalogPublishedIntegrationEvent(
            "Tenancy",
            [
                new PermissionGroupDto("TenantManagement", "Tenant Management",
                [
                    new PermissionDefinitionDto("tenant.view", "View tenants", "View tenants in the system."),
                ]),
            ]);

        await handler.HandleAsync(evt, CancellationToken.None);

        Assert.Equal("Tenancy", synchronizer.SourceService);
        var group = Assert.Single(synchronizer.Groups!);
        Assert.Equal("TenantManagement", group.Name.Value);
        Assert.Equal("Tenant Management", group.DisplayName);
        var permission = Assert.Single(group.Permissions);
        Assert.Equal("tenant.view", permission.Name.Value);
        Assert.Equal("View tenants in the system.", permission.Description);
    }

    private sealed class RecordingSynchronizer : IPermissionCatalogSynchronizer
    {
        public string? SourceService { get; private set; }
        public IReadOnlyList<PermissionGroupDefinition>? Groups { get; private set; }

        public Task SynchronizeAsync(
            string sourceService,
            IReadOnlyList<PermissionGroupDefinition> groups,
            CancellationToken cancellationToken = default)
        {
            SourceService = sourceService;
            Groups = groups;
            return Task.CompletedTask;
        }
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

```bash
dotnet test tests/ShopIt.Identity.Tests/ShopIt.Identity.Tests.csproj -v q --nologo
```

Expected: FAIL — handler type not found.

- [ ] **Step 3: Write the handler**

`src/backend/src/Services/Identity/ShopIt.Identity.Persistence/Permissions/PermissionCatalogPublishedIntegrationEventHandler.cs`:

```csharp
using Microsoft.Extensions.Logging;
using ShopIt.Framework.Core.Events.Integration;
using ShopIt.Framework.Domain.Permissions;

namespace ShopIt.Identity.Persistence.Permissions;

/// <summary>
/// Consumes <see cref="PermissionCatalogPublishedIntegrationEvent"/> published by other
/// microservices (each service announces its permission catalog on startup / whenever its
/// permissions change). Upserts the definitions into the persisted catalog and grants any
/// new permissions to the Admin role — so a service can add permissions without redeploying
/// the Identity project.
/// </summary>
public class PermissionCatalogPublishedIntegrationEventHandler(
    IPermissionCatalogSynchronizer synchronizer,
    ILogger<PermissionCatalogPublishedIntegrationEventHandler> logger)
    : IIntegrationEventHandler<PermissionCatalogPublishedIntegrationEvent>
{
    private readonly IPermissionCatalogSynchronizer _synchronizer = synchronizer;
    private readonly ILogger<PermissionCatalogPublishedIntegrationEventHandler> _logger = logger;

    public async Task HandleAsync(
        PermissionCatalogPublishedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        var groups = integrationEvent.Groups
            .Select(g => new PermissionGroupDefinition(
                new PermissionGroupName(g.Name),
                g.DisplayName,
                g.Permissions
                    .Select(p => new PermissionDefinition(
                        new PermissionName(p.Name),
                        p.DisplayName,
                        p.Description))
                    .ToList()))
            .ToList();

        _logger.LogInformation(
            "Received permission catalog from '{SourceService}' with {GroupCount} group(s).",
            integrationEvent.SourceService, groups.Count);

        await _synchronizer.SynchronizeAsync(integrationEvent.SourceService, groups, cancellationToken);
    }
}
```

- [ ] **Step 4: Update Persistence DI**

In `src/backend/src/Services/Identity/ShopIt.Identity.Persistence/DependencyInjection.cs`, after the `AddPersistenceServices<ApplicationDbContext>(...)` call and before the `AddKafkaIntegration` call, insert:

```csharp
        // The permission catalog is persisted in this database and is the union of every
        // service's definitions. Other services publish their catalogs via integration
        // events (handled below); Identity seeds its own catalog at startup. Scoped because
        // it reads the catalog from the database.
        services.AddScoped<IPermissionDefinitionProvider, DatabasePermissionDefinitionProvider>();
        services.AddSingleton<ShopItIdentityPermissionDefinitionProvider>();
        services.AddScoped<IPermissionCatalogSynchronizer, PermissionCatalogSynchronizer>();
```

Add the missing usings at the top of that file:

```csharp
using ShopIt.Framework.Domain.Permissions;
using ShopIt.Identity.Domain.Permissions;
using ShopIt.Identity.Persistence.Permissions;
```

- [ ] **Step 5: Remove the old registration from Application DI**

In `src/backend/src/Services/Identity/ShopIt.Identity.Application/DependencyInjection.cs`, delete these lines:

```csharp
        // Permission catalog is static and shared across requests.
        services.AddSingleton<IPermissionDefinitionProvider, ShopItIdentityPermissionDefinitionProvider>();
```

and remove the now-unused `using ShopIt.Identity.Domain.Permissions;`. Replace the deleted block's comment with:

```csharp
        // The permission catalog is not registered here: it is persisted in the database
        // and registered by the Persistence layer (see AddPersistence).
```

- [ ] **Step 6: Run tests + build to verify**

```bash
dotnet test tests/ShopIt.Identity.Tests/ShopIt.Identity.Tests.csproj -v q --nologo
dotnet build src/Services/Identity/ShopIt.Identity.API/ShopIt.Identity.API.csproj -v q --nologo
```

Expected: PASS; `Build succeeded.` (`ShopItIdentityPermissionDefinitionProvider` is still resolvable via its new concrete-singleton registration.)

- [ ] **Step 7: Commit**

```bash
git add src/backend/src/Services/Identity/ShopIt.Identity.Persistence/Permissions/PermissionCatalogPublishedIntegrationEventHandler.cs src/backend/src/Services/Identity/ShopIt.Identity.Persistence/DependencyInjection.cs src/backend/src/Services/Identity/ShopIt.Identity.Application/DependencyInjection.cs src/backend/tests/ShopIt.Identity.Tests
git commit -m "feat(identity): consume permission catalog events and register db-backed catalog"
```

---

### Task 8: Remove auto-admin behaviors and slim Identity's catalog

**Files:**
- Delete: `src/backend/src/Services/Identity/ShopIt.Identity.Application/Roles/EventHandlers/RoleCreatedEventHandler.cs`
- Modify: `src/backend/src/Services/Identity/ShopIt.Identity.Domain/Permissions/ShopItIdentityPermissions.cs` (remove `Tenants` class + `Groups.TenantManagement`)
- Modify: `src/backend/src/Services/Identity/ShopIt.Identity.Domain/Permissions/ShopItIdentityPermissionDefinitionProvider.cs` (remove Tenant Management group; add `SourceService` const)
- Modify: `src/backend/src/Services/Identity/ShopIt.Identity.Domain/Roles/ShopItIdentityRoleDefinitionProvider.cs` (literal tenant permission names)
- Modify: `src/backend/src/Services/Identity/ShopIt.Identity.Domain/Roles/RoleDefinition.cs` (re-document `GrantsAllPermissions`)

**Interfaces:**
- Consumes: Task 2 permission types; `RoleDefinition(RoleName Name, string? DisplayName = null, string? Description = null, bool IsDefault = false, bool IsStatic = true, IReadOnlyList<PermissionName>? DefaultPermissions = null)` with `bool GrantsAllPermissions => DefaultPermissions is null`.
- Produces: no new public API. Behavior changes: (a) no handler assigns new roles to admins; (b) `ShopItIdentityPermissionDefinitionProvider` exposes `public const string SourceService = "Identity"` (used by Task 9); (c) `ShopItIdentityRoleDefinitionProvider` references tenant permissions via private `PermissionName` fields `TenantView = new("tenant.view")`, `TenantCreate = new("tenant.create")`, `TenantUpdate = new("tenant.update")`.

- [ ] **Step 1: Delete the role-created event handler**

```bash
rm src/backend/src/Services/Identity/ShopIt.Identity.Application/Roles/EventHandlers/RoleCreatedEventHandler.cs
```

`RoleCreatedDomainEvent` stays raised by `Role.Create` — it simply has no handler now.

- [ ] **Step 2: Remove the Tenants group from `ShopItIdentityPermissions`**

In `src/backend/src/Services/Identity/ShopIt.Identity.Domain/Permissions/ShopItIdentityPermissions.cs`:
- Add `using ShopIt.Framework.Domain.Permissions;` at the top.
- Delete the `TenantManagement` line from the `Groups` class (leaving `UserManagement` and `RoleManagement`).
- Delete the entire `public static class Tenants { ... }` block.

- [ ] **Step 3: Slim the Identity provider**

In `src/backend/src/Services/Identity/ShopIt.Identity.Domain/Permissions/ShopItIdentityPermissionDefinitionProvider.cs`:
- Add `using ShopIt.Framework.Domain.Permissions;` at the top.
- Add after the class opening brace:

```csharp
    /// <summary>Source-service name stamped on Identity's catalog entries.</summary>
    public const string SourceService = "Identity";
```

- Delete the entire `ShopItIdentityPermissions.Groups.TenantManagement.AddGroup(context, "Tenant Management") ... .AddPermission(...ActivateDeactivate...);` chain (the Tenant Management group block).

- [ ] **Step 4: Update role definitions to reference tenant permissions literally**

In `src/backend/src/Services/Identity/ShopIt.Identity.Domain/Roles/ShopItIdentityRoleDefinitionProvider.cs`:
- Add `using ShopIt.Framework.Domain.Permissions;` at the top (keep the Identity one).
- Add before `GetAll()`:

```csharp
    /// <summary>
    /// Permissions owned by the Tenancy service (defined in its own permission provider).
    /// Referenced by name here since the Identity domain cannot depend on the Tenancy
    /// service; the names must stay in sync with <c>ShopItTenancyPermissions</c>.
    /// </summary>
    private static readonly PermissionName TenantView = new("tenant.view");
    private static readonly PermissionName TenantCreate = new("tenant.create");
    private static readonly PermissionName TenantUpdate = new("tenant.update");
```

- Replace `ShopItIdentityPermissions.Tenants.View` → `TenantView`, `ShopItIdentityPermissions.Tenants.Create` → `TenantCreate`, `ShopItIdentityPermissions.Tenants.Update` → `TenantUpdate` (three references total).
- Update the Admin `DefaultPermissions: null` comment to: `// null = granted every permission as it is seeded into the catalog`.

- [ ] **Step 5: Re-document `GrantsAllPermissions`**

In `src/backend/src/Services/Identity/ShopIt.Identity.Domain/Roles/RoleDefinition.cs`, replace the property's XML doc:

```csharp
    /// <summary>
    /// When <c>true</c>, the role is granted every permission as it enters the permission
    /// catalog: seeding grants it everything currently in the catalog and, when the
    /// Identity service receives a newly published catalog, any permissions it does not
    /// already hold are granted to it (admin semantics).
    /// </summary>
    public bool GrantsAllPermissions => DefaultPermissions is null;
```

- [ ] **Step 6: Build to verify**

```bash
dotnet build src/Services/Identity/ShopIt.Identity.API/ShopIt.Identity.API.csproj -v q --nologo
```

Expected: `Build succeeded.` (no reference to `ShopItIdentityPermissions.Tenants` remains anywhere — run `grep -rn "ShopItIdentityPermissions.Tenants" src/backend/src` to confirm zero matches).

- [ ] **Step 7: Commit**

```bash
git add src/backend/src/Services/Identity/ShopIt.Identity.Application/Roles src/backend/src/Services/Identity/ShopIt.Identity.Domain
git commit -m "refactor(identity): remove new-role-to-admins handler and tenancy permissions from identity catalog"
```

---

### Task 9: Identity startup — inbox topic + `SeedPermissionCatalog`

**Files:**
- Modify: `src/backend/src/Services/Identity/ShopIt.Identity.API/Program.cs`

**Interfaces:**
- Consumes: `PermissionCatalogPublishedIntegrationEvent` (Task 3), `IPermissionCatalogSynchronizer` + `ShopItIdentityPermissionDefinitionProvider` (Tasks 7/8).
- Produces: startup behavior — Identity's own catalog is persisted (source `"Identity"`) before roles are seeded; the inbox subscribes to topic `PermissionCatalogPublishedIntegrationEvent`.

- [ ] **Step 1: Add the inbox topic**

In `src/backend/src/Services/Identity/ShopIt.Identity.API/Program.cs`, inside the `configureInbox` `Topics.AddRange` array, add `nameof(PermissionCatalogPublishedIntegrationEvent)` after the `nameof(TenantCreatedIntegrationEvent)` line.

- [ ] **Step 2: Add usings**

Add to the top using block:

```csharp
using ShopIt.Framework.Core.Events.Integration;
using ShopIt.Framework.Domain.Permissions;
using ShopIt.Identity.Persistence.Permissions;
```

(`ShopIt.Framework.Domain.Permissions` is needed by `EnsureRoleAsync`'s `IPermissionDefinitionProvider` reference; `ShopIt.Identity.Persistence.Permissions` for `IPermissionCatalogSynchronizer`.)

- [ ] **Step 3: Call `SeedPermissionCatalog` before `SeedRoles`**

Replace:

```csharp
        // Seed data
        await SeedRoles(scope.ServiceProvider);
        await SeedUsers(scope.ServiceProvider);
```

with:

```csharp
        // Seed data
        await SeedPermissionCatalog(scope.ServiceProvider);
        await SeedRoles(scope.ServiceProvider);
        await SeedUsers(scope.ServiceProvider);
```

- [ ] **Step 4: Add the `SeedPermissionCatalog` function**

Insert between `app.Run();` and `static async Task SeedRoles(IServiceProvider services)`:

```csharp
/// <summary>
/// Seeds the Identity service's own permissions into the persisted permission catalog.
/// Other services publish their catalogs via <see cref="PermissionCatalogPublishedIntegrationEvent"/>
/// (handled by <see cref="PermissionCatalogPublishedIntegrationEventHandler"/>), so Identity does
/// not need to be redeployed when another service changes its permissions.
/// </summary>
static async Task SeedPermissionCatalog(IServiceProvider services)
{
    var provider = services.GetRequiredService<ShopItIdentityPermissionDefinitionProvider>();
    var synchronizer = services.GetRequiredService<IPermissionCatalogSynchronizer>();

    await synchronizer.SynchronizeAsync(
        ShopItIdentityPermissionDefinitionProvider.SourceService,
        provider.GetGroups());
}
```

- [ ] **Step 5: Build to verify**

```bash
dotnet build src/Services/Identity/ShopIt.Identity.API/ShopIt.Identity.API.csproj -v q --nologo
```

Expected: `Build succeeded.`

- [ ] **Step 6: Commit**

```bash
git add src/backend/src/Services/Identity/ShopIt.Identity.API/Program.cs
git commit -m "feat(identity): seed own permission catalog at startup and subscribe to catalog events"
```

---

### Task 10: Tenancy — own permission provider + publish catalog at startup

**Files:**
- Create: `src/backend/src/Services/Tenancy/ShopIt.Tenancy.Domain/Permissions/ShopItTenancyPermissions.cs`
- Create: `src/backend/src/Services/Tenancy/ShopIt.Tenancy.Domain/Permissions/ShopItTenancyPermissionDefinitionProvider.cs`
- Modify: `src/backend/src/Services/Tenancy/ShopIt.Tenancy.Application/DependencyInjection.cs` (register provider)
- Modify: `src/backend/src/Services/Tenancy/ShopIt.Tenancy.Presentation/Tenants/TenantsModule.cs` (using swap)
- Delete: `src/backend/src/Services/Tenancy/ShopIt.Tenancy.Presentation/Authorization/ShopItTenancyPermissions.cs`
- Modify: `src/backend/src/Services/Tenancy/ShopIt.Tenancy.API/Program.cs` (publish step)
- Test: `src/backend/tests/ShopIt.Framework.Tests/Permissions/TenancyPermissionDefinitionProviderTests.cs`

**Interfaces:**
- Consumes: Task 2 provider types + fluent extensions; `IOutboxWriter.WriteAsync(IntegrationEvent, CancellationToken)` (Framework.Core); `PermissionCatalogPublishedIntegrationEvent` + DTOs (Task 3); `TenancyDbContext` (`SaveChangesAsync` to commit the outbox row).
- Produces:
  - `static class ShopItTenancyPermissions` — `PermissionGroupName TenantManagement = new("TenantManagement")`; `PermissionName View/Create/Update/Delete/ActivateDeactivate` = `tenant.view` / `tenant.create` / `tenant.update` / `tenant.delete` / `tenant.activate-deactivate`.
  - `class ShopItTenancyPermissionDefinitionProvider : IPermissionDefinitionProvider` — `public const string SourceService = "Tenancy"`; the 5 permissions in the Tenant Management group.

- [ ] **Step 1: Write the failing provider test**

`src/backend/tests/ShopIt.Framework.Tests/Permissions/TenancyPermissionDefinitionProviderTests.cs`:

```csharp
using ShopIt.Framework.Domain.Permissions;
using ShopIt.Tenancy.Domain.Permissions;

namespace ShopIt.Framework.Tests.Permissions;

public class TenancyPermissionDefinitionProviderTests
{
    [Fact]
    public void Defines_TenantManagementGroup_WithFivePermissions()
    {
        var provider = new ShopItTenancyPermissionDefinitionProvider();

        var group = Assert.Single(provider.GetGroups());
        Assert.Equal("TenantManagement", group.Name.Value);
        Assert.Equal("Tenant Management", group.DisplayName);
        Assert.Equal(5, group.Permissions.Count);

        Assert.Contains(group.Permissions, p => p.Name.Value == "tenant.view");
        Assert.Contains(group.Permissions, p => p.Name.Value == "tenant.create");
        Assert.Contains(group.Permissions, p => p.Name.Value == "tenant.update");
        Assert.Contains(group.Permissions, p => p.Name.Value == "tenant.delete");
        Assert.Contains(group.Permissions, p => p.Name.Value == "tenant.activate-deactivate");
    }
}
```

- [ ] **Step 2: Add the project reference + run test to verify it fails**

Add to `src/backend/tests/ShopIt.Framework.Tests/ShopIt.Framework.Tests.csproj`:

```xml
    <ProjectReference Include="..\..\src\Services\Tenancy\ShopIt.Tenancy.Domain\ShopIt.Tenancy.Domain.csproj" />
```

Then:

```bash
dotnet test tests/ShopIt.Framework.Tests/ShopIt.Framework.Tests.csproj -v q --nologo
```

Expected: FAIL — provider not found.

- [ ] **Step 3: Write the Tenancy permissions**

`src/backend/src/Services/Tenancy/ShopIt.Tenancy.Domain/Permissions/ShopItTenancyPermissions.cs`:

```csharp
using ShopIt.Framework.Domain.Permissions;

namespace ShopIt.Tenancy.Domain.Permissions;

/// <summary>
/// Canonical permission names for the Tenancy service, as <see cref="PermissionName"/>
/// value objects. These are defined <em>by</em> the Tenancy service (see
/// <see cref="ShopItTenancyPermissionDefinitionProvider"/>) and published to the Identity
/// service's permission catalog via an integration event, so the Identity project does not
/// need to be redeployed when Tenancy's permissions change.
/// </summary>
public static class ShopItTenancyPermissions
{
    public static readonly PermissionGroupName TenantManagement = new("TenantManagement");

    public static readonly PermissionName View = new("tenant.view");
    public static readonly PermissionName Create = new("tenant.create");
    public static readonly PermissionName Update = new("tenant.update");
    public static readonly PermissionName Delete = new("tenant.delete");
    public static readonly PermissionName ActivateDeactivate = new("tenant.activate-deactivate");
}
```

- [ ] **Step 4: Write the Tenancy provider**

`src/backend/src/Services/Tenancy/ShopIt.Tenancy.Domain/Permissions/ShopItTenancyPermissionDefinitionProvider.cs`:

```csharp
using ShopIt.Framework.Domain.Permissions;

namespace ShopIt.Tenancy.Domain.Permissions;

/// <summary>
/// Defines the Tenancy service's permissions (tenant management). Every microservice owns
/// its own permission definitions; the Tenancy API publishes this catalog to the Identity
/// service at startup, which persists it and grants any new permissions to the Admin role.
/// </summary>
public class ShopItTenancyPermissionDefinitionProvider : IPermissionDefinitionProvider
{
    /// <summary>Source-service name stamped on this catalog's entries in Identity.</summary>
    public const string SourceService = "Tenancy";

    private readonly IReadOnlyList<PermissionGroupDefinition> _groups;

    public ShopItTenancyPermissionDefinitionProvider()
    {
        var context = new PermissionDefinitionContext();
        Define(context);
        _groups = context.GetGroups();
    }

    public void Define(IPermissionDefinitionContext context)
    {
        ShopItTenancyPermissions.TenantManagement.AddGroup(context, "Tenant Management")
            .AddPermission(ShopItTenancyPermissions.View, "View tenants", "View tenants in the system.")
            .AddPermission(ShopItTenancyPermissions.Create, "Create tenants", "Create new tenants.")
            .AddPermission(ShopItTenancyPermissions.Update, "Update tenants", "Edit tenant information.")
            .AddPermission(ShopItTenancyPermissions.Delete, "Delete tenants", "Delete tenants.")
            .AddPermission(ShopItTenancyPermissions.ActivateDeactivate, "Activate/deactivate tenants", "Change the active state of tenants.");
    }

    public IReadOnlyList<PermissionGroupDefinition> GetGroups() => _groups;

    public IEnumerable<PermissionDefinition> GetAll() =>
        _groups.SelectMany(g => g.Permissions);
}
```

- [ ] **Step 5: Register the provider in Tenancy Application DI**

In `src/backend/src/Services/Tenancy/ShopIt.Tenancy.Application/DependencyInjection.cs`, add the using and registration:

```csharp
using ShopIt.Tenancy.Domain.Permissions;
```

and inside `AddApplication`:

```csharp
        // This service's permission catalog (published to Identity at startup).
        services.AddSingleton<ShopItTenancyPermissionDefinitionProvider>();
```

- [ ] **Step 6: Swap the Presentation constants**

- Delete `src/backend/src/Services/Tenancy/ShopIt.Tenancy.Presentation/Authorization/ShopItTenancyPermissions.cs` (the string-constant class).
- In `src/backend/src/Services/Tenancy/ShopIt.Tenancy.Presentation/Tenants/TenantsModule.cs`, add `using ShopIt.Tenancy.Domain.Permissions;`. The existing `RequirePermission(ShopItTenancyPermissions.View)` calls compile unchanged because `PermissionName` implicitly converts to `string`.

- [ ] **Step 7: Publish the catalog from the Tenancy API**

In `src/backend/src/Services/Tenancy/ShopIt.Tenancy.API/Program.cs`:
- Add usings:

```csharp
using ShopIt.Framework.Core.Events.Integration;
using ShopIt.Tenancy.Domain.Permissions;
```

- After the migration `using` block (before `app.Run();`), insert:

```csharp
// Publish this service's permission catalog so the Identity service can persist it and
// grant any new permissions to the Admin role. Runs on every startup (permission
// definitions only change when this service is redeployed), so Identity does not need to
// be redeployed when Tenancy's permissions change.
using (var scope = app.Services.CreateScope())
{
    var provider = scope.ServiceProvider.GetRequiredService<ShopItTenancyPermissionDefinitionProvider>();
    var outboxWriter = scope.ServiceProvider.GetRequiredService<IOutboxWriter>();
    var dbContext = scope.ServiceProvider.GetRequiredService<TenancyDbContext>();

    var groups = provider.GetGroups()
        .Select(g => new PermissionGroupDto(
            g.Name,
            g.DisplayName,
            g.Permissions
                .Select(p => new PermissionDefinitionDto(p.Name, p.DisplayName, p.Description))
                .ToList()))
        .ToList();

    await outboxWriter.WriteAsync(new PermissionCatalogPublishedIntegrationEvent(
        ShopItTenancyPermissionDefinitionProvider.SourceService,
        groups));

    await dbContext.SaveChangesAsync();
}
```

(`TenancyDbContext` is `ShopIt.Tenancy.Persistence.Data.TenancyDbContext` — already imported.)

- [ ] **Step 8: Run tests + build to verify**

```bash
dotnet test tests/ShopIt.Framework.Tests/ShopIt.Framework.Tests.csproj -v q --nologo
dotnet build src/Services/Tenancy/ShopIt.Tenancy.API/ShopIt.Tenancy.API.csproj -v q --nologo
```

Expected: PASS; `Build succeeded.`

- [ ] **Step 9: Commit**

```bash
git add src/backend/src/Services/Tenancy src/backend/tests/ShopIt.Framework.Tests/ShopIt.Framework.Tests.csproj src/backend/tests/ShopIt.Framework.Tests/Permissions
git commit -m "feat(tenancy): own permission definitions and publish catalog to identity at startup"
```

---

### Task 11: Full verification

**Files:**
- None (verification only; commit only if the run surfaces fixes).

**Interfaces:**
- Consumes: everything from Tasks 1–10.

- [ ] **Step 1: Run the full test suite**

```bash
dotnet test tests/ShopIt.Framework.Tests/ShopIt.Framework.Tests.csproj -v q --nologo
dotnet test tests/ShopIt.Identity.Tests/ShopIt.Identity.Tests.csproj -v q --nologo
```

Expected: all PASS.

- [ ] **Step 2: Build the whole solution**

```bash
dotnet build ShopIt.slnx -v q --nologo
```

Expected: `Build succeeded.` with no new warnings beyond the pre-existing `CS8981` (lowercase migration type `init`).

- [ ] **Step 3: Confirm no stale references remain**

```bash
grep -rn "ShopItIdentityPermissions.Tenants\|TenantManagement" src/backend/src/Services/Identity --include="*.cs" | grep -v Migrations
grep -rn "ShopItTenancyPermissions" src/backend/src/Services/Tenancy --include="*.cs"
```

Expected: no matches in Identity code (Tenancy matches only in `ShopIt.Tenancy.Domain/Permissions/*` and `TenantsModule.cs`).

- [ ] **Step 4: End-to-end smoke (requires Docker + Aspire — follow `src/frontend/.freebuff/run.md`)**

1. Start the backend: `cd src/backend && dotnet run --project ShopIt.AppHost --launch-profile https` (stop any existing stack first per run.md).
2. After all services report healthy, confirm Identity persisted Tenancy's catalog:
   ```bash
   curl -sk https://localhost:5001/api/identity/permissions | grep -o "tenant.view" | head -1
   ```
   Expected: `tenant.view` (Identity's own `user.*`/`role.*` are seeded in-process at startup; Tenancy's arrive via the event).
3. Confirm the Admin role holds the new permission (permission matrix requires an authenticated token; alternatively check the `PermissionCatalogEntries` + `AspNetRoleClaims` tables in the `identity-db` Postgres container for a `tenant.view` claim on the `Admin` role in the seeded tenant).
4. Verify the removed behavior: create a new role via the portal/API as an admin and confirm NO admin user is automatically assigned to it.

- [ ] **Step 5: Commit any stragglers**

```bash
git status
git add <any-remaining-files>
git commit -m "chore(identity): finalize permission catalog distribution"
```

---

## Self-Review

**1. Spec coverage**

| Spec requirement | Task |
|---|---|
| Remove "new role → assigned to all admin users" | Task 8, Step 1 (delete `RoleCreatedEventHandler`) |
| Remove admin's dynamic "all permissions" from Identity's hardcoded provider | Tasks 5–7 (DB-backed catalog replaces in-memory provider) + Task 8 |
| Admin auto-assigned newly seeded permissions | Task 6 (grant new names to Admin in every tenant) + Task 9 (startup seed) |
| Permission providers defined per microservice | Task 2 (shared framework types) + Task 10 (Tenancy's own provider) |
| Catalog stays stored in Identity (DB) | Task 4 (`PermissionCatalogEntries` + migration) |
| Integration event from services; Identity listens and updates DB + Admin role | Tasks 3, 7, 9, 10 |
| No Identity redeploy when a service changes permissions | Tasks 3–10 (event-driven upsert + grants) |
| Authentication/Notifications | Explicitly out of scope (no permissions today); pattern documented in Task 10 |

**2. Placeholder scan:** All steps contain concrete code or exact commands; no "TBD"/"implement later"/"similar to" references. The migration step relies on the `dotnet ef` generator (the command + expected schema are specified).

**3. Type consistency:** `PermissionName`/`PermissionGroupName` value objects, `PermissionDefinition`/`PermissionGroupDefinition` records, provider/context interfaces, the event + DTOs, `PermissionCatalogEntry.Create/Update`, `IPermissionCatalogSynchronizer.SynchronizeAsync`, and `SourceService` constants are defined once (Tasks 2/3/4/6) and referenced with identical signatures in later tasks (5/7/9/10) and tests.
