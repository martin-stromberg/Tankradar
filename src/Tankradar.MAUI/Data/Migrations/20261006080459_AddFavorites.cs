using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tankradar.MAUI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFavorites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FavoriteGroups",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false, collation: "NOCASE"),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FavoriteGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FavoriteEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GroupId = table.Column<long>(type: "INTEGER", nullable: false),
                    StationId = table.Column<string>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", nullable: true),
                    Priority = table.Column<string>(type: "TEXT", nullable: false),
                    AddedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FavoriteEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FavoriteEntries_FavoriteGroups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "FavoriteGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FavoriteEntries_Stations_StationId",
                        column: x => x.StationId,
                        principalTable: "Stations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteEntries_GroupId_StationId",
                table: "FavoriteEntries",
                columns: new[] { "GroupId", "StationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteEntries_StationId",
                table: "FavoriteEntries",
                column: "StationId");

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteGroups_Name",
                table: "FavoriteGroups",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FavoriteEntries");

            migrationBuilder.DropTable(
                name: "FavoriteGroups");
        }
    }
}
