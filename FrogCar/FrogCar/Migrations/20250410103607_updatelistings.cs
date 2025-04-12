using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FrogCar.Migrations
{
    /// <inheritdoc />
    public partial class updatelistings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "CarListing",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "CarListing");
        }
    }
}
