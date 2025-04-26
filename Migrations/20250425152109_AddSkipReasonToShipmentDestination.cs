using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsRoutePlanner.Migrations
{
    /// <inheritdoc />
    public partial class AddSkipReasonToShipmentDestination : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SkipReason",
                table: "ShipmentDestinations",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SkipReason",
                table: "ShipmentDestinations");
        }
    }
}
