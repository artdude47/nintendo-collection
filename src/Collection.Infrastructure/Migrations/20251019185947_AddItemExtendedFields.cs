using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collection.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddItemExtendedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                table: "Items",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Developer",
                table: "Items",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Genre",
                table: "Items",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Publisher",
                table: "Items",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReleaseYear",
                table: "Items",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Barcode",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Developer",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Genre",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "Publisher",
                table: "Items");

            migrationBuilder.DropColumn(
                name: "ReleaseYear",
                table: "Items");
        }
    }
}
