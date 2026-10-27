using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductionPlanning.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionOperationScheduling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScheduledProductionOperations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PlannedProductionId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductOperationId = table.Column<int>(type: "INTEGER", nullable: false),
                    MachineId = table.Column<int>(type: "INTEGER", nullable: false),
                    StartAtUtc = table.Column<long>(type: "INTEGER", nullable: false),
                    EndAtUtc = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledProductionOperations", x => x.Id);
                    table.CheckConstraint("CK_ScheduledProductionOperations_TimeRange", "\"StartAtUtc\" < \"EndAtUtc\"");
                    table.ForeignKey(
                        name: "FK_ScheduledProductionOperations_Masine_MachineId",
                        column: x => x.MachineId,
                        principalTable: "Masine",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScheduledProductionOperations_OperacijeProizvoda_ProductOperationId",
                        column: x => x.ProductOperationId,
                        principalTable: "OperacijeProizvoda",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScheduledProductionOperations_PlaniraniProizvodi_PlannedProductionId",
                        column: x => x.PlannedProductionId,
                        principalTable: "PlaniraniProizvodi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledProductionOperations_MachineId_StartAtUtc_EndAtUtc",
                table: "ScheduledProductionOperations",
                columns: new[] { "MachineId", "StartAtUtc", "EndAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledProductionOperations_PlannedProductionId_ProductOperationId",
                table: "ScheduledProductionOperations",
                columns: new[] { "PlannedProductionId", "ProductOperationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledProductionOperations_ProductOperationId",
                table: "ScheduledProductionOperations",
                column: "ProductOperationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduledProductionOperations");
        }
    }
}
