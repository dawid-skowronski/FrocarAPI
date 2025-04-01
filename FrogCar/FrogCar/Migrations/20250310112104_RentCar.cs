using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FrogCar.Migrations
{
    /// <inheritdoc />
    public partial class RentCar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "carRentals",
                columns: table => new
                {
                    CarRentalId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CarListingId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RentalStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RentalEndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RentalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RentalStatus = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_carRentals", x => x.CarRentalId);
                    table.ForeignKey(
                        name: "FK_carRentals_CarListing_CarListingId",
                        column: x => x.CarListingId,
                        principalTable: "CarListing",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_carRentals_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_carRentals_CarListingId",
                table: "carRentals",
                column: "CarListingId");

            migrationBuilder.CreateIndex(
                name: "IX_carRentals_UserId",
                table: "carRentals",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "carRentals");
        }
    }
}
