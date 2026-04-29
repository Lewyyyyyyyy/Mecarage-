using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MecaManage.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAvailablePeriodAndExaminationReport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AvailablePeriodEnd",
                table: "SymptomReports",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AvailablePeriodStart",
                table: "SymptomReports",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ExaminationObservations",
                table: "RepairTaskAssignments",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ExaminationStatus",
                table: "RepairTaskAssignments",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "ExaminationSubmittedAt",
                table: "RepairTaskAssignments",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PartsNeeded",
                table: "RepairTaskAssignments",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<Guid>(
                name: "AppointmentId",
                table: "Notifications",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "InvoiceId",
                table: "Notifications",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "RepairTaskId",
                table: "Notifications",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvailablePeriodEnd",
                table: "SymptomReports");

            migrationBuilder.DropColumn(
                name: "AvailablePeriodStart",
                table: "SymptomReports");

            migrationBuilder.DropColumn(
                name: "ExaminationObservations",
                table: "RepairTaskAssignments");

            migrationBuilder.DropColumn(
                name: "ExaminationStatus",
                table: "RepairTaskAssignments");

            migrationBuilder.DropColumn(
                name: "ExaminationSubmittedAt",
                table: "RepairTaskAssignments");

            migrationBuilder.DropColumn(
                name: "PartsNeeded",
                table: "RepairTaskAssignments");

            migrationBuilder.DropColumn(
                name: "AppointmentId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "InvoiceId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "RepairTaskId",
                table: "Notifications");
        }
    }
}
