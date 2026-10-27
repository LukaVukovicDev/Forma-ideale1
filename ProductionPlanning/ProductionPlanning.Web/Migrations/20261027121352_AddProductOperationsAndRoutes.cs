using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductionPlanning.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddProductOperationsAndRoutes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TehnoloskeOperacije",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TehnoloskeOperacije", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KompatibilnostMasinaIOperacija",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MachineId = table.Column<int>(type: "INTEGER", nullable: false),
                    OperationDefinitionId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KompatibilnostMasinaIOperacija", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KompatibilnostMasinaIOperacija_Masine_MachineId",
                        column: x => x.MachineId,
                        principalTable: "Masine",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_KompatibilnostMasinaIOperacija_TehnoloskeOperacije_OperationDefinitionId",
                        column: x => x.OperationDefinitionId,
                        principalTable: "TehnoloskeOperacije",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OperacijeProizvoda",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    OperationDefinitionId = table.Column<int>(type: "INTEGER", nullable: false),
                    SequenceNumber = table.Column<int>(type: "INTEGER", nullable: false),
                    SetupDurationMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    ProcessingSecondsPerUnit = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperacijeProizvoda", x => x.Id);
                    table.CheckConstraint("CK_OperacijeProizvoda_ProcessingSecondsPerUnit_Positive", "\"ProcessingSecondsPerUnit\" > 0");
                    table.CheckConstraint("CK_OperacijeProizvoda_SequenceNumber_Positive", "\"SequenceNumber\" > 0");
                    table.CheckConstraint("CK_OperacijeProizvoda_SetupDurationMinutes_NonNegative", "\"SetupDurationMinutes\" >= 0");
                    table.ForeignKey(
                        name: "FK_OperacijeProizvoda_Proizvodi_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Proizvodi",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperacijeProizvoda_TehnoloskeOperacije_OperationDefinitionId",
                        column: x => x.OperationDefinitionId,
                        principalTable: "TehnoloskeOperacije",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KompatibilnostMasinaIOperacija_MachineId_OperationDefinitionId",
                table: "KompatibilnostMasinaIOperacija",
                columns: new[] { "MachineId", "OperationDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KompatibilnostMasinaIOperacija_OperationDefinitionId",
                table: "KompatibilnostMasinaIOperacija",
                column: "OperationDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_OperacijeProizvoda_OperationDefinitionId",
                table: "OperacijeProizvoda",
                column: "OperationDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_OperacijeProizvoda_ProductId_SequenceNumber",
                table: "OperacijeProizvoda",
                columns: new[] { "ProductId", "SequenceNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TehnoloskeOperacije_Code",
                table: "TehnoloskeOperacije",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KompatibilnostMasinaIOperacija");

            migrationBuilder.DropTable(
                name: "OperacijeProizvoda");

            migrationBuilder.DropTable(
                name: "TehnoloskeOperacije");
        }
    }
}
