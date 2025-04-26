using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsRoutePlanner.Migrations
{
    /// <inheritdoc />
    public partial class AddShipmentDateToShipment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ShipmentDate",
                table: "Shipments",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShipmentDate",
                table: "Shipments");
        }
    }
}
