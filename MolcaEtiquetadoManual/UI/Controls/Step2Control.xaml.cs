using System;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Configuration;
using MolcaEtiquetadoManual.Core.Interfaces;
using MolcaEtiquetadoManual.Core.Models;
using MolcaEtiquetadoManual.Core.Services;

namespace MolcaEtiquetadoManual.UI.Controls
{
    /// <summary>
    /// Control para el segundo paso: Visualización de datos y generación de etiqueta
    /// </summary>
    public partial class Step2Control : UserControl
    {
        private readonly IPrintService _printService;
        private readonly IEtiquetadoService _etiquetadoService;
        private readonly ITurnoService _turnoService;
        private readonly IBarcodeService _barcodeService;
        private readonly ILogService _logService;
        private readonly IJulianDateService _julianDateService;
        private readonly IEtiquetaPreviewService _etiquetaPreviewService;
        private readonly Usuario _currentUser;
        private readonly IConfiguration _configuration;
        private Button btnCancelarImpresion;
        public OrdenProduccion _currentOrden;
        private EtiquetaGenerada _etiquetaActual;
        private string _generatedBarcode;
        private bool impresioncanceladaxusuario=false;

        // Eventos
        public event EventHandler<EtiquetaGeneradaEventArgs> EtiquetaImpresa;
        public event EventHandler CancelarSolicitado;
        public event Action<string, ActivityLogItem.LogLevel> ActivityLog;

        public OrdenProduccion CurrentOrden => _currentOrden;
        public Step2Control(
            IPrintService printService,
            IEtiquetadoService etiquetadoService,
            ITurnoService turnoService,
            IBarcodeService barcodeService,
            ILogService logService,
            IJulianDateService julianDateService,
            IEtiquetaPreviewService etiquetaPreviewService,
            Usuario currentUser,
            IConfiguration configuration
            )
        {
            InitializeComponent();

            _printService = printService ?? throw new ArgumentNullException(nameof(printService));
            _etiquetadoService = etiquetadoService ?? throw new ArgumentNullException(nameof(etiquetadoService));
            _turnoService = turnoService ?? throw new ArgumentNullException(nameof(turnoService));
            _barcodeService = barcodeService ?? throw new ArgumentNullException(nameof(barcodeService));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _julianDateService = julianDateService ?? throw new ArgumentNullException(nameof(julianDateService));
            _etiquetaPreviewService = etiquetaPreviewService ?? throw new ArgumentNullException(nameof(etiquetaPreviewService));
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _configuration= configuration ?? throw new ArgumentNullException(nameof(configuration));    

            // Inicializar a vacío
            Limpiar();
        }

        /// <summary>
        /// Establece la orden de producción actual y actualiza la UI
        /// </summary>
        public void SetOrden(OrdenProduccion orden)
        {
            _currentOrden = orden ?? throw new ArgumentNullException(nameof(orden));

            // Actualizar datos en la interfaz
            ActualizarDatos();

            // Habilitar el botón de impresión
            btnImprimirEtiqueta.IsEnabled = true;

            // Ocultar el error si estaba visible
            txtError.Text = string.Empty;
        }

        private void ActualizarDatos()
        {
            if (_currentOrden == null)
                return;

            // Obtener información del turno actual
            var (turnoActual, fechaProduccion) = _turnoService.ObtenerTurnoYFechaProduccion();

            // Actualizar controles de la interfaz
            txtNumeroArticulo.Text = _currentOrden.NumeroArticulo;
            txtDescripcion.Text = _currentOrden.Descripcion;
            txtCantidadPallet.Text = _currentOrden.CantidadPorPallet.ToString();
            txtProgramaProduccion.Text = _currentOrden.ProgramaProduccion;
            txtTurno.Text = turnoActual;
            txtLote.Text = $"{_currentOrden.ProgramaProduccion}{turnoActual}";
            txtFechaProduccion.Text = fechaProduccion.ToString("dd/MM/yyyy");
            txtFechaCaducidad.Text = fechaProduccion.AddDays(_currentOrden.DiasCaducidad).ToString("dd/MM/yyyy");
            txtDUN14.Text = _currentOrden.DUN14;

            // Generar vista previa preliminar
            try
            {
                GenerarVistaPreliminar(turnoActual, fechaProduccion);
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al generar vista previa de etiqueta");
                // No mostramos error al usuario ya que no es crítico
            }
        }

