using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductionPlanning.Web.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Proizvodi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SifraProizvoda = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Naziv = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proizvodi", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Proizvodi_SifraProizvoda",
                table: "Proizvodi",
                column: "SifraProizvoda",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Proizvodi");
        }
    }
}
