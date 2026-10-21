using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ShopIt.Tenancy.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The default is 'EUR', not the empty string the scaffolder produced: this column is NOT NULL and
            // every tenant that already exists has been pricing in the platform-wide EUR setting, so 'EUR' is the
            // only value that leaves an existing price meaning what it meant (#84, ADR-0006). An empty string would
            // rewrite every tenant's unit of money to nothing.
            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "Tenants",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "EUR");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Dropping the column loses the tenants' units of money; it exists so the migration can be rolled back
            // in development, not as a supported way to un-decide #84.
            migrationBuilder.DropColumn(
                name: "Currency",
                table: "Tenants");
        }
    }
}
