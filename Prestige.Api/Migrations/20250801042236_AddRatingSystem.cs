using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Prestige.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRatingSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PersonalRatingScore",
                table: "UserTrack",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RatingPosition",
                table: "UserTrack",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PersonalRatingScore",
                table: "UserArtist",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RatingPosition",
                table: "UserArtist",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PersonalRatingScore",
                table: "UserAlbum",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RatingPosition",
                table: "UserAlbum",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RatingCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MinScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    MaxScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    ColorHex = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RatingCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RatingComparisons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ItemId1 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ItemId2 = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ItemType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    WinnerId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ComparisonDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RatingComparisons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RatingComparisons_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ratings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ItemId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ItemType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    PersonalScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ratings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ratings_RatingCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "RatingCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Ratings_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "RatingCategories",
                columns: new[] { "Id", "ColorHex", "DisplayOrder", "MaxScore", "MinScore", "Name" },
                values: new object[,]
                {
                    { 1, "#22c55e", 1, 100.0m, 75.0m, "Loved" },
                    { 2, "#84cc16", 2, 74.99m, 50.0m, "Liked" },
                    { 3, "#eab308", 3, 49.99m, 25.0m, "Okay" },
                    { 4, "#ef4444", 4, 24.99m, 0.0m, "Disliked" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_RatingCategories_DisplayOrder",
                table: "RatingCategories",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_RatingCategories_Name",
                table: "RatingCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RatingComparisons_ComparisonDate",
                table: "RatingComparisons",
                column: "ComparisonDate");

            migrationBuilder.CreateIndex(
                name: "IX_RatingComparisons_UserId_ItemType_ComparisonDate",
                table: "RatingComparisons",
                columns: new[] { "UserId", "ItemType", "ComparisonDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_CategoryId",
                table: "Ratings",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_ItemType",
                table: "Ratings",
                column: "ItemType");

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_PersonalScore",
                table: "Ratings",
                column: "PersonalScore");

            migrationBuilder.CreateIndex(
                name: "IX_Ratings_UserId_ItemId_ItemType",
                table: "Ratings",
                columns: new[] { "UserId", "ItemId", "ItemType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RatingComparisons");

            migrationBuilder.DropTable(
                name: "Ratings");

            migrationBuilder.DropTable(
                name: "RatingCategories");

            migrationBuilder.DropColumn(
                name: "PersonalRatingScore",
                table: "UserTrack");

            migrationBuilder.DropColumn(
                name: "RatingPosition",
                table: "UserTrack");

            migrationBuilder.DropColumn(
                name: "PersonalRatingScore",
                table: "UserArtist");

            migrationBuilder.DropColumn(
                name: "RatingPosition",
                table: "UserArtist");

            migrationBuilder.DropColumn(
                name: "PersonalRatingScore",
                table: "UserAlbum");

            migrationBuilder.DropColumn(
                name: "RatingPosition",
                table: "UserAlbum");
        }
    }
}
