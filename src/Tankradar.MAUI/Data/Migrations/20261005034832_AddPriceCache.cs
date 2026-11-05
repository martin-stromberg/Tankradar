using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tankradar.MAUI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceCache : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Stations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Brand = table.Column<string>(type: "TEXT", nullable: true),
                    Street = table.Column<string>(type: "TEXT", nullable: true),
                    HouseNumber = table.Column<string>(type: "TEXT", nullable: true),
                    PostCode = table.Column<string>(type: "TEXT", nullable: true),
                    Place = table.Column<string>(type: "TEXT", nullable: true),
                    Latitude = table.Column<double>(type: "REAL", nullable: false),
                    Longitude = table.Column<double>(type: "REAL", nullable: false),
                    WholeDay = table.Column<bool>(type: "INTEGER", nullable: true),
                    OpeningTimesJson = table.Column<string>(type: "TEXT", nullable: true),
                    DetailsUpdatedUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PriceEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StationId = table.Column<string>(type: "TEXT", nullable: false),
                    FuelTypeKey = table.Column<string>(type: "TEXT", nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", nullable: false),
                    RetrievedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PriceEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PriceEntries_Stations_StationId",
                        column: x => x.StationId,
                        principalTable: "Stations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PriceEntries_StationId_FuelTypeKey_RetrievedUtc",
                table: "PriceEntries",
                columns: new[] { "StationId", "FuelTypeKey", "RetrievedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Stations_Latitude_Longitude",
                table: "Stations",
                columns: new[] { "Latitude", "Longitude" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PriceEntries");

            migrationBuilder.DropTable(
                name: "Stations");
        }
    }
}
