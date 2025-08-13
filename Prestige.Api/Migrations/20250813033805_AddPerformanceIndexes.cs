using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prestige.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add composite index for rating queries by user, item type, and category
            migrationBuilder.CreateIndex(
                name: "IX_Ratings_UserId_ItemType_CategoryId",
                table: "Ratings",
                columns: new[] { "UserId", "ItemType", "CategoryId" })
                .Annotation("SqlServer:Include", new[] { "PersonalScore", "Position", "AlbumId", "CreatedAt" });

            // Add composite index for rating queries by user, item type, and album
            migrationBuilder.CreateIndex(
                name: "IX_Ratings_UserId_ItemType_AlbumId",
                table: "Ratings",
                columns: new[] { "UserId", "ItemType", "AlbumId" })
                .Annotation("SqlServer:Include", new[] { "PersonalScore", "Position", "CategoryId" });

            // Add index for user track lookups
            migrationBuilder.CreateIndex(
                name: "IX_UserTracks_UserId_TrackId",
                table: "UserTracks",
                columns: new[] { "UserId", "TrackId" })
                .Annotation("SqlServer:Include", new[] { "PlayCount", "AddedAt" });

            // Add index for user album lookups
            migrationBuilder.CreateIndex(
                name: "IX_UserAlbums_UserId_AlbumId",
                table: "UserAlbums",
                columns: new[] { "UserId", "AlbumId" })
                .Annotation("SqlServer:Include", new[] { "PlayCount", "AddedAt" });

            // Add index for user artist lookups
            migrationBuilder.CreateIndex(
                name: "IX_UserArtists_UserId_ArtistId",
                table: "UserArtists",
                columns: new[] { "UserId", "ArtistId" })
                .Annotation("SqlServer:Include", new[] { "PlayCount", "AddedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Ratings_UserId_ItemType_CategoryId",
                table: "Ratings");

            migrationBuilder.DropIndex(
                name: "IX_Ratings_UserId_ItemType_AlbumId",
                table: "Ratings");

            migrationBuilder.DropIndex(
                name: "IX_UserTracks_UserId_TrackId",
                table: "UserTracks");

            migrationBuilder.DropIndex(
                name: "IX_UserAlbums_UserId_AlbumId",
                table: "UserAlbums");

            migrationBuilder.DropIndex(
                name: "IX_UserArtists_UserId_ArtistId",
                table: "UserArtists");
        }
    }
}
