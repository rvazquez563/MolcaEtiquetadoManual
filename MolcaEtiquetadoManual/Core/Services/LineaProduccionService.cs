using MolcaEtiquetadoManual.Core.Interfaces;
using MolcaEtiquetadoManual.Core.Models;
using MolcaEtiquetadoManual.Data.Repositories;
using System.Collections.Generic;

namespace MolcaEtiquetadoManual.Core.Services
{
    public class LineaProduccionService : ILineaProduccionService
    {
        private readonly LineaProduccionRepository _repository;
        private readonly ILogService _logService;

        public LineaProduccionService(LineaProduccionRepository repository, ILogService logService)
        {
            _repository = repository;
            _logService = logService;
        }

        public List<LineaProduccion> GetAllLineas()
        {
            return _repository.GetAll();
        }

        public LineaProduccion GetLineaById(int id)
        {
            return _repository.GetById(id);
        }

        public void AddLinea(LineaProduccion linea)
        {
            _logService.Information("Creando nueva línea de producción: {Nombre}", linea.Nombre);
            _repository.Add(linea);
        }

        public void UpdateLinea(LineaProduccion linea)
        {
            _logService.Information("Actualizando línea de producción: {Nombre}, ID: {Id}", linea.Nombre, linea.Id);
            _repository.Update(linea);
        }
    }
}