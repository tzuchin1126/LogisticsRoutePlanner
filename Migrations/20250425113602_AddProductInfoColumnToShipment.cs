﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LogisticsRoutePlanner.Migrations
{
    /// <inheritdoc />
    public partial class AddProductInfoColumnToShipment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProductInfo",
                table: "Shipments",
                type: "varchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductInfo",
                table: "Shipments");
        }
    }
}
