using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Collection.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAmiiboPlatform : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Platforms",
                columns: new[] { "Id", "Manufacturer", "Name", "Notes" },
                values: new object[] { 13, "Nintendo", "Amiibo", null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Platforms",
                keyColumn: "Id",
                keyValue: 13);
        }
    }
}
