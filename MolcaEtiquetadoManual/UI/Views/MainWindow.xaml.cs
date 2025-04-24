using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
// UI/Views/MainWindow.xaml.cs
using MolcaEtiquetadoManual.Core.Interfaces;
using MolcaEtiquetadoManual.Core.Models;
using System;
using System.Windows;
using System.Windows.Input;
using MolcaEtiquetadoManual.Core.Services;
using System.Collections.ObjectModel;

namespace MolcaEtiquetadoManual.UI.Views
{
    
    public partial class MainWindow : Window
    {
        private readonly IEtiquetadoService _etiquetadoService;
        private readonly Usuario _currentUser;
        private OrdenProduccion _currentOrden;
        private string _expectedBarcode;
    
        private readonly IUsuarioService _usuarioService;
        private readonly IPrintService _printService;
        

        public MainWindow(Usuario currentUser, IEtiquetadoService etiquetadoService,
                      IUsuarioService usuarioService, IPrintService printService)
        {
            InitializeComponent();

            _currentUser = currentUser;
            _etiquetadoService = etiquetadoService;
            _usuarioService = usuarioService;
            _printService = printService;
            // Inicializar el ListView
            ActivityLog.ItemsSource = new ObservableCollection<ActivityLogItem>();

            txtUsuarioActual.Text = $"Usuario: {_currentUser.NombreUsuario}";

        

            _currentUser = currentUser;
            _etiquetadoService = etiquetadoService;

            txtUsuarioActual.Text = $"Usuario: {_currentUser.NombreUsuario}";

            // Poner foco en el campo DUN14
            txtDun14.Focus();
        }

        private void BtnCerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            var loginWindow = new LoginWindow(_usuarioService, _etiquetadoService, _printService);
            loginWindow.Show();
            this.Close();
        }

