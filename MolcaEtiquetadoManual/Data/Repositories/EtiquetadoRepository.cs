// Data/Repositories/UsuarioRepository.cs
using Microsoft.Data.SqlClient;
using MolcaEtiquetadoManual.Core.Models;
using MolcaEtiquetadoManual.Data.Context;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;


namespace MolcaEtiquetadoManual.Data.Repositories
{
    public class EtiquetadoRepository
    {
        private readonly AppDbContext _context;

        public EtiquetadoRepository(AppDbContext context)
        {
            _context = context;
        }
        // Reemplazar el uso de _context.Database.SqlQuery<int> con FromSqlInterpolated en una entidad temporal
       

        // Clase temporal para mapear el resultado del procedimiento almacenado
        private class TemporaryResult
        {
            public int Resultado { get; set; }
        }
        public OrdenProduccion BuscarOrdenPorDun14(string dun14)
        {
            _context.ChangeTracker.Clear();
            // Obtener la fecha actual para comparar con el rango permitido
            DateTime fechaActual = DateTime.Now.Date;

            // Primero buscar si existe la orden con ese DUN14, sin importar las fechas
            var ordenExistente = _context.OrdenesProduccion.FirstOrDefault(o => o.DUN14 == dun14);

            if (ordenExistente != null)
            {
                // Comprobar si está dentro del rango de fechas
                if (fechaActual >= ordenExistente.FechaProduccionInicio.Date &&
                    fechaActual <= ordenExistente.FechaProduccionFin.Date)
                {
                    return ordenExistente;
                }
                else
                {
                    // La orden existe pero está fuera del rango de fechas válido
                    // Esto permitirá que el código en Step1Control pueda mostrar un mensaje específico
                    throw new Exception($"La orden con DUN14 {dun14} existe pero está fuera del rango de fechas válido. " +
                                       $"Rango válido: {ordenExistente.FechaProduccionInicio.ToShortDateString()} - {ordenExistente.FechaProduccionFin.ToShortDateString()}");
                }
            }

            // No se encontró ninguna orden con ese DUN14
            return null;
        }
        // Reemplazar el uso de SqlQueryRaw con FromSqlInterpolated para corregir el error CS1061


        public string GuardarEtiqueta(EtiquetaGenerada etiqueta)
        {
            try
            {
                // Check if the etiqueta already exists in the database
                var existingEtiqueta = _context.EtiquetasGeneradas.FirstOrDefault(e => e.Id == etiqueta.Id);

                if (existingEtiqueta != null)
                {
                    // Update existing entity
                    _context.Entry(existingEtiqueta).CurrentValues.SetValues(etiqueta);
                }
                else
                {
                    // This is a new entity, set Id to 0 to let the database generate a new Id
                    etiqueta.Id = 0;
                    _context.EtiquetasGeneradas.Add(etiqueta);
                }

                _context.SaveChanges();
                return "Etiqueta guardada correctamente. SEC=" + etiqueta.SEC;
            }
            catch (SqlException ex)
            {
                return "Error al guardar la etiqueta en la base de datos. " + ex.Message;
            }
            catch (DbUpdateException ex)
            {
                return "Error al guardar la etiqueta en la base de datos. " + ex.Message;
            }
            catch (Exception ex)
            {
                return "Error inesperado al guardar la etiqueta. " + ex.Message;
            }
        }
        public int ObtenerSiguienteNumeroSecuencial(string programaProduccion, int lineaId)
        {
            // Buscar el último número secuencial para la línea específica
            var maxSec = _context.EtiquetasGeneradas
                .Where(s => s.DOCO == programaProduccion && s.LineaId == lineaId && s.Confirmada == true)
                .Select(s => (int?)s.SEC)
                .Max() ?? 0;  // ISNULL(MAX(SEC), 0)

            return maxSec + 1;
        }

        public int ObtenerSiguienteNumeroSecuencialdeldia(string diajuliano, int lineaId)
        {


            // Buscar el último número secuencial para la fecha actual
            var maxSec = _context.EtiquetasGeneradas
                .Where(s => s.EDDT == diajuliano && s.LineaId == lineaId && s.Confirmada == true)
                .Select(s => (int?)s.EDLN)
                .Max() ?? 0;  // ISNULL(MAX(SEC), 0)

            return maxSec + 1;
            //return ultimaEtiqueta != null ? ultimaEtiqueta.EDLN + 1 : 1;
        }

        public List<EtiquetaGenerada> ObtenerEtiquetasGeneradas(DateTime fechaInicio, DateTime fechaFin)
        {
            return _context.EtiquetasGeneradas
                .Where(e => e.FechaCreacion >= fechaInicio && e.FechaCreacion <= fechaFin)
                .OrderByDescending(e => e.FechaCreacion)
                .ToList();
        }
    }
}