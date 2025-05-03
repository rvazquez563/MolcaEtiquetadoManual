using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MolcaEtiquetadoManual.Migrations
{
    public partial class ModificacionSalida : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MOTIVO_NOCONF",
                table: "SALIDA",
                type: "nvarchar(max)",
                nullable: true,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MOTIVO_NOCONF",
                table: "SALIDA");
        }
    }
}
