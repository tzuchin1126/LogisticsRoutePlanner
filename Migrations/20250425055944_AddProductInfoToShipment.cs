using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsRoutePlanner.Migrations
{
    /// <inheritdoc />
    public partial class AddProductInfoToShipment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProductInfo",
                table: "ShipmentDestinations",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductInfo",
                table: "ShipmentDestinations");
        }
    }
}
