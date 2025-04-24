using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography.X509Certificates;

namespace MolcaEtiquetadoManual.Core.Models
{
    // Core/Models/OrdenProduccion.cs
    public class OrdenProduccion
    {
        [Column("ID")]
        public int IdLogico { get; set; }
        [Column("ITM")]
        public int Id { get; set; }

        [Column("LITM")]
        public string NumeroArticulo { get; set; }

        [Column("DSC1")]
        public string Descripcion { get; set; }

        [Column("UOM")]
        public string UnidadMedida { get; set; }

        [Column("DOCO")]
        public string ProgramaProduccion { get; set; }

        [Column("SLD")]
        public int DiasCaducidad { get; set; }

        [Column("CITM")]
        public string DUN14 { get; set; }

        [Column("STRT")]
        public DateTime FechaProduccionInicio { get; set; }

        [Column("DRQJ")]
        public DateTime FechaProduccionFin { get; set; }

        [Column("MULT")]
        public int CantidadPorPallet { get; set; }
        
        

        

    

        public string Lote => $"{ProgramaProduccion}{UnidadMedida}";
    }
}
