using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tankradar.MAUI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNearbySetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShowNearby",
                table: "UserSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShowNearby",
                table: "UserSettings");
        }
    }
}
