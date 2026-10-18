using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tankradar.MAUI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStationCustomName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomName",
                table: "Stations",
                type: "TEXT",
                maxLength: 60,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CustomName",
                table: "Stations");
        }
    }
}
