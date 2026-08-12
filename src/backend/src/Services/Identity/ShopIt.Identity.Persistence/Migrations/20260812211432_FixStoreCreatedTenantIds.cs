using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopIt.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixStoreCreatedTenantIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rows created through the default ASP.NET Core Identity stores before the
            // tenant-aware stores were introduced left TenantId as Guid.Empty. Stamp the
            // tenant of the owning role/user so tenant-scoped queries can see them again.

            migrationBuilder.Sql("""
                UPDATE "AspNetRoleClaims" rc
                SET "TenantId" = r."TenantId"
                FROM "AspNetRoles" r
                WHERE rc."RoleId" = r."Id";
                """);

            migrationBuilder.Sql("""
                UPDATE "AspNetUserClaims" uc
                SET "TenantId" = u."TenantId"
                FROM "AspNetUsers" u
                WHERE uc."UserId" = u."Id";
                """);

            migrationBuilder.Sql("""
                UPDATE "AspNetUserRoles" ur
                SET "TenantId" = u."TenantId"
                FROM "AspNetUsers" u
                WHERE ur."UserId" = u."Id";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally not reverted — the tenant ids are the correct, canonical values.
        }
    }
}
