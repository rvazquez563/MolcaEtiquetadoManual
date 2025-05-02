// Core/Services/UsuarioService.cs
using MolcaEtiquetadoManual.Core.Interfaces;
using MolcaEtiquetadoManual.Core.Models;
using MolcaEtiquetadoManual.Data.Repositories;

namespace MolcaEtiquetadoManual.Core.Services
{
    public class EtiquetadoService : IEtiquetadoService
    {
        private readonly EtiquetadoRepository _repository;

        public EtiquetadoService(EtiquetadoRepository repository)
        {
            _repository = repository;
        }
 
        public OrdenProduccion BuscarOrdenPorDun14(string dun14)
        {
            return _repository.BuscarOrdenPorDun14(dun14);
        }

        public string GuardarEtiqueta(EtiquetaGenerada etiqueta)
        {
            return _repository.GuardarEtiqueta(etiqueta);
        }
        public int ObtenerSiguienteNumeroSecuencialdeldia(string diajuliano, int linea)
        {
            return _repository.ObtenerSiguienteNumeroSecuencialdeldia(diajuliano,linea);
        }
        public int ObtenerSiguienteNumeroSecuencial(string programaProduccion, int linea)
        {
            return _repository.ObtenerSiguienteNumeroSecuencial(programaProduccion, linea);
        }
    
        public List<EtiquetaGenerada> ObtenerEtiquetasGeneradas(DateTime fechaInicio, DateTime fechaFin)
        {
            return _repository.ObtenerEtiquetasGeneradas(fechaInicio, fechaFin);
        }
    }
}