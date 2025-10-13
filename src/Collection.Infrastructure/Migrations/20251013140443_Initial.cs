using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Collection.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Platforms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Manufacturer = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Platforms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PlatformId = table.Column<int>(type: "INTEGER", nullable: false),
                    Region = table.Column<string>(type: "TEXT", nullable: false),
                    Condition = table.Column<int>(type: "INTEGER", nullable: false),
                    HasBox = table.Column<bool>(type: "INTEGER", nullable: false),
                    HasManual = table.Column<bool>(type: "INTEGER", nullable: false),
                    PurchasePrice = table.Column<decimal>(type: "TEXT", nullable: true),
                    PurchaseDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    EstimatedValue = table.Column<decimal>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Items_Platforms_PlatformId",
                        column: x => x.PlatformId,
                        principalTable: "Platforms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Platforms",
                columns: new[] { "Id", "Manufacturer", "Name", "Notes" },
                values: new object[,]
                {
                    { 1, "Nintendo", "NES", null },
                    { 2, "Nintendo", "SNES", null },
                    { 3, "Nintendo", "N64", null },
                    { 4, "Nintendo", "GameCube", null },
                    { 5, "Nintendo", "Wii", null },
                    { 6, "Nintendo", "Wii U", null },
                    { 7, "Nintendo", "Switch", null },
                    { 8, "Nintendo", "Game Boy", null },
                    { 9, "Nintendo", "Game Boy Color", null },
                    { 10, "Nintendo", "Game Boy Advance", null },
                    { 11, "Nintendo", "Nintendo DS", null },
                    { 12, "Nintendo", "Nintendo 3DS", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Items_PlatformId",
                table: "Items",
                column: "PlatformId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_UserId_PlatformId_Title",
                table: "Items",
                columns: new[] { "UserId", "PlatformId", "Title" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "Platforms");
        }
    }
}
