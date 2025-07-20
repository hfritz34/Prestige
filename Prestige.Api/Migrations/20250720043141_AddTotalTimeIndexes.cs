using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prestige.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTotalTimeIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UserTrack_TotalTime",
                table: "UserTrack",
                column: "TotalTime");

            migrationBuilder.CreateIndex(
                name: "IX_UserTrack_UserId_TotalTime",
                table: "UserTrack",
                columns: new[] { "UserId", "TotalTime" });

            migrationBuilder.CreateIndex(
                name: "IX_UserArtist_TotalTime",
                table: "UserArtist",
                column: "TotalTime");

            migrationBuilder.CreateIndex(
                name: "IX_UserArtist_UserId_TotalTime",
                table: "UserArtist",
                columns: new[] { "UserId", "TotalTime" });

            migrationBuilder.CreateIndex(
                name: "IX_UserAlbum_TotalTime",
                table: "UserAlbum",
                column: "TotalTime");

            migrationBuilder.CreateIndex(
                name: "IX_UserAlbum_UserId_TotalTime",
                table: "UserAlbum",
                columns: new[] { "UserId", "TotalTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserTrack_TotalTime",
                table: "UserTrack");

            migrationBuilder.DropIndex(
                name: "IX_UserTrack_UserId_TotalTime",
                table: "UserTrack");

            migrationBuilder.DropIndex(
                name: "IX_UserArtist_TotalTime",
                table: "UserArtist");

            migrationBuilder.DropIndex(
                name: "IX_UserArtist_UserId_TotalTime",
                table: "UserArtist");

            migrationBuilder.DropIndex(
                name: "IX_UserAlbum_TotalTime",
                table: "UserAlbum");

            migrationBuilder.DropIndex(
                name: "IX_UserAlbum_UserId_TotalTime",
                table: "UserAlbum");
        }
    }
}
