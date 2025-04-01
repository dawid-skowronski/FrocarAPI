using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FrogCar.Migrations
{
    /// <inheritdoc />
    public partial class RepairRent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_carRentals_CarListing_CarListingId",
                table: "carRentals");

            migrationBuilder.DropForeignKey(
                name: "FK_carRentals_Users_UserId",
                table: "carRentals");

            migrationBuilder.DropPrimaryKey(
                name: "PK_carRentals",
                table: "carRentals");

            migrationBuilder.RenameTable(
                name: "carRentals",
                newName: "CarRentals");

            migrationBuilder.RenameIndex(
                name: "IX_carRentals_UserId",
                table: "CarRentals",
                newName: "IX_CarRentals_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_carRentals_CarListingId",
                table: "CarRentals",
                newName: "IX_CarRentals_CarListingId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_CarRentals",
                table: "CarRentals",
                column: "CarRentalId");

            migrationBuilder.AddForeignKey(
                name: "FK_CarRentals_CarListing_CarListingId",
                table: "CarRentals",
                column: "CarListingId",
                principalTable: "CarListing",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CarRentals_Users_UserId",
                table: "CarRentals",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CarRentals_CarListing_CarListingId",
                table: "CarRentals");

            migrationBuilder.DropForeignKey(
                name: "FK_CarRentals_Users_UserId",
                table: "CarRentals");

            migrationBuilder.DropPrimaryKey(
                name: "PK_CarRentals",
                table: "CarRentals");

            migrationBuilder.RenameTable(
                name: "CarRentals",
                newName: "carRentals");

            migrationBuilder.RenameIndex(
                name: "IX_CarRentals_UserId",
                table: "carRentals",
                newName: "IX_carRentals_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_CarRentals_CarListingId",
                table: "carRentals",
                newName: "IX_carRentals_CarListingId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_carRentals",
                table: "carRentals",
                column: "CarRentalId");

            migrationBuilder.AddForeignKey(
                name: "FK_carRentals_CarListing_CarListingId",
                table: "carRentals",
                column: "CarListingId",
                principalTable: "CarListing",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_carRentals_Users_UserId",
                table: "carRentals",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
