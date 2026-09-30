using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopIt.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIsStaticToRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsStatic",
                table: "AspNetRoles",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsStatic",
                table: "AspNetRoles");
        }
    }
}
