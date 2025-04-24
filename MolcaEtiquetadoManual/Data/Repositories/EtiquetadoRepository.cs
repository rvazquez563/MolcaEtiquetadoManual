// Data/Repositories/UsuarioRepository.cs
using MolcaEtiquetadoManual.Core.Models;
using MolcaEtiquetadoManual.Data.Context;

namespace MolcaEtiquetadoManual.Data.Repositories
{
    public class EtiquetadoRepository
    {
        private readonly AppDbContext _context;

        public EtiquetadoRepository(AppDbContext context)
        {
            _context = context;
        }

        public OrdenProduccion BuscarOrdenPorDun14(string dun14)
        {
            return _context.OrdenesProduccion
                .FirstOrDefault(o => o.DUN14 == dun14);
        }

        public void GuardarEtiqueta(EtiquetaGenerada etiqueta)
        {
            _context.EtiquetasGeneradas.Add(etiqueta);
            _context.SaveChanges();
        }

        public int ObtenerSiguienteNumeroSecuencial()
        {
            string fechaHoy = DateTime.Now.ToString("MMdd");

            // Buscar el último número secuencial para la fecha actual
            var ultimaEtiqueta = _context.EtiquetasGeneradas
                .Where(e => e.EDTN == fechaHoy)
                .OrderByDescending(e => e.EDLN)
                .FirstOrDefault();

            return ultimaEtiqueta != null ? ultimaEtiqueta.EDLN + 1 : 1;
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