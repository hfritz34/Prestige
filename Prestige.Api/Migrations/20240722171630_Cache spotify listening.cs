using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prestige.Api.Migrations
{
    /// <inheritdoc />
    public partial class Cachespotifylistening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Album_Artist_ArtistId",
                table: "Album");

            migrationBuilder.DropForeignKey(
                name: "FK_Track_Album_AlbumId",
                table: "Track");

            migrationBuilder.DropForeignKey(
                name: "FK_Track_Artist_ArtistId",
                table: "Track");

            migrationBuilder.DropForeignKey(
                name: "FK_UserArtist_Artist_ArtistId",
                table: "UserArtist");

            migrationBuilder.DropForeignKey(
                name: "FK_UserTrack_Track_TrackId",
                table: "UserTrack");

            migrationBuilder.DropIndex(
                name: "IX_UserTrack_TrackId",
                table: "UserTrack");

            migrationBuilder.DropIndex(
                name: "IX_Track_ArtistId",
                table: "Track");

            migrationBuilder.DropIndex(
                name: "IX_Album_ArtistId",
                table: "Album");

            migrationBuilder.DropColumn(
                name: "TrackId",
                table: "UserTrack");

            migrationBuilder.DropColumn(
                name: "ArtistId",
                table: "Track");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Track");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Artist");

            migrationBuilder.DropColumn(
                name: "ArtistId",
                table: "Album");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Album");

            migrationBuilder.RenameColumn(
                name: "Duration",
                table: "Track",
                newName: "DurationMs");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Track",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "AlbumId",
                table: "Track",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Artist",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Album",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "AlbumArtist",
                columns: table => new
                {
                    AlbumId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ArtistsId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlbumArtist", x => new { x.AlbumId, x.ArtistsId });
                    table.ForeignKey(
                        name: "FK_AlbumArtist_Album_AlbumId",
                        column: x => x.AlbumId,
                        principalTable: "Album",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AlbumArtist_Artist_ArtistsId",
                        column: x => x.ArtistsId,
                        principalTable: "Artist",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArtistTrack",
                columns: table => new
                {
                    ArtistsId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TrackId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArtistTrack", x => new { x.ArtistsId, x.TrackId });
                    table.ForeignKey(
                        name: "FK_ArtistTrack_Artist_ArtistsId",
                        column: x => x.ArtistsId,
                        principalTable: "Artist",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArtistTrack_Track_TrackId",
                        column: x => x.TrackId,
                        principalTable: "Track",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Image",
                columns: table => new
                {
                    Url = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Height = table.Column<int>(type: "int", nullable: false),
                    Width = table.Column<int>(type: "int", nullable: false),
                    AlbumId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ArtistId = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Image", x => x.Url);
                    table.ForeignKey(
                        name: "FK_Image_Album_AlbumId",
                        column: x => x.AlbumId,
                        principalTable: "Album",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Image_Artist_ArtistId",
                        column: x => x.ArtistId,
                        principalTable: "Artist",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AlbumArtist_ArtistsId",
                table: "AlbumArtist",
                column: "ArtistsId");

            migrationBuilder.CreateIndex(
                name: "IX_ArtistTrack_TrackId",
                table: "ArtistTrack",
                column: "TrackId");

            migrationBuilder.CreateIndex(
                name: "IX_Image_AlbumId",
                table: "Image",
                column: "AlbumId");

            migrationBuilder.CreateIndex(
                name: "IX_Image_ArtistId",
                table: "Image",
                column: "ArtistId");

            migrationBuilder.AddForeignKey(
                name: "FK_Track_Album_AlbumId",
                table: "Track",
                column: "AlbumId",
                principalTable: "Album",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserArtist_Image_ArtistId",
                table: "UserArtist",
                column: "ArtistId",
                principalTable: "Image",
                principalColumn: "Url",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Track_Album_AlbumId",
                table: "Track");

            migrationBuilder.DropForeignKey(
                name: "FK_UserArtist_Image_ArtistId",
                table: "UserArtist");

            migrationBuilder.DropTable(
                name: "AlbumArtist");

            migrationBuilder.DropTable(
                name: "ArtistTrack");

            migrationBuilder.DropTable(
                name: "Image");

            migrationBuilder.RenameColumn(
                name: "DurationMs",
                table: "Track",
                newName: "Duration");

            migrationBuilder.AddColumn<string>(
                name: "TrackId",
                table: "UserTrack",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Track",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "AlbumId",
                table: "Track",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "ArtistId",
                table: "Track",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Track",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Artist",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Artist",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Album",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(255)",
                oldMaxLength: 255);

            migrationBuilder.AddColumn<string>(
                name: "ArtistId",
                table: "Album",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Album",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_UserTrack_TrackId",
                table: "UserTrack",
                column: "TrackId");

            migrationBuilder.CreateIndex(
                name: "IX_Track_ArtistId",
                table: "Track",
                column: "ArtistId");

            migrationBuilder.CreateIndex(
                name: "IX_Album_ArtistId",
                table: "Album",
                column: "ArtistId");

            migrationBuilder.AddForeignKey(
                name: "FK_Album_Artist_ArtistId",
                table: "Album",
                column: "ArtistId",
                principalTable: "Artist",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Track_Album_AlbumId",
                table: "Track",
                column: "AlbumId",
                principalTable: "Album",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Track_Artist_ArtistId",
                table: "Track",
                column: "ArtistId",
                principalTable: "Artist",
                principalColumn: "Id");

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
    }
}
