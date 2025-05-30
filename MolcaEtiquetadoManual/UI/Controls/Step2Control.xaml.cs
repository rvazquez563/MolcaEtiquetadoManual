﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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
        #region Variables Privadas
        private readonly IPrintService _printService;
        private readonly IEtiquetadoService _etiquetadoService;
        private readonly ITurnoService _turnoService;
        private readonly IBarcodeService _barcodeService;
        private readonly ILogService _logService;
        private readonly IJulianDateService _julianDateService;
        private readonly IEtiquetaPreviewService _etiquetaPreviewService;
        private readonly Usuario _currentUser;
        private readonly IConfiguration _configuration;
        private readonly ILineaProduccionService _lineaService;
        private Button btnCancelarImpresion;
        private OrdenProduccion _currentOrden;
        private EtiquetaGenerada _etiquetaActual;
        private LineaProduccion linea;
        private string _generatedBarcode;
        private bool impresioncanceladaxusuario = false;
        private int _originalCantidadPorPallet;
        private int _cantidadModificada;
        private string _tipoProductoSeleccionado = "0"; // Valor predeterminado: Producto OK
        #endregion

        #region Eventos
        public event EventHandler<EtiquetaGeneradaEventArgs> EtiquetaImpresa;
        public event EventHandler CancelarSolicitado;
        public event Action<string, ActivityLogItem.LogLevel> ActivityLog;
        #endregion

        #region Constructor
        public Step2Control(
            IPrintService printService,
            IEtiquetadoService etiquetadoService,
            ITurnoService turnoService,
            IBarcodeService barcodeService,
            ILogService logService,
            IJulianDateService julianDateService,
            IEtiquetaPreviewService etiquetaPreviewService,
            Usuario currentUser,
            IConfiguration configuration,
             ILineaProduccionService lineaService

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
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _lineaService = lineaService;

            // Inicializar a vacío
            Limpiar();

            // Seleccionar el tipo de producto predeterminado
            if (cmbTipoProducto.Items.Count > 0)
            {
                cmbTipoProducto.SelectedIndex = 0;
            }
        }
        #endregion

        #region Métodos Públicos
        /// <summary>
        /// Establece la orden de producción actual y actualiza la UI
        /// </summary>
        public void SetOrden(OrdenProduccion orden)
        {
            _currentOrden = orden ?? throw new ArgumentNullException(nameof(orden));
            _originalCantidadPorPallet = orden.CantidadPorPallet;
            _cantidadModificada = orden.CantidadPorPallet;

            // Actualizar datos en la interfaz
            ActualizarDatos();

            // Habilitar el botón de impresión
            btnImprimirEtiqueta.IsEnabled = true;

            // Ocultar el error si estaba visible
            txtError.Text = string.Empty;
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
            txtFechaElaboracion.Text = string.Empty;
            txtLote.Text = string.Empty;
            txtFechaProduccion.Text = string.Empty;
            txtFechaCaducidad.Text = string.Empty;
            txtDUN14.Text = string.Empty;
            txtError.Text = string.Empty;

            // Resetear tipo de producto al valor predeterminado
            cmbTipoProducto.SelectedIndex = 0;
            _tipoProductoSeleccionado = "0";

            btnImprimirEtiqueta.IsEnabled = false;
            progressBar.Visibility = Visibility.Collapsed;

            // Asegurar que el botón de cancelar esté visible para el próximo uso
            btnCancelar.Visibility = Visibility.Visible;

            // Ocultar panel de overlay si estaba visible
            overlayPanel.Visibility = Visibility.Collapsed;

            // Limpiar vista previa
            if (etiquetaPreview != null)
            {
                etiquetaPreview.Limpiar();
            }

            // Limpiar variables internas
            _currentOrden = null;
            _etiquetaActual = null;
            _generatedBarcode = null;
            _originalCantidadPorPallet = 0;
            _cantidadModificada = 0;
            impresioncanceladaxusuario = false;
        }
        #endregion

        #region Métodos Privados

        private void ActualizarIndicadoresCantidad()
        {
            if (_currentOrden != null && txtMaxCantidad != null)
            {
                // Actualizar el indicador de cantidad máxima
                txtMaxCantidad.Text = $"Max: {_originalCantidadPorPallet}";

                // Actualizar mensaje de ayuda
                if (txtAyudaCantidad != null)
                {
                    txtAyudaCantidad.Text = $"Cantidad original: {_originalCantidadPorPallet}. Solo puede reducir la cantidad, no aumentarla.";
                }
            }
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
            txtCantidadPallet.Text = _cantidadModificada.ToString();
            txtProgramaProduccion.Text = _currentOrden.ProgramaProduccion;

            // ✅ NUEVO: Actualizar indicadores de cantidad
            ActualizarIndicadoresCantidad();

            // Formato de fecha para el lote: DDMMYY
            string fechaFormateada = fechaProduccion.ToString("ddMMyy");
            txtFechaElaboracion.Text = fechaFormateada;

            // Crear el lote con el nuevo formato: FECHAELABORACION + TIPO_PRODUCTO
            string lote = $"{fechaFormateada}{_tipoProductoSeleccionado}";
            txtLote.Text = lote;

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

            // Formato de fecha para el lote: DDMMYY
            string fechaFormateada = fechaProduccion.ToString("ddMMyy");

            // Crear etiqueta temporal para vista previa con el nuevo formato de lote
            var etiquetaTemporal = new EtiquetaGenerada
            {
                // El lote ahora es la fecha de elaboración + dígito tipo de producto
                LOTN = $"{fechaFormateada}{_tipoProductoSeleccionado}",
                EXPR = fechaProduccion.AddDays(_currentOrden.DiasCaducidad),
                LITM = _currentOrden.NumeroArticulo,
                SOQS = _cantidadModificada,
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
                var imagenCodigo = _barcodeService?.GenerarImagenCodigoBarras(
                    codigoBarrasPreliminar,
                    ZXing.BarcodeFormat.CODE_128,
                    280,  // Ancho
                    70    // Alto
                );

                // Crear objeto de vista previa
                var vistaPrevia = new EtiquetaPreview
                {
                    NumeroArticulo = _currentOrden.NumeroArticulo,
                    Descripcion = _currentOrden.Descripcion,
                    Lote = etiquetaTemporal.LOTN,
                    FechaProduccion = fechaProduccion,
                    FechaVencimiento = etiquetaTemporal.EXPR,
                    CantidadPorPallet = _cantidadModificada,
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

                // Crear la entidad de etiqueta con el nuevo formato de lote
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

                // Crear la entidad de etiqueta con el nuevo formato de lote
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

                // Registrar actividad con la cantidad actualizada y tipo de producto
                ActivityLog?.Invoke($"Enviando etiqueta a la impresora con {_cantidadModificada} unidades por pallet y tipo de producto: {_tipoProductoSeleccionado}...", ActivityLogItem.LogLevel.Info);

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
                    // Si hubo un cambio en la cantidad por pallet respecto al original, registrarlo
                    if (_originalCantidadPorPallet != _cantidadModificada)
                    {
                        string mensaje = $"Se modificó la cantidad por pallet de {_originalCantidadPorPallet} a {_cantidadModificada}";
                        _logService.Information(mensaje);
                        ActivityLog?.Invoke(mensaje, ActivityLogItem.LogLevel.Info);
                    }

                    // Registrar el tipo de producto usado
                    string tipoProductoMensaje = $"Tipo de producto seleccionado: {GetTipoProductoDescripcion(_tipoProductoSeleccionado)}";
                    _logService.Information(tipoProductoMensaje);
                    ActivityLog?.Invoke(tipoProductoMensaje, ActivityLogItem.LogLevel.Info);

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

        private EtiquetaGenerada CrearEtiquetaGenerada(OrdenProduccion orden, string turnoActual,
            DateTime fechaProduccion, string numeroTransaccion)
        {
            // Obtener siguientes números secuenciales con manejo de errores
            int numeroSecuencial = 1;
            int numeroPallet = 1;
            string fechaJuliana = "";
            

            int lineaId = 1;
            string nombreLinea = "ETIQ"; // Valor por defecto

            try
            {
                // Obtener el número de línea desde la configuración
                var lineNumber = _configuration.GetValue<string>("AppSettings:LineNumber") ?? "1";
                lineaId = int.Parse(lineNumber);

                // Acceder al servicio de líneas que ya debe estar inyectado en el constructor
                if (_lineaService != null)
                {
                    var linea = _lineaService.GetLineaById(lineaId);
                    if (linea != null && !string.IsNullOrEmpty(linea.Nombre))
                    {
                        
                        nombreLinea = linea.Nombre; 
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.Warning("Error al obtener nombre de línea: {0}", ex.Message);
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

            // Si la cantidad por pallet fue modificada, registrarlo en el log
            if (_originalCantidadPorPallet != _cantidadModificada)
            {
                _logService.Information("Creando etiqueta con cantidad por pallet modificada de {0} a {1}",
                    _originalCantidadPorPallet, _cantidadModificada);
            }

            // Formato de fecha para el lote: DDMMYY
            string fechaFormateada = fechaProduccion.ToString("ddMMyy");

            // Crear el lote con el nuevo formato: FECHAELABORACION + TIPO_PRODUCTO
            string lote = $"{fechaFormateada}{_tipoProductoSeleccionado}";

            // Registrar tipo de producto en el log
            _logService.Information("Tipo de producto seleccionado: {0} - {1}",
                _tipoProductoSeleccionado, GetTipoProductoDescripcion(_tipoProductoSeleccionado));

            return new EtiquetaGenerada
            {
                EDUS = nombreLinea,
                EDDT = fechaJuliana,
                EDTN = numeroTransaccion ?? fechaProduccion.ToString("MMdd"),
                EDLN = numeroSecuencial,
                DOCO = orden.ProgramaProduccion,
                LITM = orden.NumeroArticulo,
                SOQS = _cantidadModificada,
                UOM1 = orden.UnidadMedida ?? "UN",
                // Nuevo formato de lote: FECHAELABORACION + TIPO_PRODUCTO
                LOTN = lote,
                EXPR = fechaProduccion.AddDays(orden.DiasCaducidad),
                TDAY = horaJuliana,
                SHFT = turnoActual,
                URDT = fechaVencimientoJuliana,
                SEC = numeroPallet,
                ESTADO = "0", // 0 = Pendiente
                URRF = orden.DUN14,
                FechaCreacion = DateTime.Now,
                Confirmada = false,
                LineaId = Convert.ToInt32(_configuration.GetValue<string>("AppSettings:LineNumber") ?? "0"),
                MotivoNoConfirmacion = _originalCantidadPorPallet != _cantidadModificada ?
                    $"Cantidad modificada manualmente de {_originalCantidadPorPallet} a {_cantidadModificada}" : ""
            };
        }

        // Obtener la descripción del tipo de producto seleccionado
        private string GetTipoProductoDescripcion(string tipoProducto)
        {
            switch (tipoProducto)
            {
                case "0": return "Producto OK";
                case "2": return "Reproceso (Producto sin terminar algún proceso)";
                case "5": return "Reprocesado";
                case "8": return "Producto W (Producto fuera de especificación)";
                case "9": return "Producto B (Producto con dispersión)";
                default: return "Tipo desconocido";
            }
        }
        private void MostrarError(string mensaje)
        {
            txtError.Text = mensaje;
            progressBar.Visibility = Visibility.Collapsed;
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
        #endregion

        #region Manejadores de Eventos
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
            // Si se modificó la cantidad, preguntar al usuario si desea descartar los cambios
            if (_currentOrden != null && (_originalCantidadPorPallet != _cantidadModificada))
            {
                var result = MessageBox.Show(
                    "Ha modificado la cantidad por pallet. ¿Desea descartar los cambios?",
                    "Confirmación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                {
                    return;
                }

                // Restaurar el valor original
                _cantidadModificada = _originalCantidadPorPallet;
            }

            // Notificar que se solicitó cancelar
            ActivityLog?.Invoke("Operación cancelada por el usuario", ActivityLogItem.LogLevel.Info);
            CancelarSolicitado?.Invoke(this, EventArgs.Empty);
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

        // Manejadores de eventos para la impresión en segundo plano
        private void PrintService_PrintProgress(object sender, ZebraPrintService.PrintProgressEventArgs e)
        {
            // Asegurarse de que se ejecuta en el hilo de UI
            Dispatcher.Invoke(() =>
            {
                progressBar.Value = e.Percentage;
                txtError.Text = e.Message;

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
                    ActivityLog?.Invoke("Etiqueta enviada a la impresora. Proceda a verificar.", ActivityLogItem.LogLevel.Info);

                    // Si hubo un cambio en la cantidad por pallet respecto al original, registrarlo
                    if (_originalCantidadPorPallet != _cantidadModificada)
                    {
                        string mensaje = $"Se utilizó una cantidad modificada: {_cantidadModificada} unidades por pallet (original: {_originalCantidadPorPallet})";
                        _logService.Information(mensaje);
                        ActivityLog?.Invoke(mensaje, ActivityLogItem.LogLevel.Info);
                    }

                    // Registrar el tipo de producto usado
                    string tipoProductoMensaje = $"Tipo de producto utilizado: {GetTipoProductoDescripcion(_tipoProductoSeleccionado)}";
                    _logService.Information(tipoProductoMensaje);
                    ActivityLog?.Invoke(tipoProductoMensaje, ActivityLogItem.LogLevel.Info);

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

        // Manejador para cuando se selecciona un tipo de producto
        private void CmbTipoProducto_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (cmbTipoProducto.SelectedItem != null)
                {
                    // Obtener el valor del tag que contiene el dígito del tipo de producto
                    ComboBoxItem selectedItem = (ComboBoxItem)cmbTipoProducto.SelectedItem;
                    _tipoProductoSeleccionado = selectedItem.Tag?.ToString() ?? "0";

                    // Actualizar la descripción - Verificar que el control exista
                    if (txtTipoProductoDescripcion != null)
                    {
                        txtTipoProductoDescripcion.Text = GetTipoProductoDescripcion(_tipoProductoSeleccionado);
                    }

                    // Actualizar lote y vista previa con el nuevo tipo seleccionado
                    if (_currentOrden != null)
                    {
                        var (turnoActual, fechaProduccion) = _turnoService.ObtenerTurnoYFechaProduccion();

                        // Actualizar el lote en la UI
                        string fechaFormateada = fechaProduccion.ToString("ddMMyy");
                        if (txtLote != null)
                        {
                            txtLote.Text = $"{fechaFormateada}{_tipoProductoSeleccionado}";
                        }

                        // Actualizar vista previa
                        GenerarVistaPreliminar(turnoActual, fechaProduccion);

                        // Registrar cambio en log
                        string mensaje = $"Tipo de producto cambiado a: {GetTipoProductoDescripcion(_tipoProductoSeleccionado)}";
                        _logService?.Information(mensaje);
                        ActivityLog?.Invoke(mensaje, ActivityLogItem.LogLevel.Info);
                    }
                }
            }
            catch (Exception ex)
            {
                // Manejo seguro de la excepción
                _logService?.Error(ex, "Error al cambiar el tipo de producto");
                ActivityLog?.Invoke($"Error al cambiar tipo de producto: {ex.Message}", ActivityLogItem.LogLevel.Error);
            }
        }

        // Nuevos métodos para el manejo de la edición de cantidad por pallet
        private void TxtCantidadPallet_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Solo permitir valores numéricos
            e.Handled = !int.TryParse(e.Text, out _);
        }

        private void TxtCantidadPallet_LostFocus(object sender, RoutedEventArgs e)
        {
            // Validar y actualizar la cantidad cuando el campo pierde el foco
            if (int.TryParse(txtCantidadPallet.Text, out int cantidad))
            {
                //  No permitir cantidad mayor a la original
                if (cantidad > _originalCantidadPorPallet)
                {
                    MessageBox.Show(
                        $"La cantidad por pallet no puede ser mayor a la cantidad original.\n\n" +
                        $"Cantidad original: {_originalCantidadPorPallet}\n" +
                        $"Cantidad ingresada: {cantidad}\n\n" +
                        $"Se restaurará la cantidad original.",
                        "Cantidad Excedida",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    // Restaurar la cantidad original
                    txtCantidadPallet.Text = _originalCantidadPorPallet.ToString();
                    _cantidadModificada = _originalCantidadPorPallet;

                    // Registrar el intento
                    _logService.Warning("Intento de exceder cantidad original: {CantidadIngresada} > {CantidadOriginal}",
                        cantidad, _originalCantidadPorPallet);
                    ActivityLog?.Invoke($"Cantidad rechazada: {cantidad} excede el máximo permitido de {_originalCantidadPorPallet}",
                        ActivityLogItem.LogLevel.Warning);

                    return;
                }

                //  Cantidad debe ser mayor a 0
                if (cantidad <= 0)
                {
                    MessageBox.Show(
                        "La cantidad por pallet debe ser mayor a 0.",
                        "Cantidad Inválida",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    // Restaurar la cantidad modificada actual
                    txtCantidadPallet.Text = _cantidadModificada.ToString();
                    ActivityLog?.Invoke("Cantidad inválida: debe ser mayor a 0", ActivityLogItem.LogLevel.Warning);
                    return;
                }

                // Si la cantidad ha cambiado respecto a la actual
                if (_cantidadModificada != cantidad)
                {
                    // Actualizar la cantidad modificada
                    _cantidadModificada = cantidad;

                    // Registrar el cambio
                    if (cantidad < _originalCantidadPorPallet)
                    {
                        _logService.Information("Cantidad por pallet reducida de {Original} a {Nueva}",
                            _originalCantidadPorPallet, cantidad);
                        ActivityLog?.Invoke($"Cantidad reducida de {_originalCantidadPorPallet} a {cantidad}",
                            ActivityLogItem.LogLevel.Info);
                    }
                    else if (cantidad == _originalCantidadPorPallet)
                    {
                        _logService.Information("Cantidad por pallet restaurada al valor original: {Cantidad}", cantidad);
                        ActivityLog?.Invoke($"Cantidad restaurada al valor original: {cantidad}",
                            ActivityLogItem.LogLevel.Info);
                    }

                    // Actualizar la vista previa
                    try
                    {
                        var (turnoActual, fechaProduccion) = _turnoService.ObtenerTurnoYFechaProduccion();
                        GenerarVistaPreliminar(turnoActual, fechaProduccion);
                    }
                    catch (Exception ex)
                    {
                        _logService.Error(ex, "Error al actualizar vista previa después de cambio de cantidad");
                    }
                }
            }
            else
            {
                // Si el valor no es válido, restaurar el valor modificado actual
                txtCantidadPallet.Text = _cantidadModificada.ToString();
                ActivityLog?.Invoke("Formato de cantidad inválido, se restauró el valor actual", ActivityLogItem.LogLevel.Warning);
            }
        }
        private void TxtCantidadPallet_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null && !string.IsNullOrEmpty(textBox.Text))
            {
                if (int.TryParse(textBox.Text, out int cantidad))
                {
                    // Mostrar advertencia visual si excede el máximo
                    if (cantidad > _originalCantidadPorPallet)
                    {
                        textBox.Background = new SolidColorBrush(Color.FromRgb(255, 230, 230)); // Fondo rojizo claro
                        textBox.BorderBrush = new SolidColorBrush(Colors.Red);
                        textBox.ToolTip = $"Cantidad máxima permitida: {_originalCantidadPorPallet}";

                        // Mostrar mensaje en el área de ayuda
                        if (txtAyudaCantidad != null)
                        {
                            txtAyudaCantidad.Text = $"⚠️ ADVERTENCIA: La cantidad {cantidad} excede el máximo permitido de {_originalCantidadPorPallet}";
                            txtAyudaCantidad.Foreground = new SolidColorBrush(Colors.Red);
                        }
                    }
                    else if (cantidad <= 0)
                    {
                        textBox.Background = new SolidColorBrush(Color.FromRgb(255, 240, 200)); // Fondo amarillento
                        textBox.BorderBrush = new SolidColorBrush(Colors.Orange);
                        textBox.ToolTip = "La cantidad debe ser mayor a 0";

                        if (txtAyudaCantidad != null)
                        {
                            txtAyudaCantidad.Text = "⚠️ La cantidad debe ser mayor a 0";
                            txtAyudaCantidad.Foreground = new SolidColorBrush(Colors.Orange);
                        }
                    }
                    else
                    {
                        // Cantidad válida
                        textBox.Background = SystemColors.WindowBrush; // Fondo normal
                        textBox.BorderBrush = new SolidColorBrush(Colors.Gray);
                        textBox.ToolTip = null;

                        if (txtAyudaCantidad != null)
                        {
                            txtAyudaCantidad.Text = $"Cantidad original: {_originalCantidadPorPallet}. Solo puede reducir la cantidad, no aumentarla.";
                            txtAyudaCantidad.Foreground = new SolidColorBrush(Colors.Gray);
                        }
                    }
                }
                else
                {
                    // Texto no numérico
                    textBox.Background = new SolidColorBrush(Color.FromRgb(255, 240, 200));
                    textBox.BorderBrush = new SolidColorBrush(Colors.Orange);
                    textBox.ToolTip = "Ingrese solo números";
                }
            }
            else
            {
                // Campo vacío
                textBox.Background = SystemColors.WindowBrush;
                textBox.BorderBrush = new SolidColorBrush(Colors.Gray);
                textBox.ToolTip = null;
            }
        }
        #endregion
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