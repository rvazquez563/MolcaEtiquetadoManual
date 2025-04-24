using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MolcaEtiquetadoManual.Core.Models
{
    // Core/Models/Usuario.cs
    public class Usuario
    {
        public int Id { get; set; }
        public string NombreUsuario { get; set; }
        public string Contraseña { get; set; }
        public string Rol { get; set; }
        public bool Activo { get; set; }
    }
}
