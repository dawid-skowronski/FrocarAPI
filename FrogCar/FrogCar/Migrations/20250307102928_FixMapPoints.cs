﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FrogCar.Migrations
{
    /// <inheritdoc />
    public partial class FixMapPoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MapPoints_CarListing_ListingId",
                table: "MapPoints");

            migrationBuilder.AddForeignKey(
                name: "FK_MapPoints_CarListing_ListingId",
                table: "MapPoints",
                column: "ListingId",
                principalTable: "CarListing",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MapPoints_CarListing_ListingId",
                table: "MapPoints");

            migrationBuilder.AddForeignKey(
                name: "FK_MapPoints_CarListing_ListingId",
                table: "MapPoints",
                column: "ListingId",
                principalTable: "CarListing",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
