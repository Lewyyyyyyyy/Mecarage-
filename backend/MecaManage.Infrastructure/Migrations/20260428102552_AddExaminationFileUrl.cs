using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MecaManage.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExaminationFileUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ExaminationFileUrl",
                table: "RepairTaskAssignments",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExaminationFileUrl",
                table: "RepairTaskAssignments");
        }
    }
}
