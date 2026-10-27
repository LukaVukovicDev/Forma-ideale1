using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductionPlanning.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddMachineAvailabilityCalendar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MachineBreaks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MachineId = table.Column<int>(type: "INTEGER", nullable: false),
                    DayOfWeek = table.Column<int>(type: "INTEGER", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MachineBreaks", x => x.Id);
                    table.CheckConstraint("CK_MachineBreaks_DayOfWeek", "\"DayOfWeek\" >= 0 AND \"DayOfWeek\" <= 6");
                    table.CheckConstraint("CK_MachineBreaks_TimeRange", "\"StartTime\" < \"EndTime\"");
                    table.ForeignKey(
                        name: "FK_MachineBreaks_Masine_MachineId",
                        column: x => x.MachineId,
                        principalTable: "Masine",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MachineCalendarExceptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MachineId = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    ExceptionType = table.Column<int>(type: "INTEGER", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MachineCalendarExceptions", x => x.Id);
                    table.CheckConstraint("CK_MachineCalendarExceptions_ExceptionType", "\"ExceptionType\" IN (1, 2)");
                    table.CheckConstraint("CK_MachineCalendarExceptions_TimeRange", "\"StartTime\" < \"EndTime\"");
                    table.ForeignKey(
                        name: "FK_MachineCalendarExceptions_Masine_MachineId",
                        column: x => x.MachineId,
                        principalTable: "Masine",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MachineWorkingIntervals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MachineId = table.Column<int>(type: "INTEGER", nullable: false),
                    DayOfWeek = table.Column<int>(type: "INTEGER", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "TEXT", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MachineWorkingIntervals", x => x.Id);
                    table.CheckConstraint("CK_MachineWorkingIntervals_DayOfWeek", "\"DayOfWeek\" >= 0 AND \"DayOfWeek\" <= 6");
                    table.CheckConstraint("CK_MachineWorkingIntervals_TimeRange", "\"StartTime\" < \"EndTime\"");
                    table.ForeignKey(
                        name: "FK_MachineWorkingIntervals_Masine_MachineId",
                        column: x => x.MachineId,
                        principalTable: "Masine",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MachineBreaks_MachineId_DayOfWeek_StartTime",
                table: "MachineBreaks",
                columns: new[] { "MachineId", "DayOfWeek", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_MachineBreaks_MachineId_DayOfWeek_StartTime_EndTime",
                table: "MachineBreaks",
                columns: new[] { "MachineId", "DayOfWeek", "StartTime", "EndTime" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MachineCalendarExceptions_MachineId_Date_ExceptionType_StartTime",
                table: "MachineCalendarExceptions",
                columns: new[] { "MachineId", "Date", "ExceptionType", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_MachineCalendarExceptions_MachineId_Date_ExceptionType_StartTime_EndTime",
                table: "MachineCalendarExceptions",
                columns: new[] { "MachineId", "Date", "ExceptionType", "StartTime", "EndTime" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MachineWorkingIntervals_MachineId_DayOfWeek_StartTime",
                table: "MachineWorkingIntervals",
                columns: new[] { "MachineId", "DayOfWeek", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_MachineWorkingIntervals_MachineId_DayOfWeek_StartTime_EndTime",
                table: "MachineWorkingIntervals",
                columns: new[] { "MachineId", "DayOfWeek", "StartTime", "EndTime" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MachineBreaks");

            migrationBuilder.DropTable(
                name: "MachineCalendarExceptions");

            migrationBuilder.DropTable(
                name: "MachineWorkingIntervals");
        }
    }
}