        private void GenerarVistaPreliminar(string turnoActual, DateTime fechaProduccion)
        {
            if (_currentOrden == null)
                return;

            // Crear etiqueta temporal para vista previa
            var etiquetaTemporal = new EtiquetaGenerada
            {
                LOTN = $"{_currentOrden.ProgramaProduccion}{turnoActual}",
                EXPR = fechaProduccion.AddDays(_currentOrden.DiasCaducidad),
                LITM = _currentOrden.NumeroArticulo,
                SOQS = _currentOrden.CantidadPorPallet,
                TDAY = DateTime.Now.ToString("HHmmss"),
                SEC = 1, // Valor simulado para previsualización
                SHFT = turnoActual
            };

            string codigoBarrasPreliminar = null;

            try
            {
                // Intentar generar el código de barras para la vista previa
                if (_barcodeService != null)
                {
                    codigoBarrasPreliminar = _barcodeService.GenerarCodigoBarras(_currentOrden, etiquetaTemporal);
                    _logService.Debug($"Código de barras para vista previa generado: {codigoBarrasPreliminar}");
                }
                else
                {
                    // Simular un código de barras para la vista previa
                    codigoBarrasPreliminar = $"{_currentOrden.NumeroArticulo}{DateTime.Now:yyMMdd}{etiquetaTemporal.LOTN}";
                    _logService.Debug("Generado código de barras simulado para vista previa");
                }
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al generar código de barras para vista previa");
                codigoBarrasPreliminar = "ERROR_CODIGO_BARRAS";
            }

            try
            {
                // Generar imagen de código de barras si tenemos servicio
                BitmapSource imagenCodigo = null;
                if (_barcodeService != null)
                {
                    imagenCodigo = _barcodeService.GenerarImagenCodigoBarras(
                        codigoBarrasPreliminar,
                        ZXing.BarcodeFormat.CODE_128,
                        280,  // Ancho
                        70    // Alto
                    );
                }

                // Crear objeto de vista previa
                var vistaPrevia = new EtiquetaPreview
                {
                    NumeroArticulo = _currentOrden.NumeroArticulo,
                    Descripcion = _currentOrden.Descripcion,
                    Lote = etiquetaTemporal.LOTN,
                    FechaProduccion = fechaProduccion,
                    FechaVencimiento = etiquetaTemporal.EXPR,
                    CantidadPorPallet = _currentOrden.CantidadPorPallet,
                    DUN14 = _currentOrden.DUN14,
                    CodigoBarras = codigoBarrasPreliminar,
                    ImagenCodigoBarras = imagenCodigo
                };

                // Actualizar control de previsualización
                etiquetaPreview.ActualizarVista(vistaPrevia);

                _logService.Information("Vista previa de etiqueta generada con éxito");
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al actualizar vista previa de etiqueta");
                // Mostrar que hubo un error en la vista previa
                etiquetaPreview.Limpiar();
            }
        }

