using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collection.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddItemExtendedSearchAndKind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Kind",
                table: "Items",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Kind",
                table: "Items");
        }
    }
}
