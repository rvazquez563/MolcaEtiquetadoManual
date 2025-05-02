using System.ComponentModel.DataAnnotations.Schema;

namespace MolcaEtiquetadoManual.Core.Models
{
    public class LineaProduccion
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public bool Activa { get; set; }
    }
}