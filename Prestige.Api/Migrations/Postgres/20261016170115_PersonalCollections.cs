using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prestige.Api.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class PersonalCollections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CollectionEntry",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ItemType = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    ItemId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TrackId = table.Column<string>(type: "text", nullable: true),
                    AlbumId = table.Column<string>(type: "text", nullable: true),
                    ArtistId = table.Column<string>(type: "text", nullable: true),
                    IsFavorite = table.Column<bool>(type: "boolean", nullable: false),
                    IsPinned = table.Column<bool>(type: "boolean", nullable: false),
                    SavedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionEntry", x => new { x.UserId, x.ItemType, x.ItemId });
                    table.CheckConstraint("CK_CollectionEntry_Item", "(\"ItemType\" = 'track' AND \"TrackId\" IS NOT NULL AND \"TrackId\" = \"ItemId\" AND \"AlbumId\" IS NULL AND \"ArtistId\" IS NULL) OR\n(\"ItemType\" = 'album' AND \"AlbumId\" IS NOT NULL AND \"AlbumId\" = \"ItemId\" AND \"TrackId\" IS NULL AND \"ArtistId\" IS NULL) OR\n(\"ItemType\" = 'artist' AND \"ArtistId\" IS NOT NULL AND \"ArtistId\" = \"ItemId\" AND \"TrackId\" IS NULL AND \"AlbumId\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_CollectionEntry_Album_AlbumId",
                        column: x => x.AlbumId,
                        principalTable: "Album",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CollectionEntry_Artist_ArtistId",
                        column: x => x.ArtistId,
                        principalTable: "Artist",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CollectionEntry_Track_TrackId",
                        column: x => x.TrackId,
                        principalTable: "Track",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CollectionEntry_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollectionEntry_AlbumId",
                table: "CollectionEntry",
                column: "AlbumId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionEntry_ArtistId",
                table: "CollectionEntry",
                column: "ArtistId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionEntry_TrackId",
                table: "CollectionEntry",
                column: "TrackId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionEntry_UserId_UpdatedAt",
                table: "CollectionEntry",
                columns: new[] { "UserId", "UpdatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CollectionEntry");
        }
    }
}
