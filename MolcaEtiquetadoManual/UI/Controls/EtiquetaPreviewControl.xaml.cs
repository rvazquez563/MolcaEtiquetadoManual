using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using MolcaEtiquetadoManual.Core.Models;

namespace MolcaEtiquetadoManual.UI.Controls
{
    public partial class EtiquetaPreviewControl : UserControl
    {
        public EtiquetaPreviewControl()
        {
            InitializeComponent();

            // Inicialmente mostrar el mensaje de "no hay vista previa"
            MostrarMensajeNoPreview(true);
        }

        /// <summary>
        /// Actualiza el control con los datos de la vista previa
        /// </summary>
        public void ActualizarVista(EtiquetaPreview preview)
        {
            if (preview == null)
            {
                Limpiar();
                return;
            }

            // Ocultar mensaje de "no preview"
            MostrarMensajeNoPreview(false);

            // Formato de fecha de vencimiento como "22 MAR 15"
            string fechaVenc = preview.FechaVencimiento.ToString("dd MMM yy").ToUpper();
            txtFechaVencimiento.Text = fechaVenc;

            // Formato de fecha y hora de declaración como "YYMMDD HH:MM"
            string fechaDeclaracion = preview.FechaProduccion.ToString("yyMMdd HH:mm");
            txtFechaHoraDeclaracion.Text = fechaDeclaracion;

            // Mostrar el lote con el nuevo formato DDMMYY#
            txtLote.Text = preview.Lote;

            // DUN14
            txtDun14.Text = preview.DUN14;

            // Descripción
            txtDescripcion.Text = preview.Descripcion;

            // Número de artículo
            txtNumeroArticulo.Text = preview.NumeroArticulo;

            // Cantidad por pallet formateada como "0008 Bultos"
            txtCantidadPallet.Text = $"{preview.CantidadPorPallet:D4} Bultos";

            // Actualizar imagen del código de barras
            if (preview.ImagenCodigoBarras != null)
            {
                imgCodigoBarras.Source = preview.ImagenCodigoBarras;
            }
            else
            {
                imgCodigoBarras.Source = null;
            }
        }

        /// <summary>
        /// Limpia la vista previa
        /// </summary>
        public void Limpiar()
        {
            txtFechaVencimiento.Text = "";
            txtFechaHoraDeclaracion.Text = "";
            txtLote.Text = "";
            txtDun14.Text = "";
            txtDescripcion.Text = "";
            txtNumeroArticulo.Text = "";
            txtCantidadPallet.Text = "";
            imgCodigoBarras.Source = null;

            // Mostrar mensaje de "no hay vista previa"
            MostrarMensajeNoPreview(true);
        }

        /// <summary>
        /// Muestra u oculta el mensaje de "no hay vista previa"
        /// </summary>
        private void MostrarMensajeNoPreview(bool mostrar)
        {
            txtNoPreview.Visibility = mostrar ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}