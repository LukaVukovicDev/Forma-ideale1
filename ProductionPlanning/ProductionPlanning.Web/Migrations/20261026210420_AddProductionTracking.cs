using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductionPlanning.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Masine",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SifraMasine = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Naziv = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Masine", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlaniraniProizvodi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    MachineId = table.Column<int>(type: "INTEGER", nullable: false),
                    PlannedDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    PlannedQuantity = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaniraniProizvodi", x => x.Id);
                    table.CheckConstraint("CK_PlaniraniProizvodi_PlannedQuantity_Positive", "\"PlannedQuantity\" > 0");
                    table.ForeignKey(
                        name: "FK_PlaniraniProizvodi_Masine_MachineId",
                        column: x => x.MachineId,
                        principalTable: "Masine",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PlaniraniProizvodi_Proizvodi_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Proizvodi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Uradjeno",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlannedProductionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    RecordedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Uradjeno", x => x.Id);
                    table.CheckConstraint("CK_Uradjeno_Quantity_Positive", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_Uradjeno_PlaniraniProizvodi_PlannedProductionId",
                        column: x => x.PlannedProductionId,
                        principalTable: "PlaniraniProizvodi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Masine_SifraMasine",
                table: "Masine",
                column: "SifraMasine",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PlaniraniProizvodi_MachineId",
                table: "PlaniraniProizvodi",
                column: "MachineId");

            migrationBuilder.CreateIndex(
                name: "IX_PlaniraniProizvodi_PlannedDate",
                table: "PlaniraniProizvodi",
                column: "PlannedDate");

            migrationBuilder.CreateIndex(
                name: "IX_PlaniraniProizvodi_ProductId",
                table: "PlaniraniProizvodi",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Uradjeno_PlannedProductionId",
                table: "Uradjeno",
                column: "PlannedProductionId");

            migrationBuilder.CreateIndex(
                name: "IX_Uradjeno_RecordedAtUtc",
                table: "Uradjeno",
                column: "RecordedAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Uradjeno");

            migrationBuilder.DropTable(
                name: "PlaniraniProizvodi");

            migrationBuilder.DropTable(
                name: "Masine");
        }
    }
}
