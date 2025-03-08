using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FrogCar.Migrations
{
    public partial class ListingIdToMapPoints : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ListingId",
                table: "MapPoints",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_MapPoints_ListingId",
                table: "MapPoints",
                column: "ListingId");

            // Zmiana z Cascade na SetNull
            migrationBuilder.AddForeignKey(
                name: "FK_MapPoints_CarListing_ListingId",
                table: "MapPoints",
                column: "ListingId",
                principalTable: "CarListing",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);  // Zmiana tutaj
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MapPoints_CarListing_ListingId",
                table: "MapPoints");

            migrationBuilder.DropIndex(
                name: "IX_MapPoints_ListingId",
                table: "MapPoints");

            migrationBuilder.DropColumn(
                name: "ListingId",
                table: "MapPoints");
        }
    }
}
