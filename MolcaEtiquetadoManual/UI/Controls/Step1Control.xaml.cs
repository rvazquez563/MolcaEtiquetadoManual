using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MolcaEtiquetadoManual.Core.Interfaces;
using MolcaEtiquetadoManual.Core.Models;

namespace MolcaEtiquetadoManual.UI.Controls
{
    /// <summary>
    /// Control para el primer paso: Escaneo de código DUN14
    /// </summary>
    public partial class Step1Control : UserControl
    {
        private readonly IEtiquetadoService _etiquetadoService;
        private readonly ILogService _logService;

        // Evento para notificar cuando se encuentra una orden
        public event EventHandler<OrdenProduccionEventArgs> OrdenEncontrada;

        // Evento para notificar actividad
        public event Action<string, ActivityLogItem.LogLevel> ActivityLog;

        public Step1Control(IEtiquetadoService etiquetadoService, ILogService logService)
        {
            InitializeComponent();

            _etiquetadoService = etiquetadoService ?? throw new ArgumentNullException(nameof(etiquetadoService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));

            Loaded += (s, e) => txtDun14.Focus();

            // Asegurar que el control de texto recibe el foco cuando el control se hace visible
            IsVisibleChanged += (s, e) =>
            {
                if ((bool)e.NewValue)
                {
                    Dispatcher.BeginInvoke(new Action(() => txtDun14.Focus()));
                }
            };
        }

        private void BtnBuscar_Click(object sender, RoutedEventArgs e)
        {
            BuscarOrden();
        }

        private void TxtDun14_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BuscarOrden();
            }
        }

        private void BuscarOrden()
        {
            string dun14 = txtDun14.Text?.Trim();

            // Mostrar el indicador de carga
            progressBar.Visibility = Visibility.Visible;
            txtError.Text = string.Empty;
            btnBuscar.IsEnabled = false;

            try
            {
                // Validación básica
                if (string.IsNullOrEmpty(dun14))
                {
                    MostrarError("Por favor, ingrese o escanee un código DUN14.");
                    txtDun14.Focus();
                    return;
                }

                // Registrar actividad
                ActivityLog?.Invoke($"Buscando orden con DUN14: {dun14}...", ActivityLogItem.LogLevel.Info);

                // Intentar buscar la orden en la base de datos
                try
                {
                    var orden = _etiquetadoService.BuscarOrdenPorDun14(dun14);

                    if (orden != null)
                    {
                        // Registrar éxito en el log
                        _logService.Information("Orden encontrada con DUN14: {DUN14}, Descripción: {Descripcion}",
                            dun14, orden.Descripcion);
                        ActivityLog?.Invoke($"Orden encontrada: {orden.Descripcion}", ActivityLogItem.LogLevel.Info);

                        // Notificar que se encontró una orden
                        OrdenEncontrada?.Invoke(this, new OrdenProduccionEventArgs(orden));

                        // Limpiar el campo para el próximo escaneo
                        txtDun14.Text = string.Empty;
                    }
                    else
                    {
                        // Para la versión de prueba, crearemos una orden ficticia
                        // En producción, esta parte no existiría
                        //var ordenFicticia = new OrdenProduccion
                        //{
                        //    IdLogico = 1,
                        //    Id = 123456,
                        //    NumeroArticulo = "00012345",
                        //    Descripcion = "HARINA 000 - BOLSA 1KG",
                        //    UnidadMedida = "UN",
                        //    ProgramaProduccion = "557525",
                        //    DiasCaducidad = 90,
                        //    DUN14 = dun14,
                        //    FechaProduccionInicio = DateTime.Today,
                        //    FechaProduccionFin = DateTime.Today.AddDays(1),
                        //    CantidadPorPallet = 48
                        //};

                        // Registrar en el log
                        // _logService.Information("Creando orden ficticia para pruebas: {DUN14}", dun14);
                        _logService.Warning("Error de BD, usando orden ficticia para pruebas: {DUN14}", dun14);
                        ActivityLog?.Invoke($"Orden no encontrada con el codigo {dun14}!",
                            ActivityLogItem.LogLevel.Error);
                        // Notificar que se "encontró" una orden
                        //OrdenEncontrada?.Invoke(this, new OrdenProduccionEventArgs(ordenFicticia));

                        // Limpiar el campo para el próximo escaneo
                        txtDun14.Text = string.Empty;
                    }
                }
                catch (Exception dbEx)
                {
                    // Error al buscar en la base de datos, pero no queremos que falle todo el programa
                    _logService.Error(dbEx, "Error al buscar en la base de datos: {Error}", dbEx.Message);

                    // Para la versión de prueba, creamos una orden ficticia si hay error en la BD
                    //var ordenFicticia = new OrdenProduccion
                    //{
                    //    IdLogico = 1,
                    //    Id = 123456,
                    //    NumeroArticulo = "00012345",
                    //    Descripcion = "HARINA 000 - BOLSA 1KG (BD ERROR)",
                    //    UnidadMedida = "UN",
                    //    ProgramaProduccion = "557525",
                    //    DiasCaducidad = 90,
                    //    DUN14 = dun14,
                    //    FechaProduccionInicio = DateTime.Today,
                    //    FechaProduccionFin = DateTime.Today.AddDays(1),
                    //    CantidadPorPallet = 48
                    //};

                    // Registrar en el log
                    _logService.Warning("Error de BD, usando orden ficticia para pruebas: {DUN14}", dun14);
                    ActivityLog?.Invoke($"Orden no encontrada con el codigo {dun14}! :+{dbEx.Message}",
                        ActivityLogItem.LogLevel.Error);

                    // Notificar que se "encontró" una orden
                    //OrdenEncontrada?.Invoke(this, new OrdenProduccionEventArgs(ordenFicticia));

                    // Limpiar el campo para el próximo escaneo
                    txtDun14.Text = string.Empty;
                }
            }
            catch (Exception ex)
            {
                // Error general en la búsqueda
                _logService.Error(ex, "Error al buscar orden con DUN14: {DUN14}", dun14);
                MostrarError($"Error al buscar la orden: {ex.Message}");
                ActivityLog?.Invoke($"Error al buscar la orden: {ex.Message}", ActivityLogItem.LogLevel.Error);
            }
            finally
            {
                // Ocultar el indicador de carga y habilitar el botón
                progressBar.Visibility = Visibility.Collapsed;
                btnBuscar.IsEnabled = true;
                txtDun14.Focus();
            }
        }

        private void MostrarError(string mensaje)
        {
            txtError.Text = mensaje;
            progressBar.Visibility = Visibility.Collapsed;
            btnBuscar.IsEnabled = true;
        }

        /// <summary>
        /// Prepara el control para un nuevo escaneo
        /// </summary>
        public void Reiniciar()
        {
            txtDun14.Text = string.Empty;
            txtError.Text = string.Empty;
            progressBar.Visibility = Visibility.Collapsed;
            btnBuscar.IsEnabled = true;
            txtDun14.Focus();
        }
    }

    /// <summary>
    /// Clase para los datos del evento de orden encontrada
    /// </summary>
    public class OrdenProduccionEventArgs : EventArgs
    {
        public OrdenProduccion Orden { get; }

        public OrdenProduccionEventArgs(OrdenProduccion orden)
        {
            Orden = orden;
        }
    }
}