using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prestige.Api.Migrations
{
    /// <inheritdoc />
    public partial class changeUserObjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserArtist_Image_ArtistId",
                table: "UserArtist");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserTrack",
                table: "UserTrack");

            migrationBuilder.DropIndex(
                name: "IX_UserTrack_UserId",
                table: "UserTrack");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserArtist",
                table: "UserArtist");

            migrationBuilder.DropIndex(
                name: "IX_UserArtist_UserId",
                table: "UserArtist");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserAlbum",
                table: "UserAlbum");

            migrationBuilder.DropIndex(
                name: "IX_UserAlbum_UserId",
                table: "UserAlbum");

            migrationBuilder.DropColumn(
                name: "IsFavorite",
                table: "UserTrack");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "UserArtist");

            migrationBuilder.DropColumn(
                name: "IsFavorite",
                table: "UserArtist");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "UserAlbum");

            migrationBuilder.DropColumn(
                name: "IsFavorite",
                table: "UserAlbum");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "UserTrack",
                newName: "TrackId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserTrack",
                table: "UserTrack",
                columns: new[] { "UserId", "TrackId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserArtist",
                table: "UserArtist",
                columns: new[] { "UserId", "ArtistId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserAlbum",
                table: "UserAlbum",
                columns: new[] { "UserId", "AlbumId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserTrack_TrackId",
                table: "UserTrack",
                column: "TrackId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserArtist_Artist_ArtistId",
                table: "UserArtist",
                column: "ArtistId",
                principalTable: "Artist",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserTrack_Track_TrackId",
                table: "UserTrack",
                column: "TrackId",
                principalTable: "Track",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserArtist_Artist_ArtistId",
                table: "UserArtist");

            migrationBuilder.DropForeignKey(
                name: "FK_UserTrack_Track_TrackId",
                table: "UserTrack");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserTrack",
                table: "UserTrack");

            migrationBuilder.DropIndex(
                name: "IX_UserTrack_TrackId",
                table: "UserTrack");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserArtist",
                table: "UserArtist");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserAlbum",
                table: "UserAlbum");

            migrationBuilder.RenameColumn(
                name: "TrackId",
                table: "UserTrack",
                newName: "Id");

            migrationBuilder.AddColumn<bool>(
                name: "IsFavorite",
                table: "UserTrack",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Id",
                table: "UserArtist",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsFavorite",
                table: "UserArtist",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Id",
                table: "UserAlbum",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsFavorite",
                table: "UserAlbum",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserTrack",
                table: "UserTrack",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserArtist",
                table: "UserArtist",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserAlbum",
                table: "UserAlbum",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_UserTrack_UserId",
                table: "UserTrack",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserArtist_UserId",
                table: "UserArtist",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAlbum_UserId",
                table: "UserAlbum",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserArtist_Image_ArtistId",
                table: "UserArtist",
                column: "ArtistId",
                principalTable: "Image",
                principalColumn: "Url",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