        private void BtnImprimirEtiqueta_Click(object sender, RoutedEventArgs e)
        {
            var printerSettings = _configuration.GetSection("PrinterSettings");
            if (bool.Parse(printerSettings["UseMockPrinter"]) == true)
            {
                ImprimirEtiqueta1();
            }
            else
            {
                ImprimirEtiqueta();
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            // Notificar que se solicitó cancelar
            ActivityLog?.Invoke("Operación cancelada por el usuario", ActivityLogItem.LogLevel.Info);
            CancelarSolicitado?.Invoke(this, EventArgs.Empty);
        }

        private void ImprimirEtiqueta()
        {
            if (_currentOrden == null)
            {
                MostrarError("No hay una orden seleccionada para imprimir");
                return;
            }

            // Mostrar indicador de progreso
            progressBar.Visibility = Visibility.Visible;
            progressBar.IsIndeterminate = false;
            progressBar.Value = 0;
            btnImprimirEtiqueta.IsEnabled = false;
            txtError.Text = string.Empty;

            // Ocultar el botón de cancelar que vuelve al paso anterior
            btnCancelar.Visibility = Visibility.Collapsed;

            // Mostrar overlay para bloquear la interacción con los controles de fondo
            overlayPanel.Visibility = Visibility.Visible;

            // Crear botón de cancelación si no existe
            if (btnCancelarImpresion == null)
            {
                btnCancelarImpresion = new Button();
                btnCancelarImpresion.Content = "CANCELAR IMPRESIÓN";
                btnCancelarImpresion.Style = (Style)FindResource("MaterialDesignRaisedAccentButton");
                btnCancelarImpresion.Background = new SolidColorBrush(Colors.Orange);
                btnCancelarImpresion.Foreground = new SolidColorBrush(Colors.White);
                btnCancelarImpresion.Margin = new Thickness(10);
                btnCancelarImpresion.Padding = new Thickness(15, 8, 15, 8);
                btnCancelarImpresion.Click += BtnCancelarImpresion_Click;
                overlayButtonPanel.Children.Add(btnCancelarImpresion);
            }
            btnCancelarImpresion.Visibility = Visibility.Visible;
          

            try
            {
                // Obtener información del turno y fecha
                var (turnoActual, fechaProduccion) = _turnoService.ObtenerTurnoYFechaProduccion();
                string numeroTransaccion = _turnoService.ObtenerNumeroTransaccion(fechaProduccion);

                // Crear la entidad de etiqueta
                _etiquetaActual = CrearEtiquetaGenerada(_currentOrden, turnoActual, fechaProduccion, numeroTransaccion);

                // Intentar generar el código de barras
                try
                {
                    if (_barcodeService != null)
                    {
                        _generatedBarcode = _barcodeService.GenerarCodigoBarras(_currentOrden, _etiquetaActual);
                        _logService.Debug($"Código de barras generado: {_generatedBarcode}");
                    }
                    else
                    {
                        // Generamos un código ficticio para pruebas
                        _generatedBarcode = $"{_currentOrden.NumeroArticulo}-{DateTime.Now:yyMMddHHmmss}-{_etiquetaActual.LOTN}";
                        _logService.Warning("Usando código de barras ficticio para pruebas: {0}", _generatedBarcode);
                    }
                }
                catch (Exception ex)
                {
                    _logService.Error(ex, "Error al generar código de barras, usando ficticio");
                    _generatedBarcode = $"{_currentOrden.NumeroArticulo}-{DateTime.Now:yyMMddHHmmss}-{_etiquetaActual.LOTN}";
                }

                // Registrar actividad
                ActivityLog?.Invoke("Enviando etiqueta a la impresora...", ActivityLogItem.LogLevel.Info);

                // Guardar la etiqueta en la base de datos ANTES de imprimir (con Confirmada = false)
                try
                {
                    _etiquetaActual.Confirmada = false;
                    string resultado = _etiquetadoService.GuardarEtiqueta(_etiquetaActual);
                    _logService.Information("Etiqueta guardada en BD con SEC={0}", resultado);
                }
                catch (Exception ex)
                {
                    _logService.Error(ex, "Error al guardar etiqueta en BD, continuando con impresión");
                }

                // Suscribirse a eventos de progreso y finalización
                if (_printService is ZebraPrintService zebraPrintService)
                {
                    zebraPrintService.PrintProgress += PrintService_PrintProgress;
                    zebraPrintService.PrintCompleted += PrintService_PrintCompleted;
                }

                // Iniciar la impresión
                bool impresionIniciada = _printService.ImprimirEtiqueta(_currentOrden, _etiquetaActual, _generatedBarcode);

                if (!impresionIniciada)
                {
                    MostrarError("No se pudo iniciar el proceso de impresión");
                    ResetearInterfazImpresion();
                }
            }
            catch (Exception ex)
            {
                MostrarError($"Error al preparar la impresión: {ex.Message}");
                _logService.Error(ex, "Error al preparar impresión: {Error}", ex.Message);
                ActivityLog?.Invoke($"Error al preparar impresión: {ex.Message}", ActivityLogItem.LogLevel.Error);
                ResetearInterfazImpresion();
            }
        }
        public void ResetearEstadoImpresion()
        {
            progressBar.Visibility = Visibility.Collapsed;
            btnImprimirEtiqueta.IsEnabled = true; // Habilitar el botón IMPRIMIR
            btnCancelar.Visibility = Visibility.Visible; // Mostrar el botón CANCELAR
            overlayPanel.Visibility = Visibility.Collapsed; // Ocultar el overlay

            // Ocultar el botón CANCELAR IMPRESIÓN
            if (btnCancelarImpresion != null)
            {
                btnCancelarImpresion.Visibility = Visibility.Collapsed;
            }

            // Resetear variables de impresión si es necesario
            impresioncanceladaxusuario = false;
        }
        private void ImprimirEtiqueta1()
        {
            if (_currentOrden == null)
            {
                MostrarError("No hay una orden seleccionada para imprimir");
                return;
            }

            // Mostrar indicador de progreso
            progressBar.Visibility = Visibility.Visible;
            btnImprimirEtiqueta.IsEnabled = false;
            txtError.Text = string.Empty;

            try
            {
                // Obtener información del turno y fecha
                var (turnoActual, fechaProduccion) = _turnoService.ObtenerTurnoYFechaProduccion();
                string numeroTransaccion = _turnoService.ObtenerNumeroTransaccion(fechaProduccion);

                // Crear la entidad de etiqueta
                _etiquetaActual = CrearEtiquetaGenerada(_currentOrden, turnoActual, fechaProduccion, numeroTransaccion);

                // Intentar generar el código de barras
                try
                {
                    if (_barcodeService != null)
                    {
                        _generatedBarcode = _barcodeService.GenerarCodigoBarras(_currentOrden, _etiquetaActual);
                        _logService.Debug($"Código de barras generado: {_generatedBarcode}");
                    }
                    else
                    {
                        // Generamos un código ficticio para pruebas
                        _generatedBarcode = $"{_currentOrden.NumeroArticulo}-{DateTime.Now:yyMMddHHmmss}-{_etiquetaActual.LOTN}";
                        _logService.Warning("Usando código de barras ficticio para pruebas: {0}", _generatedBarcode);
                    }
                }
                catch (Exception ex)
                {
                    _logService.Error(ex, "Error al generar código de barras, usando ficticio");
                    // Generamos un código ficticio para pruebas en caso de error
                    _generatedBarcode = $"{_currentOrden.NumeroArticulo}-{DateTime.Now:yyMMddHHmmss}-{_etiquetaActual.LOTN}";
                }

                // Registrar actividad
                ActivityLog?.Invoke("Enviando etiqueta a la impresora...", ActivityLogItem.LogLevel.Info);

                // Guardar la etiqueta en la base de datos ANTES de imprimir (con Confirmada = false)
                try
                {
                    _etiquetaActual.Confirmada = false;
                    // Intentamos guardar en la BD, pero si falla seguimos con la impresión
                    string resultado = _etiquetadoService.GuardarEtiqueta(_etiquetaActual);
                    _logService.Information("Etiqueta guardada en BD con SEC={0}", resultado);
                }
                catch (Exception ex)
                {
                    _logService.Error(ex, "Error al guardar etiqueta en BD, continuando con impresión");
                }

                // Simular impresión para pruebas
                bool impresionExitosa = false;
                try
                {
                    // Intentar imprimir a través del servicio
                    if (_printService != null)
                    {
                        impresionExitosa = _printService.ImprimirEtiqueta(_currentOrden, _etiquetaActual, _generatedBarcode);
                    }

                    // Si no hay servicio o falla, simulamos éxito para pruebas
                    if (!impresionExitosa)
                    {
                        _logService.Warning("Simulando impresión exitosa para pruebas");
                        impresionExitosa = true;
                    }
                }
                catch (Exception ex)
                {
                    _logService.Error(ex, "Error al imprimir, simulando éxito para pruebas");
                    impresionExitosa = true; // Simulamos éxito para poder seguir con las pruebas
                }

                if (impresionExitosa)
                {
                    ActivityLog?.Invoke("Etiqueta enviada a la impresora. Proceda a verificar.", ActivityLogItem.LogLevel.Info);

                    // Notificar que se imprimió la etiqueta
                    EtiquetaImpresa?.Invoke(this, new EtiquetaGeneradaEventArgs(_etiquetaActual, _generatedBarcode));
                }
                else
                {
                    MostrarError("Error al enviar la etiqueta a la impresora");
                    ActivityLog?.Invoke("Error al enviar la etiqueta a la impresora", ActivityLogItem.LogLevel.Error);
                }
            }
            catch (Exception ex)
            {
                MostrarError($"Error al imprimir la etiqueta: {ex.Message}");
                _logService.Error(ex, "Error al imprimir etiqueta: {Error}", ex.Message);
                ActivityLog?.Invoke($"Error al imprimir la etiqueta: {ex.Message}", ActivityLogItem.LogLevel.Error);
            }
            finally
            {
                // Ocultar indicador de progreso
                progressBar.Visibility = Visibility.Collapsed;
                btnImprimirEtiqueta.IsEnabled = true;
            }
        }
        // Manejadores de eventos para la impresión en segundo plano
        private void PrintService_PrintProgress(object sender, ZebraPrintService.PrintProgressEventArgs e)
        {
            // Asegurarse de que se ejecuta en el hilo de UI
            Dispatcher.Invoke(() =>
            {
                progressBar.Value = e.Percentage;
                txtError.Text = e.Message;
                //txtError.Foreground = Brushes.Black; // Color normal para mensajes de progreso

                // Actualizar también el texto en el overlay
                overlayStatusText.Text = e.Message;

                // Informar a la interfaz de actividad
                ActivityLog?.Invoke(e.Message, ActivityLogItem.LogLevel.Info);
            });
        }
        private void PrintService_PrintCompleted(object sender, ZebraPrintService.PrintCompletedEventArgs e)
        {
            // Desuscribirse de los eventos
            if (sender is ZebraPrintService zebraPrintService)
            {
                zebraPrintService.PrintProgress -= PrintService_PrintProgress;
                zebraPrintService.PrintCompleted -= PrintService_PrintCompleted;
            }

            // Asegurarse de que se ejecuta en el hilo de UI
            Dispatcher.Invoke(() =>
            {
                if (e.Success)
                {
                    // Exitoso
                    txtError.Text = e.Message;
                    //txtError.Foreground = Brushes.Green;
                    ActivityLog?.Invoke("Etiqueta enviada a la impresora. Proceda a verificar.", ActivityLogItem.LogLevel.Info);

                    // Notificar que se imprimió la etiqueta
                    EtiquetaImpresa?.Invoke(this, new EtiquetaGeneradaEventArgs(_etiquetaActual, _generatedBarcode));
                }
                else
                {
                    // Error
                    MostrarError(e.Message);
                    ActivityLog?.Invoke(e.Message, ActivityLogItem.LogLevel.Error);
                    ResetearInterfazImpresion();
                    if (impresioncanceladaxusuario)
                    {
                        _etiquetaActual.MotivoNoConfirmacion = "La impresion fue cancelada por el usuario";
                        string resultado = _etiquetadoService.GuardarEtiqueta(_etiquetaActual);
                    }
                    else
                    {
                        _etiquetaActual.MotivoNoConfirmacion = "La impresion dio error: " + e.Message;
                        string resultado = _etiquetadoService.GuardarEtiqueta(_etiquetaActual);
                    }
                    
                }
            });
        }

        private void BtnCancelarImpresion_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_printService is ZebraPrintService zebraPrintService)
                {
                    impresioncanceladaxusuario = true;
                    zebraPrintService.CancelarImpresion();
                    ActivityLog?.Invoke("Cancelando impresión...", ActivityLogItem.LogLevel.Warning);
                    
                }

            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al cancelar impresión");
            }
        }

        private void ResetearInterfazImpresion()
        {
            progressBar.Visibility = Visibility.Collapsed;
            btnImprimirEtiqueta.IsEnabled = true;
            btnCancelar.Visibility = Visibility.Visible;
            overlayPanel.Visibility = Visibility.Collapsed;

            if (btnCancelarImpresion != null)
            {
                btnCancelarImpresion.Visibility = Visibility.Collapsed;
            }
        }
        private EtiquetaGenerada CrearEtiquetaGenerada(OrdenProduccion orden, string turnoActual,
            DateTime fechaProduccion, string numeroTransaccion)
        {
            // Obtener siguientes números secuenciales con manejo de errores
            int numeroSecuencial = 1;
            int numeroPallet = 1;
            string fechaJuliana = "";
            int lineaId = 1;
            try
            {

                var lineNumber = _configuration.GetValue<string>("AppSettings:LineNumber") ?? "1";
                lineaId = int.Parse(lineNumber);
            }
            catch
            {
                _logService.Warning("No se pudo obtener el número de línea desde la configuración, usando valor por defecto: 1");
            }

            try
            {
                // Intentar convertir la fecha a formato juliano si tenemos el servicio
                if (_julianDateService != null)
                {
                    fechaJuliana = _julianDateService.ConvertirAFechaJuliana(fechaProduccion);
                }
                else
                {
                    // Formato juliano simulado para pruebas: 1AADDD (1 = siglo, AA = año, DDD = día del año)
                    fechaJuliana = $"1{fechaProduccion.ToString("yyDDD")}";
                }

                // Obtener secuencial del día con manejo de errores
                try
                {
                    numeroSecuencial = _etiquetadoService.ObtenerSiguienteNumeroSecuencialdeldia(fechaJuliana, lineaId);
                }
                catch (Exception ex)
                {
                    _logService.Error(ex, "Error al obtener secuencial del día, usando valor por defecto");
                    numeroSecuencial = 1; // Valor por defecto para pruebas
                }

                // Obtener número de pallet secuencial con manejo de errores
                try
                {
                    numeroPallet = _etiquetadoService.ObtenerSiguienteNumeroSecuencial(orden.ProgramaProduccion, lineaId);
                }
                catch (Exception ex)
                {
                    _logService.Error(ex, "Error al obtener secuencial de pallet, usando valor por defecto");
                    numeroPallet = 1; // Valor por defecto para pruebas
                }

            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error general al obtener secuenciales, usando valores por defecto");
            }

            // Crear la entidad con manejo seguro de posibles errores
            string usuario = _currentUser.NombreUsuario?.Length > 4 ?
                _currentUser.NombreUsuario.Substring(0, 4) : (_currentUser.NombreUsuario ?? "ETIQ").PadRight(4);

            string horaJuliana = "";
            try
            {
                if (_julianDateService != null)
                {
                    horaJuliana = _julianDateService.ConvertirAHoraJuliana(fechaProduccion);
                }
                else
                {
                    horaJuliana = fechaProduccion.ToString("HHmmss");
                }
            }
            catch
            {
                horaJuliana = fechaProduccion.ToString("HHmmss");
            }

            DateTime fechaVencimiento = fechaProduccion.AddDays(orden.DiasCaducidad);
            string fechaVencimientoJuliana = string.Empty;

            // Convertir a formato juliano
            try
            {
                if (_julianDateService != null)
                {
                    fechaVencimientoJuliana = _julianDateService.ConvertirAFechaJuliana(fechaVencimiento);
                }
                else
                {
                    // Formato simulado si no hay servicio: 1AADDD
                    fechaVencimientoJuliana = $"1{fechaVencimiento.ToString("yyDDD")}";
                }
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al convertir fecha de vencimiento a juliano, usando formato básico");
                fechaVencimientoJuliana = $"1{fechaVencimiento.ToString("yyDDD")}";
            }

            // Asignar a la etiqueta (antes guardábamos la fecha DateTime)
            //_etiquetaActual.URDT = fechaVencimientoJuliana;

            return new EtiquetaGenerada
            {
                EDUS = usuario,
                EDDT = fechaJuliana,
                EDTN = numeroTransaccion ?? fechaProduccion.ToString("MMdd"),
                EDLN = numeroSecuencial,
                DOCO = orden.ProgramaProduccion,
                LITM = orden.NumeroArticulo,
                SOQS = orden.CantidadPorPallet,
                UOM1 = orden.UnidadMedida ?? "UN",
                LOTN = $"{orden.ProgramaProduccion}{turnoActual}",
                EXPR = fechaProduccion.AddDays(orden.DiasCaducidad),
                TDAY = horaJuliana,
                SHFT = turnoActual,
                URDT = fechaVencimientoJuliana,
                SEC = numeroPallet,
                ESTADO = "1", // 1 = Activa/Pendiente
                URRF = orden.DUN14,
                FechaCreacion = DateTime.Now,
                Confirmada = false,
                LineaId= Convert.ToInt32(_configuration.GetValue<string>("AppSettings:LineNumber") ?? "0")
            };
        }

        private void MostrarError(string mensaje)
        {
            txtError.Text = mensaje;
            progressBar.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Limpia todos los campos y reinicia el control
        /// </summary>
        public void Limpiar()
        {
            txtNumeroArticulo.Text = string.Empty;
            txtDescripcion.Text = string.Empty;
            txtCantidadPallet.Text = string.Empty;
            txtProgramaProduccion.Text = string.Empty;
            txtTurno.Text = string.Empty;
            txtLote.Text = string.Empty;
            txtFechaProduccion.Text = string.Empty;
            txtFechaCaducidad.Text = string.Empty;
            txtDUN14.Text = string.Empty;
            txtError.Text = string.Empty;

            btnImprimirEtiqueta.IsEnabled = false;
            progressBar.Visibility = Visibility.Collapsed;

            // Limpiar vista previa
            if (etiquetaPreview != null)
            {
                etiquetaPreview.Limpiar();
            }

            // Limpiar variables internas
            _currentOrden = null;
            _etiquetaActual = null;
            _generatedBarcode = null;
        }
    }

    /// <summary>
    /// Clase para los datos del evento de etiqueta impresa
    /// </summary>
    public class EtiquetaGeneradaEventArgs : EventArgs
    {
        public EtiquetaGenerada Etiqueta { get; }
        public string CodigoBarras { get; }

        public EtiquetaGeneradaEventArgs(EtiquetaGenerada etiqueta, string codigoBarras)
        {
            Etiqueta = etiqueta;
            CodigoBarras = codigoBarras;
        }
    }
}