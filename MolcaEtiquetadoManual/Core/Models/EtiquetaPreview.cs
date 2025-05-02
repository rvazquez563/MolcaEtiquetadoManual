using System;
using System.Windows.Media.Imaging;

namespace MolcaEtiquetadoManual.Core.Models
{
    public class EtiquetaPreview
    {
        // Datos de la etiqueta
        public string NumeroArticulo { get; set; }
        public string Descripcion { get; set; }
        public string Lote { get; set; }
        public DateTime FechaProduccion { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public int CantidadPorPallet { get; set; }
        public string DUN14 { get; set; }
        public string CodigoBarras { get; set; }

        // Imagen del código de barras
        public BitmapSource ImagenCodigoBarras { get; set; }

        // Constructor por defecto
        public EtiquetaPreview()
        {
            FechaProduccion = DateTime.Now;
            FechaVencimiento = DateTime.Now.AddDays(30); // Valor por defecto
        }

        // Constructor para crear una vista previa desde una orden y una etiqueta
        public EtiquetaPreview(OrdenProduccion orden, EtiquetaGenerada etiqueta, string codigoBarras = null)
        {
            if (orden == null || etiqueta == null)
                return;

            NumeroArticulo = orden.NumeroArticulo;
            Descripcion = orden.Descripcion;
            Lote = etiqueta.LOTN;
            FechaProduccion = DateTime.Now; // Fecha actual para la declaración
            FechaVencimiento = etiqueta.URDT;
            CantidadPorPallet = orden.CantidadPorPallet;
            DUN14 = orden.DUN14;
            CodigoBarras = codigoBarras;
        }
    }
}