
// Core/Models/EtiquetaGenerada.cs
using System.ComponentModel.DataAnnotations.Schema;
namespace MolcaEtiquetadoManual.Core.Models
{ 
    public class EtiquetaGenerada
    {
        [Column("ID")]
        public int Id { get; set; }

        [Column("EDUS")]
        public string EDUS { get; set; }  // Usuario transmisión

        [Column("EDDT")]
        public string EDDT { get; set; }  // Fecha de transmisión en juliana

        [Column("EDTN")]
        public string EDTN { get; set; }  // Número transmisión (MMDD)

        [Column("EDLN")]
        public int EDLN { get; set; }  // Línea de transmisión

        [Column("DOCO")]
        public string DOCO { get; set; }  // Programa de producción

        [Column("LITM")]
        public string LITM { get; set; }  // 2do. Número de artículo

        [Column("SOQS")]
        public int SOQS { get; set; }  // Cantidad por pallet

        [Column("UOM")]
        public string UOM1 { get; set; }  // Unidad de medida principal

        [Column("LOTN")]
        public string LOTN { get; set; }  // DOCO+SHFT (Lote)

        [Column("EXPR")]
        public DateTime EXPR { get; set; }  // Fecha de vencimiento

        [Column("TDAY")]
        public string TDAY { get; set; }  // Hora minuto y segundo

        [Column("SHFT")]
        public string SHFT { get; set; }  // Turno (1-2-3)

        [Column("URDT")]
        public string URDT { get; set; }

        [Column("SEC")]
        public int SEC { get; set; }

        [Column("ESTADO")]
        public string ESTADO { get; set; }

        [Column("URRF")]
        public string URRF { get; set; }

        public DateTime FechaCreacion { get; set; }
        public bool Confirmada { get; set; }

        [Column("LINEID")]
        public int LineaId { get; set; }

        [Column("MOTIVO_NOCONF")]
        public string MotivoNoConfirmacion { get; set; }
    }
}