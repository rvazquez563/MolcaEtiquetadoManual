using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MolcaEtiquetadoManual.Migrations
{
    public partial class Creardb : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ENTRADA",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ITM = table.Column<int>(type: "int", nullable: false),
                    LITM = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    DSC1 = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UOM = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DOCO = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SLD = table.Column<int>(type: "int", nullable: false),
                    CITM = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    STRT = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DRQJ = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MULT = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ENTRADA", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "LineasProduccion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Activa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineasProduccion", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SALIDA",
                columns: table => new
                {
                    ID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EDUS = table.Column<string>(type: "nvarchar(max)",  nullable: false),
                    EDDT = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EDTN = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EDLN = table.Column<int>(type: "int", nullable: false),
                    DOCO = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LITM = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SOQS = table.Column<int>(type: "int", nullable: false),
                    UOM = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LOTN = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EXPR = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TDAY = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SHFT = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    URDT = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SEC = table.Column<int>(type: "int", nullable: false),
                    ESTADO = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    URRF = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Confirmada = table.Column<bool>(type: "bit", nullable: false),
                    LINEID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SALIDA", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreUsuario = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Contraseña = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Rol = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ENTRADA");

            migrationBuilder.DropTable(
                name: "LineasProduccion");

            migrationBuilder.DropTable(
                name: "SALIDA");

            migrationBuilder.DropTable(
                name: "Usuarios");
        }
    }
}
