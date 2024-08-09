using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prestige.Api.Migrations
{
    /// <inheritdoc />
    public partial class FullUserIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            string sqlScript = File.ReadAllText("./Migrations/20240807152208_FullUserIndex.sql");
            migrationBuilder.Sql(sqlScript, true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            string sqlScript = File.ReadAllText("./Migrations/20240807152208_FullUserIndex_Down.sql");
            migrationBuilder.Sql(sqlScript, true);
        }
    }
}
