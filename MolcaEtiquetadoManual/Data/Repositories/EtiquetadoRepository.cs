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
        public int ObtenerSiguienteNumeroPallet(string programaProduccion)
        {
            // Crear una clase temporal para mapear el resultado del procedimiento almacenado
            var resultado = _context.Database
                .ExecuteSqlRaw($"EXEC SP_ObtenerSiguienteNumeroPallet @ProgramaProduccion = {programaProduccion}");             
                
               

            return resultado;
        }

        // Clase temporal para mapear el resultado del procedimiento almacenado
        private class TemporaryResult
        {
            public int Resultado { get; set; }
        }
        public OrdenProduccion BuscarOrdenPorDun14(string dun14)
        {
            return _context.OrdenesProduccion
                .FirstOrDefault(o => o.DUN14 == dun14);
        }

        // Reemplazar el uso de SqlQueryRaw con FromSqlInterpolated para corregir el error CS1061
        public int GuardarEtiquetaConStoredProcedure(EtiquetaGenerada etiqueta)
        {
            var parametros = new[]
            {
                new SqlParameter("@EDUS", etiqueta.EDUS),
                new SqlParameter("@EDDT", etiqueta.EDDT),
                new SqlParameter("@EDTN", etiqueta.EDTN),
                new SqlParameter("@EDLN", etiqueta.EDLN),
                new SqlParameter("@DOCO", etiqueta.DOCO),
                new SqlParameter("@LITM", etiqueta.LITM),
                new SqlParameter("@SOQS", etiqueta.SOQS),
                new SqlParameter("@UOM", etiqueta.UOM1),
                new SqlParameter("@LOTN", etiqueta.LOTN),
                new SqlParameter("@EXPR", etiqueta.EXPR),
                new SqlParameter("@TDAY", etiqueta.TDAY),
                new SqlParameter("@SHFT", etiqueta.SHFT),
                new SqlParameter("@URDT", etiqueta.URDT),
                new SqlParameter("@ESTADO", etiqueta.ESTADO),
                new SqlParameter("@URRF", etiqueta.URRF ?? string.Empty),
                new SqlParameter("@Confirmada", etiqueta.Confirmada)
            };

            // Usar FromSqlInterpolated con una entidad temporal para mapear el resultado
            var resultado = _context.EtiquetasGeneradas
                .FromSqlInterpolated($@"
                    EXEC SP_InsertarEtiqueta 
                    @EDUS = {parametros[0].Value}, 
                    @EDDT = {parametros[1].Value},
                    @EDTN = {parametros[2].Value},
                    @EDLN = {parametros[3].Value},
                    @DOCO = {parametros[4].Value},
                    @LITM = {parametros[5].Value},
                    @SOQS = {parametros[6].Value},
                    @UOM = {parametros[7].Value},
                    @LOTN = {parametros[8].Value},    
                    @EXPR = {parametros[9].Value},
                    @TDAY = {parametros[10].Value},
                    @SHFT = {parametros[11].Value},    
                    @URDT = {parametros[12].Value},
                    @ESTADO = {parametros[13].Value},
                    @URRF = {parametros[14].Value},
                    @Confirmada = {parametros[15].Value}
                ")
                .AsEnumerable()
                .Select(e => e.EDLN)
                .FirstOrDefault();

            return resultado;
        }

        public void GuardarEtiqueta(EtiquetaGenerada etiqueta)
        {
            _context.EtiquetasGeneradas.Add(etiqueta);
            _context.SaveChanges();
        }

        public int ObtenerSiguienteNumeroSecuencial(string programaProduccion)
        {
            

            // Buscar el último número secuencial para la fecha actual
            var maxSec = _context.EtiquetasGeneradas
                .Where(s => s.DOCO == programaProduccion && s.Confirmada==true)
                .Select(s => (int?)s.SEC)
                .Max() ?? 0;  // ISNULL(MAX(SEC), 0)

            return maxSec + 1;
            //return ultimaEtiqueta != null ? ultimaEtiqueta.EDLN + 1 : 1;
        }

        public int ObtenerSiguienteNumeroSecuencialdeldia(string diajuliano)
        {


            // Buscar el último número secuencial para la fecha actual
            var maxSec = _context.EtiquetasGeneradas
                .Where(s => s.EDDT == diajuliano && s.Confirmada == true)
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