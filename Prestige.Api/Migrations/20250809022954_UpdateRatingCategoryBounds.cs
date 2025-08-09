using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prestige.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRatingCategoryBounds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "RatingCategories",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.UpdateData(
                table: "RatingCategories",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "MaxScore", "MinScore" },
                values: new object[] { 10.0m, 6.8m });

            migrationBuilder.UpdateData(
                table: "RatingCategories",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ColorHex", "MaxScore", "MinScore" },
                values: new object[] { "#eab308", 6.7m, 3.4m });

            migrationBuilder.UpdateData(
                table: "RatingCategories",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ColorHex", "MaxScore", "MinScore", "Name" },
                values: new object[] { "#ef4444", 3.3m, 0.0m, "Disliked" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "RatingCategories",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "MaxScore", "MinScore" },
                values: new object[] { 100.0m, 75.0m });

            migrationBuilder.UpdateData(
                table: "RatingCategories",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "ColorHex", "MaxScore", "MinScore" },
                values: new object[] { "#84cc16", 74.99m, 50.0m });

            migrationBuilder.UpdateData(
                table: "RatingCategories",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "ColorHex", "MaxScore", "MinScore", "Name" },
                values: new object[] { "#eab308", 49.99m, 25.0m, "Okay" });

            migrationBuilder.InsertData(
                table: "RatingCategories",
                columns: new[] { "Id", "ColorHex", "DisplayOrder", "MaxScore", "MinScore", "Name" },
                values: new object[] { 4, "#ef4444", 4, 24.99m, 0.0m, "Disliked" });
        }
    }
}
