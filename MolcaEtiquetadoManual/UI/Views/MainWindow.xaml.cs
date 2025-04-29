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
        private readonly ILogService _logService;
        private OrdenProduccion _currentOrden;
        private string _expectedBarcode;
        private EtiquetaGenerada etiactual;
        private readonly IUsuarioService _usuarioService;
        private readonly IPrintService _printService;
        private readonly ITurnoService _turnoService;

        public MainWindow(Usuario currentUser, IEtiquetadoService etiquetadoService,
                      IUsuarioService usuarioService, IPrintService printService,
                      ITurnoService turnoService, ILogService logService)
        {
            InitializeComponent();

            _currentUser = currentUser;
            _etiquetadoService = etiquetadoService;
            _usuarioService = usuarioService;
            _printService = printService;
            _turnoService = turnoService;
            _logService = logService;

            // Inicializar el ListView
            ActivityLog.ItemsSource = new ObservableCollection<ActivityLogItem>();

            txtUsuarioActual.Text = $"Usuario: {_currentUser.NombreUsuario}";

            _logService.Information("Sesión iniciada - Usuario: {Username}", _currentUser.NombreUsuario);

            // Poner foco en el campo DUN14
            txtDun14.Focus();
        }
        private void BtnCerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            _logService.Information("Sesión cerrada - Usuario: {Username}", _currentUser.NombreUsuario);

            var loginWindow = new LoginWindow(_usuarioService, _etiquetadoService, _printService, _turnoService, _logService);
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
                _logService.Warning("Intento de búsqueda con DUN14 vacío");
                MessageBox.Show("Por favor, ingrese o escanee un código DUN14.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _logService.Information("Buscando orden con DUN14: {DUN14}", dun14);

                // Buscar la orden en la base de datos
                var orden = _etiquetadoService.BuscarOrdenPorDun14(dun14);

                if (orden != null)
                {
                    // Guardar la orden actual
                    _currentOrden = orden;

                    _logService.Information("Orden encontrada. ID: {ID}, Descripción: {Descripcion}",
                        orden.Id, orden.Descripcion);

                    // Mostrar datos de la orden
                    MostrarDatosOrden(orden);

                    // Habilitar el botón de impresión
                    btnImprimirEtiqueta.IsEnabled = true;

                    AddActivityLog("Orden encontrada. Listo para imprimir.");
                }
                else
                {
                    _logService.Warning("No se encontró orden con DUN14: {DUN14}", dun14);
                    LimpiarDatosOrden();
                    AddActivityLog("No se encontró ninguna orden con ese código DUN14.");
                    MessageBox.Show("No se encontró ninguna orden con ese código DUN14.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al buscar orden con DUN14: {DUN14}", dun14);
                AddActivityLog($"Error al buscar la orden: {ex.Message}");
                MessageBox.Show($"Error al buscar la orden: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void AddActivityLog(string message, ActivityLogItem.LogLevel level = ActivityLogItem.LogLevel.Info)
        {
            var logItem = new ActivityLogItem
            {
                Description = message,
                Time = DateTime.Now.ToString("HH:mm:ss"),
                Level = level
            };

            // Si aún no has inicializado la lista, hazlo
            if (ActivityLog.ItemsSource == null)
            {
                ActivityLog.ItemsSource = new ObservableCollection<ActivityLogItem>();
            }

            // Agrega el item a la lista
            var items = ActivityLog.ItemsSource as ObservableCollection<ActivityLogItem>;
            items.Insert(0, logItem); // Inserta al principio
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
                var (turnoActual, fechaProduccion) = _turnoService.ObtenerTurnoYFechaProduccion();
                string numeroTransaccion = _turnoService.ObtenerNumeroTransaccion(fechaProduccion);
                // Generar código de barras según especificación
                _expectedBarcode = GenerarCodigoBarras(_currentOrden, turnoActual, fechaProduccion);
                var etiquetaGenerada = new EtiquetaGenerada
                {
                    EDUS = _currentUser.NombreUsuario, // O cualquier identificador del sistema de etiquetado
                    EDDT = PasarFechaJuliana(fechaProduccion.ToString("ddMMyy")),//fecha juliana 
                    EDTN = fechaProduccion.ToString("MMdd"), // Por ejemplo: 1125 para 25 de noviembre
                    EDLN = _etiquetadoService.ObtenerSiguienteNumeroSecuencialdeldia(PasarFechaJuliana(fechaProduccion.ToString("ddMMyy"))),
                    DOCO = _currentOrden.ProgramaProduccion,
                    LITM = _currentOrden.NumeroArticulo,
                    SOQS = _currentOrden.CantidadPorPallet,
                    UOM1 = _currentOrden.UnidadMedida, // O la unidad de medida que corresponda
                    LOTN = _currentOrden.ProgramaProduccion + turnoActual,
                    EXPR = fechaProduccion.AddDays(_currentOrden.DiasCaducidad),
                    TDAY = fechaProduccion.ToString("HHmmss"),
                    SHFT = turnoActual,
                    URDT = _currentOrden.FechaProduccionInicio.AddDays(_currentOrden.DiasCaducidad),//no se sabe este
                    SEC = _etiquetadoService.ObtenerSiguienteNumeroSecuencial(_currentOrden.ProgramaProduccion),
                    ESTADO = "1", // O cualquier estado que necesites
                    URRF = _currentOrden.DUN14, // O cualquier referencia que necesites
                    FechaCreacion = DateTime.Now,
                    Confirmada = false
                };
                etiactual = etiquetaGenerada;
                // Imprimir etiqueta (aquí implementarías el código real de impresión)
                _expectedBarcode = GenerarCodigoBarras(_currentOrden, turnoActual, fechaProduccion);
                bool impresionExitosa = ImprimirEtiqueta(etiquetaGenerada, _expectedBarcode);

                if (impresionExitosa)
                {
                    AddActivityLog("Etiqueta impresa. Por favor escanee el código para verificar.", ActivityLogItem.LogLevel.Info);

                    // Habilitar controles de verificación
                    txtCodigoVerificacion.IsEnabled = true;
                    btnVerificar.IsEnabled = true;
                    txtCodigoVerificacion.Focus();
                }
                else
                {
                    AddActivityLog( "Error al imprimir la etiqueta. Intente nuevamente.", ActivityLogItem.LogLevel.Error);
                }
            }
            catch (Exception ex)
            {
                AddActivityLog( $"Error al imprimir la etiqueta: {ex.Message}", ActivityLogItem.LogLevel.Error);
                MessageBox.Show($"Error al imprimir la etiqueta: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        public static string PasarFechaJuliana(string fecha)
        {
            // La fecha llega como DDMMYY y la pasamos a juliana
            // 114154
            // Primer carácter: CENTURIA (1 es desde 2000)
            // Carácter dos y tres: AÑO (14)
            // TRES ULTIMOS CARACTERES: nro del día del año: 154 ES 03 de junio
            try
            {
                // Validar que la fecha tenga el formato correcto
                if (string.IsNullOrEmpty(fecha) || fecha.Length != 6)
                    throw new ArgumentException("La fecha debe tener formato DDMMYY");

                // Extraer día, mes y año
                int dia = int.Parse(fecha.Substring(0, 2));
                int mes = int.Parse(fecha.Substring(2, 2));
                int año = 2000 + int.Parse(fecha.Substring(4, 2));

                // Crear fecha
                DateTime fechaAUsar = new DateTime(año, mes, dia);

                // Calcular día juliano
                int diaDelAño = fechaAUsar.DayOfYear;
                string sDia = diaDelAño.ToString("000");
                string sAño = fechaAUsar.ToString("yy");

                // Retornar fecha juliana
                return "1" + sAño + sDia;
            }
            catch (Exception ex)
            {

                throw new Exception($"Error al convertir fecha a formato juliano: {ex.Message}");
            }
        }
        private string GenerarCodigoBarras(OrdenProduccion orden, string turnoActual, DateTime fechaProduccion)
        {
            // Según la especificación del documento
            string numeroArticulo = orden.NumeroArticulo.PadRight(8, '0'); // 8 caracteres
            string fechaVencimiento = fechaProduccion.AddDays(orden.DiasCaducidad).ToString("ddMMyy");  // 6 caracteres
            string cantidadPallet = orden.CantidadPorPallet.ToString("D4"); // 4 caracteres
            string fechaDeclaracion = fechaProduccion.ToString("ddMMyy"); // 6 caracteres
            string horaDeclaracion = fechaProduccion.ToString("HHmmss"); // 6 caracteres
            string lote = (orden.ProgramaProduccion+turnoActual).PadRight(7, '0'); // 7 caracteres

            // Concatenar según la estructura definida en la especificación
            string codigoBarras = $"{numeroArticulo}{fechaVencimiento}{cantidadPallet}{fechaDeclaracion}{horaDeclaracion}{lote}";

            return codigoBarras;
        }

        private bool ImprimirEtiqueta(EtiquetaGenerada etiqueta, string codigoBarras)
        {
            // Aquí implementarías el código real para enviar a la impresora
            // Por ahora, solo simulamos una impresión exitosa

            MessageBox.Show($"Simulando impresión de etiqueta:\n\n" +
                           $"Artículo: {etiqueta.LITM} - \n" +
                           $"Cantidad: {etiqueta.SOQS}\n" +
                           $"Lote: {etiqueta.LOTN}\n" +
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

                    // string numeroTransaccion = _turnoService.ObtenerNumeroTransaccion(fechaProduccion);

                    // Guardar en la tabla de salida
                    var etiquetaGenerada = etiactual;
                    etiquetaGenerada.Confirmada = true;
                    _etiquetadoService.GuardarEtiquetaConStoredProcedure(etiquetaGenerada);
                    MessageBox.Show("¡Etiqueta verificada y registrada con éxito!", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Limpiar y preparar para una nueva etiqueta
                    txtDun14.Text = string.Empty;
                    txtCodigoVerificacion.Text = string.Empty;
                    LimpiarDatosOrden();
                    AddActivityLog( "Etiqueta registrada. Listo para un nuevo proceso.", ActivityLogItem.LogLevel.Info);

                    // Poner foco en el campo DUN14
                    txtDun14.Focus();
                }
                catch (Exception ex)
                {
                    AddActivityLog( $"Error al registrar la etiqueta: {ex.Message}", ActivityLogItem.LogLevel.Error);
                    MessageBox.Show($"Error al registrar la etiqueta: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                AddActivityLog( "¡Error! El código escaneado no coincide con la etiqueta generada.", ActivityLogItem.LogLevel.Error);
                MessageBox.Show("El código escaneado no coincide con la etiqueta generada. Por favor, verifique e intente nuevamente.", "Error de verificación", MessageBoxButton.OK, MessageBoxImage.Error);
                txtCodigoVerificacion.Text = string.Empty;
                txtCodigoVerificacion.Focus();
            }
        }
    }
}