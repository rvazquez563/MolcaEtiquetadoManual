using Microsoft.EntityFrameworkCore.Migrations;
using MolcaEtiquetadoManual.Data.StoredProcedures;

#nullable disable

namespace MolcaEtiquetadoManual.Migrations
{
    public partial class AgregoStores : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(StoredProcedureDefinitions.SP_ObtenerSiguienteNumeroPallet);
            migrationBuilder.Sql(StoredProcedureDefinitions.SP_InsertarEtiqueta);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Eliminar los stored procedures si se revierte la migración
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS SP_ObtenerSiguienteNumeroPallet");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS SP_InsertarEtiqueta");
        }
    }
}
