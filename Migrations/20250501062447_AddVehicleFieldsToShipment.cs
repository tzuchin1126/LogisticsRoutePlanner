using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsRoutePlanner.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleFieldsToShipment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VehicleNumber",
                table: "Shipments",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "VehicleType",
                table: "Shipments",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "SkipReason",
                table: "ShipmentDestinations",
                type: "longtext",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VehicleNumber",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "VehicleType",
                table: "Shipments");

            migrationBuilder.UpdateData(
                table: "ShipmentDestinations",
                keyColumn: "SkipReason",
                keyValue: null,
                column: "SkipReason",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "SkipReason",
                table: "ShipmentDestinations",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "longtext",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
