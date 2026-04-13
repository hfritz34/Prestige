using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prestige.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserConsent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserConsents",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AllowsPersonalInsights = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    AllowsDerivedFeatureStorage = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    AllowsAnonymizedAggregation = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    AllowsModelTraining = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConsentVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserConsents", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "ConsentEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Field = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OldValue = table.Column<bool>(type: "bit", nullable: false),
                    NewValue = table.Column<bool>(type: "bit", nullable: false),
                    TriggeredBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_ConsentEvents_UserId_Timestamp",
                table: "ConsentEvents",
                columns: new[] { "UserId", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsentEvents");

            migrationBuilder.DropTable(
                name: "UserConsents");
        }
    }
}
