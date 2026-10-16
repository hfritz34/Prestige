using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Prestige.Api.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class CatalogBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Album",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Album", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Artist",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Artist", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ImportHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FileHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ImportDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RecordCount = table.Column<int>(type: "integer", nullable: false),
                    MinTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MaxTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    BatchId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImportHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RatingCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MinScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    MaxScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    ColorHex = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RatingCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    NickName = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProfilePicURL = table.Column<string>(type: "text", nullable: false),
                    Bio = table.Column<string>(type: "text", nullable: true),
                    AccessToken = table.Column<string>(type: "text", nullable: false),
                    RefreshToken = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsSetup = table.Column<bool>(type: "boolean", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserConsents",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    AllowsPersonalInsights = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    AllowsDerivedFeatureStorage = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    AllowsAnonymizedAggregation = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    AllowsModelTraining = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConsentVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserConsents", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "Track",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    AlbumId = table.Column<string>(type: "text", nullable: false),
                    DurationMs = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Track", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Track_Album_AlbumId",
                        column: x => x.AlbumId,
                        principalTable: "Album",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AlbumArtist",
                columns: table => new
                {
                    AlbumId = table.Column<string>(type: "text", nullable: false),
                    ArtistsId = table.Column<string>(type: "text", nullable: false)
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
                name: "Image",
                columns: table => new
                {
                    Url = table.Column<string>(type: "text", nullable: false),
                    Height = table.Column<int>(type: "integer", nullable: false),
                    Width = table.Column<int>(type: "integer", nullable: false),
                    AlbumId = table.Column<string>(type: "text", nullable: true),
                    ArtistId = table.Column<string>(type: "text", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "Friendships",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    FriendId = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequestDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AcceptedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Friendships", x => new { x.UserId, x.FriendId });
                    table.ForeignKey(
                        name: "FK_Friendships_User_FriendId",
                        column: x => x.FriendId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Friendships_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RatingComparisons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ItemId1 = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ItemId2 = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ItemType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    WinnerId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ComparisonDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ItemId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ItemType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    AlbumId = table.Column<string>(type: "text", nullable: true),
                    CategoryId = table.Column<int>(type: "integer", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    RankWithinAlbum = table.Column<int>(type: "integer", nullable: true),
                    PersonalScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "UserAlbum",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    AlbumId = table.Column<string>(type: "text", nullable: false),
                    TotalTime = table.Column<int>(type: "integer", nullable: false),
                    IsFavorite = table.Column<bool>(type: "boolean", nullable: false),
                    IsPinned = table.Column<bool>(type: "boolean", nullable: false),
                    PersonalRatingScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    RatingPosition = table.Column<int>(type: "integer", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAlbum", x => new { x.UserId, x.AlbumId });
                    table.ForeignKey(
                        name: "FK_UserAlbum_Album_AlbumId",
                        column: x => x.AlbumId,
                        principalTable: "Album",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserAlbum_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserArtist",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ArtistId = table.Column<string>(type: "text", nullable: false),
                    TotalTime = table.Column<int>(type: "integer", nullable: false),
                    IsFavorite = table.Column<bool>(type: "boolean", nullable: false),
                    IsPinned = table.Column<bool>(type: "boolean", nullable: false),
                    PersonalRatingScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    RatingPosition = table.Column<int>(type: "integer", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserArtist", x => new { x.UserId, x.ArtistId });
                    table.ForeignKey(
                        name: "FK_UserArtist_Artist_ArtistId",
                        column: x => x.ArtistId,
                        principalTable: "Artist",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserArtist_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConsentEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Field = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OldValue = table.Column<bool>(type: "boolean", nullable: false),
                    NewValue = table.Column<bool>(type: "boolean", nullable: false),
                    TriggeredBy = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsentEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConsentEvents_UserConsents_UserId",
                        column: x => x.UserId,
                        principalTable: "UserConsents",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArtistTrack",
                columns: table => new
                {
                    ArtistsId = table.Column<string>(type: "text", nullable: false),
                    TrackId = table.Column<string>(type: "text", nullable: false)
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
                name: "UserTrack",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    TrackId = table.Column<string>(type: "text", nullable: false),
                    TotalTime = table.Column<int>(type: "integer", nullable: false),
                    IsFavorite = table.Column<bool>(type: "boolean", nullable: false),
                    IsPinned = table.Column<bool>(type: "boolean", nullable: false),
                    PersonalRatingScore = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    RatingPosition = table.Column<int>(type: "integer", nullable: true),
                    LastUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTrack", x => new { x.UserId, x.TrackId });
                    table.ForeignKey(
                        name: "FK_UserTrack_Track_TrackId",
                        column: x => x.TrackId,
                        principalTable: "Track",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserTrack_User_UserId",
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
                    { 1, "#22c55e", 1, 10.0m, 6.8m, "Loved" },
                    { 2, "#eab308", 2, 6.7m, 3.4m, "Liked" },
                    { 3, "#ef4444", 3, 3.3m, 0.0m, "Disliked" }
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
                name: "IX_ConsentEvents_UserId_Timestamp",
                table: "ConsentEvents",
                columns: new[] { "UserId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_Friendships_FriendId",
                table: "Friendships",
                column: "FriendId");

            migrationBuilder.CreateIndex(
                name: "IX_Image_AlbumId",
                table: "Image",
                column: "AlbumId");

            migrationBuilder.CreateIndex(
                name: "IX_Image_ArtistId",
                table: "Image",
                column: "ArtistId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportHistory_BatchId",
                table: "ImportHistories",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_ImportHistory_UserId_FileHash",
                table: "ImportHistories",
                columns: new[] { "UserId", "FileHash" });

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

            migrationBuilder.CreateIndex(
                name: "IX_Track_AlbumId",
                table: "Track",
                column: "AlbumId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAlbum_AlbumId",
                table: "UserAlbum",
                column: "AlbumId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAlbum_TotalTime",
                table: "UserAlbum",
                column: "TotalTime");

            migrationBuilder.CreateIndex(
                name: "IX_UserAlbum_UserId_TotalTime",
                table: "UserAlbum",
                columns: new[] { "UserId", "TotalTime" });

            migrationBuilder.CreateIndex(
                name: "IX_UserArtist_ArtistId",
                table: "UserArtist",
                column: "ArtistId");

            migrationBuilder.CreateIndex(
                name: "IX_UserArtist_TotalTime",
                table: "UserArtist",
                column: "TotalTime");

            migrationBuilder.CreateIndex(
                name: "IX_UserArtist_UserId_TotalTime",
                table: "UserArtist",
                columns: new[] { "UserId", "TotalTime" });

            migrationBuilder.CreateIndex(
                name: "IX_UserTrack_TotalTime",
                table: "UserTrack",
                column: "TotalTime");

            migrationBuilder.CreateIndex(
                name: "IX_UserTrack_TrackId",
                table: "UserTrack",
                column: "TrackId");

            migrationBuilder.CreateIndex(
                name: "IX_UserTrack_UserId_TotalTime",
                table: "UserTrack",
                columns: new[] { "UserId", "TotalTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlbumArtist");

            migrationBuilder.DropTable(
                name: "ArtistTrack");

            migrationBuilder.DropTable(
                name: "ConsentEvents");

            migrationBuilder.DropTable(
                name: "Friendships");

            migrationBuilder.DropTable(
                name: "Image");

            migrationBuilder.DropTable(
                name: "ImportHistories");

            migrationBuilder.DropTable(
                name: "RatingComparisons");

            migrationBuilder.DropTable(
                name: "Ratings");

            migrationBuilder.DropTable(
                name: "UserAlbum");

            migrationBuilder.DropTable(
                name: "UserArtist");

            migrationBuilder.DropTable(
                name: "UserTrack");

            migrationBuilder.DropTable(
                name: "UserConsents");

            migrationBuilder.DropTable(
                name: "RatingCategories");

            migrationBuilder.DropTable(
                name: "Artist");

            migrationBuilder.DropTable(
                name: "Track");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropTable(
                name: "Album");
        }
    }
}
