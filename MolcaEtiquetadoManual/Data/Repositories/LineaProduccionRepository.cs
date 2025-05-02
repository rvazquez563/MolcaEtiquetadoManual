using MolcaEtiquetadoManual.Core.Models;
using MolcaEtiquetadoManual.Data.Context;
using System.Collections.Generic;
using System.Linq;

namespace MolcaEtiquetadoManual.Data.Repositories
{
    public class LineaProduccionRepository
    {
        private readonly AppDbContext _context;

        public LineaProduccionRepository(AppDbContext context)
        {
            _context = context;
        }

        public List<LineaProduccion> GetAll()
        {
            return _context.LineasProduccion
                .OrderBy(l => l.Id)
                .ToList();
        }

        public LineaProduccion GetById(int id)
        {
            return _context.LineasProduccion.Find(id);
        }

        public void Add(LineaProduccion linea)
        {
            _context.LineasProduccion.Add(linea);
            _context.SaveChanges();
        }

        public void Update(LineaProduccion linea)
        {
            _context.LineasProduccion.Update(linea);
            _context.SaveChanges();
        }
    }
}