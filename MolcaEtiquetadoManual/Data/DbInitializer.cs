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

            //Verificar si ya hay usuarios
            if (context.Usuarios.Any())
            {
                // Si hay usuarios, solo verificamos si hay que agregar líneas
                if (!context.LineasProduccion.Any())
                {
                    // Agregar líneas iniciales
                    var lineasi = new LineaProduccion[]
                    {
                        new LineaProduccion { Id = 1, Nombre = "Línea 1", Descripcion = "Línea principal de producción", Activa = true },
                        new LineaProduccion { Id = 2, Nombre = "Línea 2", Descripcion = "Línea secundaria de producción", Activa = true },
                        new LineaProduccion { Id = 3, Nombre = "Línea 3", Descripcion = "Línea terciaria de producción", Activa = true },
                        new LineaProduccion { Id = 4, Nombre = "Línea 4", Descripcion = "Línea cuaternaria de producción", Activa = true }
                    };

                    foreach (var l in lineasi)
                    {
                        context.LineasProduccion.Add(l);
                    }
                    context.SaveChanges();
                }
                return; // La base de datos ya tiene datos de usuarios
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

            // Agregar líneas iniciales
            var lineas = new LineaProduccion[]
            {
        new LineaProduccion { Id = 1, Nombre = "Línea 1", Descripcion = "Línea principal de producción", Activa = true },
        new LineaProduccion { Id = 2, Nombre = "Línea 2", Descripcion = "Línea secundaria de producción", Activa = true },
        new LineaProduccion { Id = 3, Nombre = "Línea 3", Descripcion = "Línea terciaria de producción", Activa = true },
        new LineaProduccion { Id = 4, Nombre = "Línea 4", Descripcion = "Línea cuaternaria de producción", Activa = true }
            };

            foreach (var l in lineas)
            {
                context.LineasProduccion.Add(l);
            }
            context.SaveChanges();
        }
    }
}
