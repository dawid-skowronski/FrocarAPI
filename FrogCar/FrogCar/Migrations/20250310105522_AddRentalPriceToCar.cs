using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FrogCar.Migrations
{
    /// <inheritdoc />
    public partial class AddRentalPriceToCar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "RentalPricePerDay",
                table: "CarListing",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RentalPricePerDay",
                table: "CarListing");
        }
    }
}
