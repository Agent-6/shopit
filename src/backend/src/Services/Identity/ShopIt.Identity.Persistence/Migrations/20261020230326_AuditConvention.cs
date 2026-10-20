using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopIt.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AuditConvention : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Audit fields adopt the framework convention (ADR-0004): CreatedOn/CreatedBy,
            // LastModifiedOn/LastModifiedBy, DeletedOn/DeletedBy, with Guid? actors.
            //
            // CreatedAt is renamed, not dropped, so existing rows keep their creation instant.
            // CreatedBy cannot be converted — existing rows hold the string "system", which is not a
            // Guid — so the column is replaced. Retro-fitting audit values is out of scope for #31.
            migrationBuilder.RenameColumn(
                name: "LastModifiedAt",
                table: "AspNetUsers",
                newName: "LastModifiedOn");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "LastModifiedOn",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "AspNetUsers",
                newName: "CreatedOn");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "AspNetUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedOn",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "AspNetUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastModifiedBy",
                table: "AspNetUsers",
                type: "uuid",
                nullable: true);

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "AspNetRoles",
                newName: "CreatedOn");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "AspNetRoles");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "AspNetRoles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastModifiedOn",
                table: "AspNetRoles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LastModifiedBy",
                table: "AspNetRoles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedOn",
                table: "AspNetRoles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "AspNetRoles",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CreatedBy", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "DeletedOn", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "DeletedBy", table: "AspNetUsers");
            migrationBuilder.DropColumn(name: "LastModifiedBy", table: "AspNetUsers");

            migrationBuilder.RenameColumn(
                name: "LastModifiedOn",
                table: "AspNetUsers",
                newName: "LastModifiedAt");

            migrationBuilder.AlterColumn<DateTime>(
                name: "LastModifiedAt",
                table: "AspNetUsers",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.RenameColumn(
                name: "CreatedOn",
                table: "AspNetUsers",
                newName: "CreatedAt");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "AspNetUsers",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.DropColumn(name: "CreatedBy", table: "AspNetRoles");
            migrationBuilder.DropColumn(name: "LastModifiedOn", table: "AspNetRoles");
            migrationBuilder.DropColumn(name: "LastModifiedBy", table: "AspNetRoles");
            migrationBuilder.DropColumn(name: "DeletedOn", table: "AspNetRoles");
            migrationBuilder.DropColumn(name: "DeletedBy", table: "AspNetRoles");

            migrationBuilder.RenameColumn(
                name: "CreatedOn",
                table: "AspNetRoles",
                newName: "CreatedAt");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "AspNetRoles",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
