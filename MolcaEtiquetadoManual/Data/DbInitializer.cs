using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Data/DbInitializer.cs
using MolcaEtiquetadoManual.Core.Models;
using MolcaEtiquetadoManual.Data.Context;
using System;
using System.Linq;

namespace MolcaEtiquetadoManual.Data
{
    public static class DbInitializer
    {
        public static void Initialize(AppDbContext context)
        {
            context.Database.EnsureCreated();

            // Verificar si ya hay usuarios
            if (context.Usuarios.Any())
            {
                return; // La base de datos ya tiene datos
            }

            // Agregar usuarios iniciales
            var usuarios = new Usuario[]
            {
                new Usuario { NombreUsuario = "admin", Contraseña = "admin123", Rol = "Administrador", Activo = true },
                new Usuario { NombreUsuario = "operador", Contraseña = "op123", Rol = "Operador", Activo = true }
            };

            foreach (var u in usuarios)
            {
                context.Usuarios.Add(u);
            }
            context.SaveChanges();
        }
    }
}