        private void TxtDun14_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                BuscarOrden();
            }
        }

        private void BtnBuscar_Click(object sender, RoutedEventArgs e)
        {
            BuscarOrden();
        }

        private void BuscarOrden()
        {
            string dun14 = txtDun14.Text.Trim();

            if (string.IsNullOrEmpty(dun14))
            {
                MessageBox.Show("Por favor, ingrese o escanee un código DUN14.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Buscar la orden en la base de datos
                var orden = _etiquetadoService.BuscarOrdenPorDun14(dun14);

                if (orden != null)
                {
                    // Guardar la orden actual
                    _currentOrden = orden;

                    // Mostrar datos de la orden
                    MostrarDatosOrden(orden);

                    // Habilitar el botón de impresión
                    btnImprimirEtiqueta.IsEnabled = true;

                    AddActivityLog( "Orden encontrada. Listo para imprimir.");
                }
                else
                {
                    LimpiarDatosOrden();
                    AddActivityLog( "No se encontró ninguna orden con ese código DUN14.");
                    MessageBox.Show("No se encontró ninguna orden con ese código DUN14.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                AddActivityLog( $"Error al buscar la orden: {ex.Message}");
                MessageBox.Show($"Error al buscar la orden: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void AddActivityLog(string message)
        {
            var logItem = new ActivityLogItem
            {
                Description = message,
                Time = DateTime.Now.ToString("HH:mm:ss")
            };

            // Si aún no has inicializado la lista, hazlo
            if (ActivityLog.ItemsSource == null)
            {
                ActivityLog.ItemsSource = new ObservableCollection<ActivityLogItem>();
            }

            // Agrega el item a la lista
            var items = ActivityLog.ItemsSource as ObservableCollection<ActivityLogItem>;
            items.Insert(0, logItem); // Inserta al principio para que los más recientes estén arriba
        }
        private void MostrarDatosOrden(OrdenProduccion orden)
        {
            //txtNumeroArticulo.Text = orden.NumeroArticulo;
            txtDescripcion.Text = orden.Descripcion;
            txtCantidadPallet.Text = orden.CantidadPorPallet.ToString();
            //txtProgramaProduccion.Text = orden.ProgramaProduccion;
            //txtTurno.Text = orden.Turno;
            //txtLote.Text = orden.Lote;
            txtFechaProduccion.Text = orden.FechaProduccionInicio.ToString("dd/MM/yyyy");
            //txtFechaCaducidad.Text = orden.FechaCaducidad.ToString("dd/MM/yyyy");
        }

        private void LimpiarDatosOrden()
        {
            //txtNumeroArticulo.Text = string.Empty;
            txtDescripcion.Text = string.Empty;
            txtCantidadPallet.Text = string.Empty;
            //txtProgramaProduccion.Text = string.Empty;
            //txtTurno.Text = string.Empty;
            //txtLote.Text = string.Empty;
            txtFechaProduccion.Text = string.Empty;
            //txtFechaCaducidad.Text = string.Empty;

            btnImprimirEtiqueta.IsEnabled = false;
            txtCodigoVerificacion.IsEnabled = false;
            btnVerificar.IsEnabled = false;

            _currentOrden = null;
        }

        private void BtnImprimirEtiqueta_Click(object sender, RoutedEventArgs e)
        {
            if (_currentOrden == null)
            {
                MessageBox.Show("No hay una orden seleccionada para imprimir.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Generar código de barras según especificación
                _expectedBarcode = GenerarCodigoBarras(_currentOrden);

                // Imprimir etiqueta (aquí implementarías el código real de impresión)
                bool impresionExitosa = ImprimirEtiqueta(_currentOrden, _expectedBarcode);

                if (impresionExitosa)
                {
                    AddActivityLog("Etiqueta impresa. Por favor escanee el código para verificar.");

                    // Habilitar controles de verificación
                    txtCodigoVerificacion.IsEnabled = true;
                    btnVerificar.IsEnabled = true;
                    txtCodigoVerificacion.Focus();
                }
                else
                {
                    AddActivityLog( "Error al imprimir la etiqueta. Intente nuevamente.");
                }
            }
            catch (Exception ex)
            {
                AddActivityLog( $"Error al imprimir la etiqueta: {ex.Message}");
                MessageBox.Show($"Error al imprimir la etiqueta: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GenerarCodigoBarras(OrdenProduccion orden)
        {
            // Según la especificación del documento
            string numeroArticulo = orden.NumeroArticulo.PadRight(8, '0'); // 8 caracteres
            string fechaVencimiento = orden.FechaProduccionInicio.AddDays(orden.DiasCaducidad).ToString("ddMMyy");  // 6 caracteres
            string cantidadPallet = orden.CantidadPorPallet.ToString("D4"); // 4 caracteres
            string fechaDeclaracion = DateTime.Now.ToString("ddMMyy"); // 6 caracteres
            string horaDeclaracion = DateTime.Now.ToString("HHmmss"); // 6 caracteres
            string lote = orden.Lote.PadRight(7, '0'); // 7 caracteres

            // Concatenar según la estructura definida en la especificación
            string codigoBarras = $"{numeroArticulo}{fechaVencimiento}{cantidadPallet}{fechaDeclaracion}{horaDeclaracion}{lote}";

            return codigoBarras;
        }

        private bool ImprimirEtiqueta(OrdenProduccion orden, string codigoBarras)
        {
            // Aquí implementarías el código real para enviar a la impresora
            // Por ahora, solo simulamos una impresión exitosa

            MessageBox.Show($"Simulando impresión de etiqueta:\n\n" +
                           $"Artículo: {orden.NumeroArticulo} - {orden.Descripcion}\n" +
                           $"Cantidad: {orden.CantidadPorPallet}\n" +
                           $"Lote: {orden.Lote}\n" +
                           $"Código de barras: {codigoBarras}",
                           "Impresión Simulada", MessageBoxButton.OK, MessageBoxImage.Information);

            return true;
        }

        private void TxtCodigoVerificacion_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                VerificarCodigoBarras();
            }
        }

        private void BtnVerificar_Click(object sender, RoutedEventArgs e)
        {
            VerificarCodigoBarras();
        }

        private void VerificarCodigoBarras()
        {
            string codigoEscaneado = txtCodigoVerificacion.Text.Trim();

            if (string.IsNullOrEmpty(codigoEscaneado))
            {
                MessageBox.Show("Por favor, escanee el código de barras de la etiqueta.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (codigoEscaneado == _expectedBarcode)
            {
                try
                {
                    // Guardar en la tabla de salida
                    var etiquetaGenerada = new EtiquetaGenerada
                    {
                        EDUS = "KETAN1", // O cualquier identificador del sistema de etiquetado
                        EDDT = DateTime.Now,
                        EDTN = DateTime.Now.ToString("MMdd"), // Por ejemplo: 1125 para 25 de noviembre
                        EDLN = _etiquetadoService.ObtenerSiguienteNumeroSecuencial(),
                        DOCO = _currentOrden.ProgramaProduccion,
                        LITM = _currentOrden.NumeroArticulo,
                        SOQS = _currentOrden.CantidadPorPallet,
                        UOM1 = "PK", // O la unidad de medida que corresponda
                        LOTN = _currentOrden.Lote,
                        EXPR = _currentOrden.FechaProduccionInicio.AddDays(_currentOrden.DiasCaducidad),
                        TDAY = DateTime.Now.ToString("HHmmss"),
                        //SHFT = _currentOrden.Turno,
                        URDT = _currentOrden.FechaProduccionInicio.AddDays(_currentOrden.DiasCaducidad),
                        UsuarioId = _currentUser.Id,
                        ESTADO = "1", // O cualquier estado que necesites
                        URRF = "", // O cualquier referencia que necesites
                        FechaCreacion = DateTime.Now,
                        Confirmada = true
                    };

                    _etiquetadoService.GuardarEtiqueta(etiquetaGenerada);

                    MessageBox.Show("¡Etiqueta verificada y registrada con éxito!", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Limpiar y preparar para una nueva etiqueta
                    txtDun14.Text = string.Empty;
                    txtCodigoVerificacion.Text = string.Empty;
                    LimpiarDatosOrden();
                    AddActivityLog( "Etiqueta registrada. Listo para un nuevo proceso.");

                    // Poner foco en el campo DUN14
                    txtDun14.Focus();
                }
                catch (Exception ex)
                {
                    AddActivityLog( $"Error al registrar la etiqueta: {ex.Message}");
                    MessageBox.Show($"Error al registrar la etiqueta: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                AddActivityLog( "¡Error! El código escaneado no coincide con la etiqueta generada.");
                MessageBox.Show("El código escaneado no coincide con la etiqueta generada. Por favor, verifique e intente nuevamente.", "Error de verificación", MessageBoxButton.OK, MessageBoxImage.Error);
                txtCodigoVerificacion.Text = string.Empty;
                txtCodigoVerificacion.Focus();
            }
        }
    }
}