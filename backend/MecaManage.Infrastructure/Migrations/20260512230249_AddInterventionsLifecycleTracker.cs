using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MecaManage.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInterventionsLifecycleTracker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Interventions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    GarageId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ClientId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    VehicleId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    AppointmentId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    SymptomReportId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    InvoiceId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    RepairTaskId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProceedWithIntervention = table.Column<bool>(type: "tinyint(1)", nullable: true),
                    ExaminationNotes = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PartsUsedJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RepairNotes = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PaymentAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PaymentMethod = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PaymentDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    PaidBy = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Interventions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Interventions_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Interventions_Garages_GarageId",
                        column: x => x.GarageId,
                        principalTable: "Garages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Interventions_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Interventions_SymptomReports_SymptomReportId",
                        column: x => x.SymptomReportId,
                        principalTable: "SymptomReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Interventions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Interventions_Users_ClientId",
                        column: x => x.ClientId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Interventions_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Interventions_AppointmentId",
                table: "Interventions",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Interventions_ClientId",
                table: "Interventions",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_Interventions_GarageId",
                table: "Interventions",
                column: "GarageId");

            migrationBuilder.CreateIndex(
                name: "IX_Interventions_InvoiceId",
                table: "Interventions",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Interventions_SymptomReportId",
                table: "Interventions",
                column: "SymptomReportId");

            migrationBuilder.CreateIndex(
                name: "IX_Interventions_TenantId",
                table: "Interventions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Interventions_VehicleId",
                table: "Interventions",
                column: "VehicleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Interventions");
        }
    }
}
