using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MecaManage.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGarageAdminId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AdminId",
                table: "Garages",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_Garages_AdminId",
                table: "Garages",
                column: "AdminId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Garages_Users_AdminId",
                table: "Garages",
                column: "AdminId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Garages_Users_AdminId",
                table: "Garages");

            migrationBuilder.DropIndex(
                name: "IX_Garages_AdminId",
                table: "Garages");

            migrationBuilder.DropColumn(
                name: "AdminId",
                table: "Garages");
        }
    }
}
