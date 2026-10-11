using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tankradar.MAUI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTankbookEntryDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TankbookEntries_VehicleId",
                table: "TankbookEntries");

            migrationBuilder.AddColumn<bool>(
                name: "IsPartial",
                table: "TankbookEntries",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Kind",
                table: "TankbookEntries",
                type: "TEXT",
                nullable: false,
                defaultValue: "Fuel");

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "TankbookEntries",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OccurredAt",
                table: "TankbookEntries",
                type: "TEXT",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "OdometerKm",
                table: "TankbookEntries",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "Quantity",
                table: "TankbookEntries",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptPhotoFileName",
                table: "TankbookEntries",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StationId",
                table: "TankbookEntries",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StationName",
                table: "TankbookEntries",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAmount",
                table: "TankbookEntries",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "TankbookEntries",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            // Datenübernahme für Bestandszeilen aus Schritt 12: Zeitpunkt = Anlagezeitpunkt, Kilometerstand = Start-Kilometerstand des Fahrzeugs,
            // Vorgänge von Elektrofahrzeugen sind Ladevorgänge. Alle übrigen Spalten behalten ihre Standardwerte.
            migrationBuilder.Sql("UPDATE \"TankbookEntries\" SET \"OccurredAt\" = \"CreatedUtc\";");
            migrationBuilder.Sql(
                "UPDATE \"TankbookEntries\" SET \"OdometerKm\" = COALESCE((SELECT \"StartOdometerKm\" FROM \"Vehicles\" WHERE \"Vehicles\".\"Id\" = \"TankbookEntries\".\"VehicleId\"), 0);");
            migrationBuilder.Sql(
                "UPDATE \"TankbookEntries\" SET \"Kind\" = 'Electricity' WHERE \"VehicleId\" IN (SELECT \"Id\" FROM \"Vehicles\" WHERE \"FuelKind\" = 'Electric');");

            migrationBuilder.CreateIndex(
                name: "IX_TankbookEntries_VehicleId_OccurredAt",
                table: "TankbookEntries",
                columns: new[] { "VehicleId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TankbookEntries_VehicleId_OccurredAt",
                table: "TankbookEntries");

            migrationBuilder.DropColumn(
                name: "IsPartial",
                table: "TankbookEntries");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "TankbookEntries");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "TankbookEntries");

            migrationBuilder.DropColumn(
                name: "OccurredAt",
                table: "TankbookEntries");

            migrationBuilder.DropColumn(
                name: "OdometerKm",
                table: "TankbookEntries");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "TankbookEntries");

            migrationBuilder.DropColumn(
                name: "ReceiptPhotoFileName",
                table: "TankbookEntries");

            migrationBuilder.DropColumn(
                name: "StationId",
                table: "TankbookEntries");

            migrationBuilder.DropColumn(
                name: "StationName",
                table: "TankbookEntries");

            migrationBuilder.DropColumn(
                name: "TotalAmount",
                table: "TankbookEntries");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "TankbookEntries");

            migrationBuilder.CreateIndex(
                name: "IX_TankbookEntries_VehicleId",
                table: "TankbookEntries",
                column: "VehicleId");
        }
    }
}
