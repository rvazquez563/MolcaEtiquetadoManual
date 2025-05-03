using MolcaEtiquetadoManual.Core.Models;
using MolcaEtiquetadoManual.Data.Context;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace MolcaEtiquetadoManual.Data
{
    public static class DbInitializer
    {
        public static void Initialize(AppDbContext context)
        {
            try
            {
                // Verificar si la base de datos existe, si no, crearla
                // Nota: EnsureCreated podría estar causando conflictos con las migraciones
                // Si estás usando migraciones, considera usar Migrate() en su lugar
                bool wasCreated = context.Database.EnsureCreated();

                if (wasCreated)
                {
                    Console.WriteLine("Base de datos creada correctamente.");
                }

                // Desactivar la detección de cambios para mejorar el rendimiento
                context.ChangeTracker.AutoDetectChangesEnabled = false;

                // Verificar si ya hay usuarios
                if (!context.Usuarios.Any())
                {
                    // Agregar usuarios iniciales
                    SeedUsuarios(context);
                }

                // Verificar si ya hay líneas de producción
                if (!context.LineasProduccion.Any())
                {
                    // Agregar líneas de producción
                    SeedLineasProduccion(context);
                }

                // Opcionalmente, verificar y agregar órdenes de prueba
                if (!context.OrdenesProduccion.Any())
                {
                    SeedOrdenesProduccion(context);
                }
            }
            catch (DbUpdateException ex)
            {
                // Extraer detalles específicos de la excepción de Entity Framework
                var sqlException = ex.InnerException as SqlException;
                if (sqlException != null)
                {
                    Console.WriteLine($"Error SQL {sqlException.Number}: {sqlException.Message}");

                    // Para identificar problemas específicos
                    if (sqlException.Number == 2601 || sqlException.Number == 2627)
                    {
                        Console.WriteLine("Error de violación de clave única o restricción de unicidad.");
                    }
                    else if (sqlException.Number == 547)
                    {
                        Console.WriteLine("Error de violación de restricción de clave foránea.");
                    }
                }

                throw new Exception($"Error al inicializar la base de datos: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error general: {ex.Message}");
                throw;
            }
        }

        private static void SeedUsuarios(AppDbContext context)
        {
            try
            {
                Console.WriteLine("Iniciando la inserción de usuarios...");

                var usuarios = new Usuario[]
                {
                    new Usuario { NombreUsuario = "admin", Contraseña = "admin123", Rol = "Administrador", Activo = true },
                    new Usuario { NombreUsuario = "operador", Contraseña = "op123", Rol = "Operador", Activo = true }
                };

                foreach (var u in usuarios)
                {
                    // Agregar individualmente y guardar para detectar errores específicos
                    context.Usuarios.Add(u);
                    context.SaveChanges();
                    Console.WriteLine($"Usuario {u.NombreUsuario} agregado correctamente.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al insertar usuarios: {ex.Message}");
                throw;
            }
        }

        private static void SeedLineasProduccion(AppDbContext context)
        {
            try
            {
                Console.WriteLine("Iniciando la inserción de líneas de producción...");

                var lineas = new LineaProduccion[]
                {
                    new LineaProduccion { Nombre = "Línea 1", Descripcion = "Línea principal de producción", Activa = true },
                    new LineaProduccion { Nombre = "Línea 2", Descripcion = "Línea secundaria de producción", Activa = true },
                    new LineaProduccion { Nombre = "Línea 3", Descripcion = "Línea terciaria de producción", Activa = true },
                    new LineaProduccion { Nombre = "Línea 4", Descripcion = "Línea cuaternaria de producción", Activa = true }
                };

                foreach (var l in lineas)
                {
                    // Agregar individualmente para detectar errores específicos
                    context.LineasProduccion.Add(l);
                    context.SaveChanges();
                    Console.WriteLine($"Línea {l.Nombre} agregada correctamente.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al insertar líneas de producción: {ex.Message}");
                throw;
            }
        }

        private static void SeedOrdenesProduccion(AppDbContext context)
        {
            try
            {
                Console.WriteLine("Iniciando la inserción de órdenes de producción de prueba...");

                var ordenes = new OrdenProduccion[]
                {
                    new OrdenProduccion
                    {
                        Id = 123456,
                        NumeroArticulo = "00744801",
                        Descripcion = "HARINA 000 - BOLSA 1KG",
                        UnidadMedida = "UN",
                        ProgramaProduccion = "557525",
                        DiasCaducidad = 90,
                        DUN14 = "17792180001525",
                        FechaProduccionInicio = DateTime.Now,
                        FechaProduccionFin = DateTime.Now.AddDays(7),
                        CantidadPorPallet = 48
                    }
                };

                foreach (var o in ordenes)
                {
                    // Agregar individualmente para detectar errores específicos
                    context.OrdenesProduccion.Add(o);
                    context.SaveChanges();
                    Console.WriteLine($"Orden {o.Id} agregada correctamente.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al insertar órdenes de producción: {ex.Message}");
                throw;
            }
        }
    }
}