using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Prestige.Api.Migrations
{
    /// <inheritdoc />
    public partial class updatetokenvaliduntiltousedatetimeinsteadofdatetime2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "TokenValidUntil",
                table: "User",
                type: "datetime",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "TokenValidUntil",
                table: "User",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime");
        }
    }
}
