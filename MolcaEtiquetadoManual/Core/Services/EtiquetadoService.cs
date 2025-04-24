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

        public void GuardarEtiqueta(EtiquetaGenerada etiqueta)
        {
            _repository.GuardarEtiqueta(etiqueta);
        }

        public int ObtenerSiguienteNumeroSecuencial()
        {
            return _repository.ObtenerSiguienteNumeroSecuencial();
        }

        public List<EtiquetaGenerada> ObtenerEtiquetasGeneradas(DateTime fechaInicio, DateTime fechaFin)
        {
            return _repository.ObtenerEtiquetasGeneradas(fechaInicio, fechaFin);
        }
    }
}