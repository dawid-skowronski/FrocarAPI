using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FrogCar.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRewiev : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Usuń istniejący klucz główny
            migrationBuilder.DropPrimaryKey(
                name: "PK_CarRentalReviews",
                table: "CarRentalReviews");

            // Usuń kolumnę Id, jeśli istnieje
            migrationBuilder.DropColumn(
                name: "Id",
                table: "CarRentalReviews");

            // Usuń kolumnę ReviewId, jeśli już istnieje (dla bezpieczeństwa, jeśli nie było rollbacku)
            migrationBuilder.DropColumn(
                name: "ReviewId",
                table: "CarRentalReviews");

            // Dodaj ReviewId jako kolumnę z IDENTITY
            migrationBuilder.AddColumn<int>(
                name: "ReviewId",
                table: "CarRentalReviews",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            // Ustaw ReviewId jako klucz główny
            migrationBuilder.AddPrimaryKey(
                name: "PK_CarRentalReviews",
                table: "CarRentalReviews",
                column: "ReviewId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Cofnij klucz główny
            migrationBuilder.DropPrimaryKey(
                name: "PK_CarRentalReviews",
                table: "CarRentalReviews");

            // Usuń kolumnę ReviewId
            migrationBuilder.DropColumn(
                name: "ReviewId",
                table: "CarRentalReviews");

            // Przywróć kolumnę Id z IDENTITY
            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "CarRentalReviews",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            // Ustaw Id jako klucz główny
            migrationBuilder.AddPrimaryKey(
                name: "PK_CarRentalReviews",
                table: "CarRentalReviews",
                column: "Id");
        }


    }
};
