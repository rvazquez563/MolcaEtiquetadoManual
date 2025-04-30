// UI/Views/MainWindow.xaml.cs - Constructor y métodos principales actualizados
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MolcaEtiquetadoManual.Core.Interfaces;
using MolcaEtiquetadoManual.Core.Models;
using Microsoft.Extensions.Configuration;

namespace MolcaEtiquetadoManual.UI.Views
{
    public partial class MainWindow : Window
    {
        private readonly IEtiquetadoService _etiquetadoService;
        private readonly IUsuarioService _usuarioService;
        private readonly IPrintService _printService;
        private readonly ITurnoService _turnoService;
        private readonly ILogService _logService;
        private readonly IBarcodeService _barcodeService;
        private readonly IJulianDateService _julianDateService;
        private readonly IConfiguration _configuration;

        private Usuario _currentUser;
        private OrdenProduccion _currentOrden;
        private EtiquetaGenerada _etiquetaActual;
        private string _expectedBarcode;

        // Colección observable para la lista de actividad
        public ObservableCollection<ActivityLogItem> ActivityItems { get; set; }

        // Constructor seguro con validación de nulos
        public MainWindow(Usuario currentUser,
                        IEtiquetadoService etiquetadoService,
                        IUsuarioService usuarioService,
                        IPrintService printService,
                        ITurnoService turnoService,
                        ILogService logService,
                        IBarcodeService barcodeService = null,
                        IJulianDateService julianDateService = null,
                        IConfiguration configuration = null)
        {
            InitializeComponent();

            // Validación explícita para evitar NullReferenceException
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _etiquetadoService = etiquetadoService ?? throw new ArgumentNullException(nameof(etiquetadoService));
            _usuarioService = usuarioService ?? throw new ArgumentNullException(nameof(usuarioService));
            _printService = printService ?? throw new ArgumentNullException(nameof(printService));
            _turnoService = turnoService ?? throw new ArgumentNullException(nameof(turnoService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));

            // Estos pueden ser opcionales, así que no lanzamos excepciones
            _barcodeService = barcodeService;
            _julianDateService = julianDateService;
            _configuration = configuration;

            // Inicializar la colección de actividad
            ActivityItems = new ObservableCollection<ActivityLogItem>();
            lvActividad.ItemsSource = ActivityItems;

            // Establecer DataContext para binding
            DataContext = this;

            // Mostrar información del usuario
            txtUsuarioActual.Text = $"Usuario: {_currentUser.NombreUsuario} [{_currentUser.Rol}]";

            // Mostrar versión de la aplicación - MANEJO SEGURO DE CONFIGURACIÓN NULA
            string appVersion = "1.0.0"; // Valor predeterminado
            string companyName = "Molca"; // Valor predeterminado

            // Solo intentar leer la configuración si no es nula
            if (_configuration != null)
            {
                try
                {
                    var appSettings = _configuration.GetSection("AppSettings");
                    if (appSettings != null)
                    {
                        appVersion = appSettings["Version"] ?? appVersion;
                        companyName = appSettings["Company"] ?? companyName;
                    }
                }
                catch (Exception ex)
                {
                    _logService.Error(ex, "Error al leer la configuración de la aplicación");
                }
            }

            txtVersion.Text = $"{companyName} - Sistema de Etiquetado Manual v{appVersion}";

            // Registrar inicio de sesión
            _logService.Information("Sesión iniciada - Usuario: {Username}, Rol: {Role}",
                _currentUser.NombreUsuario, _currentUser.Rol);

            AddActivityLogItem("Bienvenido al Sistema de Etiquetado Manual", ActivityLogItem.LogLevel.Info);
            AddActivityLogItem("Escanee un código DUN-14 para comenzar", ActivityLogItem.LogLevel.Info);

            // Poner foco en el campo DUN14
            txtDun14.Focus();
        }

