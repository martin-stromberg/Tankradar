using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tankradar.MAUI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFuelTypeSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FuelTypeSettings",
                columns: table => new
                {
                    FuelTypeKey = table.Column<string>(type: "TEXT", nullable: false),
                    SortOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsSelected = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuelTypeSettings", x => x.FuelTypeKey);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FuelTypeSettings");
        }
    }
}
