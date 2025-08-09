using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prestige.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAlbumIdToRating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AlbumId",
                table: "Ratings",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlbumId",
                table: "Ratings");
        }
    }
}