        private void BtnCerrarSesion_Click(object sender, RoutedEventArgs e)
        {
            _logService.Information("Sesión cerrada - Usuario: {Username}", _currentUser.NombreUsuario);

            var loginWindow = new LoginWindow(_usuarioService, _etiquetadoService, _printService,
                                            _turnoService, _logService, _barcodeService,
                                            _julianDateService, _configuration);
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

        private void BtnImprimirEtiqueta_Click(object sender, RoutedEventArgs e)
        {
            ImprimirEtiqueta();
        }

        private void AddActivityLogItem(string message, ActivityLogItem.LogLevel level = ActivityLogItem.LogLevel.Info)
        {
            // Crear un nuevo ítem de log
            var logItem = new ActivityLogItem
            {
                Description = message,
                Time = DateTime.Now.ToString("HH:mm:ss"),
                Level = level
            };

            // Agregar al inicio para que los más recientes estén arriba
            Application.Current.Dispatcher.Invoke(() =>
            {
                ActivityItems.Insert(0, logItem);
            });

            // También actualizamos el estado para mostrar el mensaje más reciente
            txtEstado.Text = message;

            // Registrar en el log del sistema según el nivel
            switch (level)
            {
                case ActivityLogItem.LogLevel.Info:
                    _logService.Information(message);
                    break;
                case ActivityLogItem.LogLevel.Warning:
                    _logService.Warning(message);
                    break;
                case ActivityLogItem.LogLevel.Error:
                    _logService.Error(message);
                    break;
                case ActivityLogItem.LogLevel.Debug:
                    _logService.Debug(message);
                    break;
            }
        }

        private void BuscarOrden()
        {
            string dun14 = txtDun14.Text.Trim();

            if (string.IsNullOrEmpty(dun14))
            {
                AddActivityLogItem("Por favor, ingrese o escanee un código DUN14.", ActivityLogItem.LogLevel.Warning);
                MessageBox.Show("Por favor, ingrese o escanee un código DUN14.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                AddActivityLogItem($"Buscando orden con DUN14: {dun14}...", ActivityLogItem.LogLevel.Info);

                // Buscar la orden en la base de datos
                var orden = _etiquetadoService.BuscarOrdenPorDun14(dun14);

                if (orden != null)
                {
                    // Guardar la orden actual
                    _currentOrden = orden;

                    AddActivityLogItem($"Orden encontrada: {orden.Descripcion}", ActivityLogItem.LogLevel.Info);

                    // Mostrar datos de la orden
                    MostrarDatosOrden(orden);

                    // Habilitar el botón de impresión
                    btnImprimirEtiqueta.IsEnabled = true;
                }
                else
                {
                    AddActivityLogItem($"No se encontró ninguna orden con el código DUN14: {dun14}", ActivityLogItem.LogLevel.Warning);
                    LimpiarCampos();
                    MessageBox.Show("No se encontró ninguna orden con ese código DUN14.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                AddActivityLogItem($"Error al buscar la orden: {ex.Message}", ActivityLogItem.LogLevel.Error);
                _logService.Error(ex, "Error al buscar orden con DUN14: {DUN14}", dun14);
                MessageBox.Show($"Error al buscar la orden: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MostrarDatosOrden(OrdenProduccion orden)
        {
            // Obtener información del turno actual
            var (turnoActual, fechaProduccion) = _turnoService.ObtenerTurnoYFechaProduccion();

            // Mostrar los datos en los campos de texto
            txtNumeroArticulo.Text = orden.NumeroArticulo;
            txtDescripcion.Text = orden.Descripcion;
            txtCantidadPallet.Text = orden.CantidadPorPallet.ToString();
            txtProgramaProduccion.Text = orden.ProgramaProduccion;
            txtTurno.Text = turnoActual;
            txtLote.Text = $"{orden.ProgramaProduccion}{turnoActual}";
            txtFechaProduccion.Text = fechaProduccion.ToString("dd/MM/yyyy");
            txtFechaCaducidad.Text = fechaProduccion.AddDays(orden.DiasCaducidad).ToString("dd/MM/yyyy");
        }

        private void LimpiarCampos()
        {
            // Limpiar los campos de texto
            txtNumeroArticulo.Text = string.Empty;
            txtDescripcion.Text = string.Empty;
            txtCantidadPallet.Text = string.Empty;
            txtProgramaProduccion.Text = string.Empty;
            txtTurno.Text = string.Empty;
            txtLote.Text = string.Empty;
            txtFechaProduccion.Text = string.Empty;
            txtFechaCaducidad.Text = string.Empty;

            // Deshabilitar botones
            btnImprimirEtiqueta.IsEnabled = false;
            txtCodigoVerificacion.IsEnabled = false;
            btnVerificar.IsEnabled = false;

            // Limpiar variables
            _currentOrden = null;
            _etiquetaActual = null;
            _expectedBarcode = null;
        }

        private void ImprimirEtiqueta()
        {
            if (_currentOrden == null)
            {
                AddActivityLogItem("No hay una orden seleccionada para imprimir.", ActivityLogItem.LogLevel.Warning);
                MessageBox.Show("No hay una orden seleccionada para imprimir.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Obtener información del turno y fecha
                var (turnoActual, fechaProduccion) = _turnoService.ObtenerTurnoYFechaProduccion();
                string numeroTransaccion = _turnoService.ObtenerNumeroTransaccion(fechaProduccion);

                // Crear la entidad de etiqueta
                _etiquetaActual = CrearEtiquetaGenerada(_currentOrden, turnoActual, fechaProduccion, numeroTransaccion);

                // Generar el código de barras
                _expectedBarcode = _barcodeService.GenerarCodigoBarras(_currentOrden, _etiquetaActual);

                AddActivityLogItem("Enviando etiqueta a la impresora...", ActivityLogItem.LogLevel.Info);

                // Imprimir la etiqueta
                bool resultado = _printService.ImprimirEtiqueta(_currentOrden, _etiquetaActual, _expectedBarcode);

                if (resultado)
                {
                    AddActivityLogItem("Etiqueta enviada a la impresora. Escanee el código para verificar.", ActivityLogItem.LogLevel.Info);

                    // Habilitar controles de verificación
                    txtCodigoVerificacion.IsEnabled = true;
                    btnVerificar.IsEnabled = true;
                    txtCodigoVerificacion.Focus();
                }
                else
                {
                    AddActivityLogItem("Error al enviar la etiqueta a la impresora.", ActivityLogItem.LogLevel.Error);
                    MessageBox.Show("Error al enviar la etiqueta a la impresora. Verifique la conexión e intente nuevamente.",
                        "Error de impresión", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                AddActivityLogItem($"Error al imprimir la etiqueta: {ex.Message}", ActivityLogItem.LogLevel.Error);
                _logService.Error(ex, "Error al imprimir etiqueta para orden: {ProgramaProduccion}",
                    _currentOrden.ProgramaProduccion);
                MessageBox.Show($"Error al imprimir la etiqueta: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private EtiquetaGenerada CrearEtiquetaGenerada(OrdenProduccion orden, string turnoActual,
            DateTime fechaProduccion, string numeroTransaccion)
        {
            // Convertir la fecha a formato juliano
            string fechaJuliana = _julianDateService.ConvertirAFechaJuliana(fechaProduccion);

            // Obtener siguientes números secuenciales
            int numeroSecuencial = _etiquetadoService.ObtenerSiguienteNumeroSecuencialdeldia(fechaJuliana);
            int numeroPallet = _etiquetadoService.ObtenerSiguienteNumeroSecuencial(orden.ProgramaProduccion);

            // Crear la entidad
            return new EtiquetaGenerada
            {
                EDUS = _currentUser.NombreUsuario.Length > 4 ?
                    _currentUser.NombreUsuario.Substring(0, 4) : _currentUser.NombreUsuario.PadRight(4),
                EDDT = fechaJuliana,
                EDTN = numeroTransaccion,
                EDLN = numeroSecuencial,
                DOCO = orden.ProgramaProduccion,
                LITM = orden.NumeroArticulo,
                SOQS = orden.CantidadPorPallet,
                UOM1 = orden.UnidadMedida,
                LOTN = $"{orden.ProgramaProduccion}{turnoActual}",
                EXPR = fechaProduccion.AddDays(orden.DiasCaducidad),
                TDAY = _julianDateService.ConvertirAHoraJuliana(fechaProduccion),
                SHFT = turnoActual,
                URDT = fechaProduccion.AddDays(orden.DiasCaducidad),
                SEC = numeroPallet,
                ESTADO = "1", // 1 = Activa/Pendiente
                URRF = orden.DUN14,
                FechaCreacion = DateTime.Now,
                Confirmada = false
            };
        }
        private void BtnConfigImpresora_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Verificar si el usuario tiene permisos (solo administradores)
                if (_currentUser.Rol != "Administrador")
                {
                    MessageBox.Show("Solo los administradores pueden acceder a la configuración de impresora.",
                        "Acceso Restringido", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Crear y mostrar la ventana de configuración
                var configWindow = new PrinterSettingsWindow(_logService, _configuration);
                configWindow.Owner = this;

                _logService.Information("Abriendo ventana de configuración de impresora - Usuario: {Username}",
                    _currentUser.NombreUsuario);

                // Mostrar como diálogo modal
                configWindow.ShowDialog();

                // Registrar en el historial
                AddActivityLogItem("Se accedió a la configuración de impresora", ActivityLogItem.LogLevel.Info);
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al abrir ventana de configuración de impresora");
                MessageBox.Show($"Error al abrir la configuración: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void VerificarCodigoBarras()
        {
            string codigoEscaneado = txtCodigoVerificacion.Text.Trim();

            if (string.IsNullOrEmpty(codigoEscaneado))
            {
                AddActivityLogItem("Por favor, escanee el código de barras de la etiqueta.", ActivityLogItem.LogLevel.Warning);
                MessageBox.Show("Por favor, escanee el código de barras de la etiqueta.",
                    "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Verificar coincidencia
            bool esValido = _barcodeService.VerificarCodigoBarras(_expectedBarcode, codigoEscaneado);

            if (esValido)
            {
                try
                {
                    // Marcar como confirmada y guardar en la base de datos
                    _etiquetaActual.Confirmada = true;
                    int resultado = _etiquetadoService.GuardarEtiquetaConStoredProcedure(_etiquetaActual);

                    AddActivityLogItem($"¡Etiqueta verificada y registrada con éxito! Nº Pallet: {resultado}",
                        ActivityLogItem.LogLevel.Info);

                    MessageBox.Show($"¡Etiqueta verificada y registrada con éxito!\nNº Pallet: {resultado}",
                        "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Limpiar para una nueva operación
                    txtDun14.Text = string.Empty;
                    txtCodigoVerificacion.Text = string.Empty;
                    LimpiarCampos();

                    // Poner foco en el campo DUN14
                    txtDun14.Focus();
                }
                catch (Exception ex)
                {
                    AddActivityLogItem($"Error al registrar la etiqueta: {ex.Message}", ActivityLogItem.LogLevel.Error);
                    _logService.Error(ex, "Error al registrar etiqueta verificada");
                    MessageBox.Show($"Error al registrar la etiqueta: {ex.Message}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                AddActivityLogItem("¡Error! El código escaneado no coincide con la etiqueta generada.",
                    ActivityLogItem.LogLevel.Error);

                MessageBox.Show("El código escaneado no coincide con la etiqueta generada. " +
                    "Por favor, verifique e intente nuevamente.",
                    "Error de verificación", MessageBoxButton.OK, MessageBoxImage.Error);

                txtCodigoVerificacion.Text = string.Empty;
                txtCodigoVerificacion.Focus();
            }
        }
    }
}
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Data;
//using System.Windows.Documents;
//using System.Windows.Input;
//using System.Windows.Media;
//using System.Windows.Media.Imaging;
//using System.Windows.Shapes;
//// UI/Views/MainWindow.xaml.cs
//using MolcaEtiquetadoManual.Core.Interfaces;
//using MolcaEtiquetadoManual.Core.Models;
//using System;
//using System.Windows;
//using System.Windows.Input;
//using MolcaEtiquetadoManual.Core.Services;
//using System.Collections.ObjectModel;

//namespace MolcaEtiquetadoManual.UI.Views
//{

//    public partial class MainWindow : Window
//    {
//        private readonly IEtiquetadoService _etiquetadoService;
//        private readonly Usuario _currentUser;
//        private readonly ILogService _logService;
//        private OrdenProduccion _currentOrden;
//        private string _expectedBarcode;
//        private EtiquetaGenerada etiactual;
//        private readonly IUsuarioService _usuarioService;
//        private readonly IPrintService _printService;
//        private readonly ITurnoService _turnoService;

//        public MainWindow(Usuario currentUser, IEtiquetadoService etiquetadoService,
//                      IUsuarioService usuarioService, IPrintService printService,
//                      ITurnoService turnoService, ILogService logService)
//        {
//            InitializeComponent();

//            _currentUser = currentUser;
//            _etiquetadoService = etiquetadoService;
//            _usuarioService = usuarioService;
//            _printService = printService;
//            _turnoService = turnoService;
//            _logService = logService;

//            // Inicializar el ListView
//            ActivityLog.ItemsSource = new ObservableCollection<ActivityLogItem>();

//            txtUsuarioActual.Text = $"Usuario: {_currentUser.NombreUsuario}";

//            _logService.Information("Sesión iniciada - Usuario: {Username}", _currentUser.NombreUsuario);

//            // Poner foco en el campo DUN14
//            txtDun14.Focus();
//        }
//        private void BtnCerrarSesion_Click(object sender, RoutedEventArgs e)
//        {
//            _logService.Information("Sesión cerrada - Usuario: {Username}", _currentUser.NombreUsuario);

//            var loginWindow = new LoginWindow(_usuarioService, _etiquetadoService, _printService, _turnoService, _logService);
//            loginWindow.Show();
//            this.Close();
//        }
//        private void TxtDun14_KeyDown(object sender, KeyEventArgs e)
//        {
//            if (e.Key == Key.Enter)
//            {
//                BuscarOrden();
//            }
//        }

//        private void BtnBuscar_Click(object sender, RoutedEventArgs e)
//        {
//            BuscarOrden();
//        }

//        private void BuscarOrden()
//        {
//            string dun14 = txtDun14.Text.Trim();

//            if (string.IsNullOrEmpty(dun14))
//            {
//                _logService.Warning("Intento de búsqueda con DUN14 vacío");
//                MessageBox.Show("Por favor, ingrese o escanee un código DUN14.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }

//            try
//            {
//                _logService.Information("Buscando orden con DUN14: {DUN14}", dun14);

//                // Buscar la orden en la base de datos
//                var orden = _etiquetadoService.BuscarOrdenPorDun14(dun14);

//                if (orden != null)
//                {
//                    // Guardar la orden actual
//                    _currentOrden = orden;

//                    _logService.Information("Orden encontrada. ID: {ID}, Descripción: {Descripcion}",
//                        orden.Id, orden.Descripcion);

//                    // Mostrar datos de la orden
//                    MostrarDatosOrden(orden);

//                    // Habilitar el botón de impresión
//                    btnImprimirEtiqueta.IsEnabled = true;

//                    AddActivityLog("Orden encontrada. Listo para imprimir.");
//                }
//                else
//                {
//                    _logService.Warning("No se encontró orden con DUN14: {DUN14}", dun14);
//                    LimpiarDatosOrden();
//                    AddActivityLog("No se encontró ninguna orden con ese código DUN14.");
//                    MessageBox.Show("No se encontró ninguna orden con ese código DUN14.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
//                }
//            }
//            catch (Exception ex)
//            {
//                _logService.Error(ex, "Error al buscar orden con DUN14: {DUN14}", dun14);
//                AddActivityLog($"Error al buscar la orden: {ex.Message}");
//                MessageBox.Show($"Error al buscar la orden: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//        }
//        private void AddActivityLog(string message, ActivityLogItem.LogLevel level = ActivityLogItem.LogLevel.Info)
//        {
//            var logItem = new ActivityLogItem
//            {
//                Description = message,
//                Time = DateTime.Now.ToString("HH:mm:ss"),
//                Level = level
//            };

//            // Si aún no has inicializado la lista, hazlo
//            if (ActivityLog.ItemsSource == null)
//            {
//                ActivityLog.ItemsSource = new ObservableCollection<ActivityLogItem>();
//            }

//            // Agrega el item a la lista
//            var items = ActivityLog.ItemsSource as ObservableCollection<ActivityLogItem>;
//            items.Insert(0, logItem); // Inserta al principio
//        }
//        private void MostrarDatosOrden(OrdenProduccion orden)
//        {
//            //txtNumeroArticulo.Text = orden.NumeroArticulo;
//            txtDescripcion.Text = orden.Descripcion;
//            txtCantidadPallet.Text = orden.CantidadPorPallet.ToString();
//            //txtProgramaProduccion.Text = orden.ProgramaProduccion;
//            //txtTurno.Text = orden.Turno;
//            //txtLote.Text = orden.Lote;
//            txtFechaProduccion.Text = orden.FechaProduccionInicio.ToString("dd/MM/yyyy");
//            //txtFechaCaducidad.Text = orden.FechaCaducidad.ToString("dd/MM/yyyy");
//        }

//        private void LimpiarDatosOrden()
//        {
//            //txtNumeroArticulo.Text = string.Empty;
//            txtDescripcion.Text = string.Empty;
//            txtCantidadPallet.Text = string.Empty;
//            //txtProgramaProduccion.Text = string.Empty;
//            //txtTurno.Text = string.Empty;
//            //txtLote.Text = string.Empty;
//            txtFechaProduccion.Text = string.Empty;
//            //txtFechaCaducidad.Text = string.Empty;

//            btnImprimirEtiqueta.IsEnabled = false;
//            txtCodigoVerificacion.IsEnabled = false;
//            btnVerificar.IsEnabled = false;

//            _currentOrden = null;
//        }

//        private void BtnImprimirEtiqueta_Click(object sender, RoutedEventArgs e)
//        {
//            if (_currentOrden == null)
//            {
//                MessageBox.Show("No hay una orden seleccionada para imprimir.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }

//            try
//            {
//                var (turnoActual, fechaProduccion) = _turnoService.ObtenerTurnoYFechaProduccion();
//                string numeroTransaccion = _turnoService.ObtenerNumeroTransaccion(fechaProduccion);
//                // Generar código de barras según especificación
//                _expectedBarcode = GenerarCodigoBarras(_currentOrden, turnoActual, fechaProduccion);
//                var etiquetaGenerada = new EtiquetaGenerada
//                {
//                    EDUS = _currentUser.NombreUsuario, // O cualquier identificador del sistema de etiquetado
//                    EDDT = PasarFechaJuliana(fechaProduccion.ToString("ddMMyy")),//fecha juliana 
//                    EDTN = fechaProduccion.ToString("MMdd"), // Por ejemplo: 1125 para 25 de noviembre
//                    EDLN = _etiquetadoService.ObtenerSiguienteNumeroSecuencialdeldia(PasarFechaJuliana(fechaProduccion.ToString("ddMMyy"))),
//                    DOCO = _currentOrden.ProgramaProduccion,
//                    LITM = _currentOrden.NumeroArticulo,
//                    SOQS = _currentOrden.CantidadPorPallet,
//                    UOM1 = _currentOrden.UnidadMedida, // O la unidad de medida que corresponda
//                    LOTN = _currentOrden.ProgramaProduccion + turnoActual,
//                    EXPR = fechaProduccion.AddDays(_currentOrden.DiasCaducidad),
//                    TDAY = fechaProduccion.ToString("HHmmss"),
//                    SHFT = turnoActual,
//                    URDT = _currentOrden.FechaProduccionInicio.AddDays(_currentOrden.DiasCaducidad),//no se sabe este
//                    SEC = _etiquetadoService.ObtenerSiguienteNumeroSecuencial(_currentOrden.ProgramaProduccion),
//                    ESTADO = "1", // O cualquier estado que necesites
//                    URRF = _currentOrden.DUN14, // O cualquier referencia que necesites
//                    FechaCreacion = DateTime.Now,
//                    Confirmada = false
//                };
//                etiactual = etiquetaGenerada;
//                // Imprimir etiqueta (aquí implementarías el código real de impresión)
//                _expectedBarcode = GenerarCodigoBarras(_currentOrden, turnoActual, fechaProduccion);
//                bool impresionExitosa = ImprimirEtiqueta(etiquetaGenerada, _expectedBarcode);

//                if (impresionExitosa)
//                {
//                    AddActivityLog("Etiqueta impresa. Por favor escanee el código para verificar.", ActivityLogItem.LogLevel.Info);

//                    // Habilitar controles de verificación
//                    txtCodigoVerificacion.IsEnabled = true;
//                    btnVerificar.IsEnabled = true;
//                    txtCodigoVerificacion.Focus();
//                }
//                else
//                {
//                    AddActivityLog( "Error al imprimir la etiqueta. Intente nuevamente.", ActivityLogItem.LogLevel.Error);
//                }
//            }
//            catch (Exception ex)
//            {
//                AddActivityLog( $"Error al imprimir la etiqueta: {ex.Message}", ActivityLogItem.LogLevel.Error);
//                MessageBox.Show($"Error al imprimir la etiqueta: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//        }
//        public static string PasarFechaJuliana(string fecha)
//        {
//            // La fecha llega como DDMMYY y la pasamos a juliana
//            // 114154
//            // Primer carácter: CENTURIA (1 es desde 2000)
//            // Carácter dos y tres: AÑO (14)
//            // TRES ULTIMOS CARACTERES: nro del día del año: 154 ES 03 de junio
//            try
//            {
//                // Validar que la fecha tenga el formato correcto
//                if (string.IsNullOrEmpty(fecha) || fecha.Length != 6)
//                    throw new ArgumentException("La fecha debe tener formato DDMMYY");

//                // Extraer día, mes y año
//                int dia = int.Parse(fecha.Substring(0, 2));
//                int mes = int.Parse(fecha.Substring(2, 2));
//                int año = 2000 + int.Parse(fecha.Substring(4, 2));

//                // Crear fecha
//                DateTime fechaAUsar = new DateTime(año, mes, dia);

//                // Calcular día juliano
//                int diaDelAño = fechaAUsar.DayOfYear;
//                string sDia = diaDelAño.ToString("000");
//                string sAño = fechaAUsar.ToString("yy");

//                // Retornar fecha juliana
//                return "1" + sAño + sDia;
//            }
//            catch (Exception ex)
//            {

//                throw new Exception($"Error al convertir fecha a formato juliano: {ex.Message}");
//            }
//        }
//        private string GenerarCodigoBarras(OrdenProduccion orden, string turnoActual, DateTime fechaProduccion)
//        {
//            // Según la especificación del documento
//            string numeroArticulo = orden.NumeroArticulo.PadRight(8, '0'); // 8 caracteres
//            string fechaVencimiento = fechaProduccion.AddDays(orden.DiasCaducidad).ToString("ddMMyy");  // 6 caracteres
//            string cantidadPallet = orden.CantidadPorPallet.ToString("D4"); // 4 caracteres
//            string fechaDeclaracion = fechaProduccion.ToString("ddMMyy"); // 6 caracteres
//            string horaDeclaracion = fechaProduccion.ToString("HHmmss"); // 6 caracteres
//            string lote = (orden.ProgramaProduccion+turnoActual).PadRight(7, '0'); // 7 caracteres

//            // Concatenar según la estructura definida en la especificación
//            string codigoBarras = $"{numeroArticulo}{fechaVencimiento}{cantidadPallet}{fechaDeclaracion}{horaDeclaracion}{lote}";

//            return codigoBarras;
//        }

//        private bool ImprimirEtiqueta(EtiquetaGenerada etiqueta, string codigoBarras)
//        {
//            // Aquí implementarías el código real para enviar a la impresora
//            // Por ahora, solo simulamos una impresión exitosa

//            MessageBox.Show($"Simulando impresión de etiqueta:\n\n" +
//                           $"Artículo: {etiqueta.LITM} - \n" +
//                           $"Cantidad: {etiqueta.SOQS}\n" +
//                           $"Lote: {etiqueta.LOTN}\n" +
//                           $"Código de barras: {codigoBarras}",
//                           "Impresión Simulada", MessageBoxButton.OK, MessageBoxImage.Information);

//            return true;
//        }

//        private void TxtCodigoVerificacion_KeyDown(object sender, KeyEventArgs e)
//        {
//            if (e.Key == Key.Enter)
//            {
//                VerificarCodigoBarras();
//            }
//        }

//        private void BtnVerificar_Click(object sender, RoutedEventArgs e)
//        {
//            VerificarCodigoBarras();
//        }

//        private void VerificarCodigoBarras()
//        {
//            string codigoEscaneado = txtCodigoVerificacion.Text.Trim();

//            if (string.IsNullOrEmpty(codigoEscaneado))
//            {
//                MessageBox.Show("Por favor, escanee el código de barras de la etiqueta.", "Advertencia", MessageBoxButton.OK, MessageBoxImage.Warning);
//                return;
//            }

//            if (codigoEscaneado == _expectedBarcode)
//            {
//                try
//                {

//                    // string numeroTransaccion = _turnoService.ObtenerNumeroTransaccion(fechaProduccion);

//                    // Guardar en la tabla de salida
//                    var etiquetaGenerada = etiactual;
//                    etiquetaGenerada.Confirmada = true;
//                    _etiquetadoService.GuardarEtiquetaConStoredProcedure(etiquetaGenerada);
//                    MessageBox.Show("¡Etiqueta verificada y registrada con éxito!", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

//                    // Limpiar y preparar para una nueva etiqueta
//                    txtDun14.Text = string.Empty;
//                    txtCodigoVerificacion.Text = string.Empty;
//                    LimpiarDatosOrden();
//                    AddActivityLog( "Etiqueta registrada. Listo para un nuevo proceso.", ActivityLogItem.LogLevel.Info);

//                    // Poner foco en el campo DUN14
//                    txtDun14.Focus();
//                }
//                catch (Exception ex)
//                {
//                    AddActivityLog( $"Error al registrar la etiqueta: {ex.Message}", ActivityLogItem.LogLevel.Error);
//                    MessageBox.Show($"Error al registrar la etiqueta: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
//                }
//            }
//            else
//            {
//                AddActivityLog( "¡Error! El código escaneado no coincide con la etiqueta generada.", ActivityLogItem.LogLevel.Error);
//                MessageBox.Show("El código escaneado no coincide con la etiqueta generada. Por favor, verifique e intente nuevamente.", "Error de verificación", MessageBoxButton.OK, MessageBoxImage.Error);
//                txtCodigoVerificacion.Text = string.Empty;
//                txtCodigoVerificacion.Focus();
//            }
//        }
//    }
//}