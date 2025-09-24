using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MolcaEtiquetadoManual.Core.Interfaces;
using MolcaEtiquetadoManual.Core.Models;
using Microsoft.Extensions.Configuration;
using System.ComponentModel;
using System.Threading;
using System.Net.NetworkInformation;

namespace MolcaEtiquetadoManual.Core.Services
{
    public class ZebraPrintService : IPrintService
    {
        private readonly string _printerIp;
        private readonly int _printerPort;
        private readonly string _formatName;
        private readonly string _formatUnit;
        private readonly int _labelQuantity;
        private readonly ILogService _logService;
        private readonly IConfiguration _configuration;
        private readonly BackgroundWorker _printWorker;
        private CancellationTokenSource _cancellationTokenSource;

        public ZebraPrintService(IConfiguration configuration, ILogService logService)
        {
            // Obtener configuración de appSettings.json
            var printerSection = configuration.GetSection("PrinterSettings");
            _printerIp = printerSection["IpAddress"];
            _printerPort = int.Parse(printerSection["Port"]);
            _formatName = printerSection["FormatName"] ?? "MOLCA.ZPL";
            _formatUnit = printerSection["FormatUnit"] ?? "E";
            // Obtener cantidad de etiquetas (valor por defecto: 1)
            _labelQuantity = int.Parse(printerSection["LabelQuantity"] ?? "1");
            _logService = logService;
            _configuration = configuration;

            _printWorker = new BackgroundWorker();
            _printWorker.WorkerReportsProgress = true;
            _printWorker.WorkerSupportsCancellation = true;
            _printWorker.DoWork += PrintWorker_DoWork;
            _printWorker.ProgressChanged += PrintWorker_ProgressChanged;
            _printWorker.RunWorkerCompleted += PrintWorker_RunWorkerCompleted;

            _logService.Information($"Inicializando servicio de impresión: IP={_printerIp}, Puerto={_printerPort}, Formato={_formatName}, Cantidad de etiquetas={_labelQuantity}");
        }

        public event EventHandler<PrintProgressEventArgs> PrintProgress;
        public event EventHandler<PrintCompletedEventArgs> PrintCompleted;

        public bool ImprimirEtiqueta(OrdenProduccion orden, EtiquetaGenerada etiqueta, string codigoBarras)
        {
            try
            {
                if (_printWorker.IsBusy)
                {
                    _logService.Warning("Ya hay una operación de impresión en curso");
                    return false;
                }

                // Comprobar si debemos simular éxito
                bool simularExito = false;

                if (simularExito)
                {
                    _logService.Information("Simulando impresión exitosa por configuración (SimulateSuccessfulPrint=true)");
                    SimularProcesoDeImpresion();
                    return true;
                }

                // Generar el ZPL
                string comandoZpl = GenerarComandoZPL(orden, etiqueta, codigoBarras);
                GuardarComandoZplParaDebug(comandoZpl);

                // Iniciar el token de cancelación
                _cancellationTokenSource = new CancellationTokenSource();

                // Crear objeto con los datos para el worker
                var printData = new PrintJobData
                {
                    ZplCommand = comandoZpl,
                    Orden = orden,
                    Etiqueta = etiqueta,
                    CodigoBarras = codigoBarras
                };

                // Notificar inicio
                OnPrintProgress(0, $"Iniciando proceso de impresión de {_labelQuantity} etiqueta(s)...");

                // Iniciar el worker
                _printWorker.RunWorkerAsync(printData);

                return true;
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al iniciar el proceso de impresión");
                return false;
            }
        }

        // Método privado nuevo para simular el proceso de impresión
        private void SimularProcesoDeImpresion()
        {
            // Crear una tarea para simular el progreso en segundo plano
            Task.Run(async () =>
            {
                try
                {
                    // Simulamos el progreso de impresión
                    OnPrintProgress(10, $"Conectando con la impresora para imprimir {_labelQuantity} etiqueta(s)...");
                    await Task.Delay(500);

                    OnPrintProgress(30, "Enviando datos de la etiqueta...");
                    await Task.Delay(800);

                    OnPrintProgress(60, "Procesando impresión...");
                    await Task.Delay(700);

                    OnPrintProgress(90, "Finalizando impresión...");
                    await Task.Delay(500);

                    // Completamos la impresión con éxito
                    OnPrintCompleted(true, $"Impresión de {_labelQuantity} etiqueta(s) completada exitosamente.");
                }
                catch (Exception ex)
                {
                    _logService.Error(ex, "Error en la simulación de impresión");
                    OnPrintCompleted(false, $"Error en la impresión: {ex.Message}", ex);
                }
            });
        }

        private void GuardarComandoZplParaDebug(string comandoZpl)
        {
            try
            {
                // Crear directorio si no existe
                string directorio = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MolcaEtiquetadoManual", "ZplDebug");

                if (!Directory.Exists(directorio))
                {
                    Directory.CreateDirectory(directorio);
                }

                // Guardar el archivo con timestamp
                string archivo = Path.Combine(directorio,
                    $"zpl_comando_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

                File.WriteAllText(archivo, comandoZpl);
                _logService.Debug($"Comando ZPL guardado en {archivo} para debugging");
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al guardar comando ZPL para debug");
            }
        }

        private string GenerarComandoZPL(OrdenProduccion orden, EtiquetaGenerada etiqueta, string codigoBarras)
        {
            // Preparar datos para las variables de la etiqueta
            string fechaVencimiento = etiqueta.EXPR.ToString("ddMMyy");  // Vencimiento FN1
            string fechaProduccion = DateTime.Now.ToString("yyMMdd HH:mm");  // Fecha Producción FN2
            string lote1 = etiqueta.EDDT;  // Lote 1 - FN3 (Julian Date)
            string lote2 = $"{etiqueta.DOCO}{etiqueta.SEC}";  // Lote 2 - FN4
            string descripcion = orden.Descripcion;  // Descripción - FN5
            string codigoJDE = orden.NumeroArticulo.PadLeft(8, '0');  // Código JDE - FN6
            string cantidadBultos = $"{etiqueta.SOQS} Bultos";  // Bultos - FN7
            string codBarrasH = codigoBarras;  // Código de barras horizontal - FN8
            string codBarrasV = etiqueta.LOTN;  // Código de barras vertical - FN9

            // Generar el comando ZPL que usa el formato guardado
            StringBuilder sb = new StringBuilder();
            char chr = '^';  // Carácter de control para comandos ZPL

            sb.AppendLine($"{chr}XA");  // Iniciar formato
            sb.AppendLine($"{chr}XF{_formatUnit}:{_formatName}");  // Cargar formato guardado
            sb.AppendLine($"{chr}FN1{chr}FD{fechaVencimiento}{chr}FS");  // Vencimiento
            sb.AppendLine($"{chr}FN2{chr}FD{fechaProduccion}{chr}FS");  // Fecha Producción
            sb.AppendLine($"{chr}FN3{chr}FD{lote1}{chr}FS");  // Lote 1
            sb.AppendLine($"{chr}FN4{chr}FD{lote2}{chr}FS");  // Lote 2
            sb.AppendLine($"{chr}FN5{chr}FD{descripcion}{chr}FS");  // Descripción
            sb.AppendLine($"{chr}FN6{chr}FD{codigoJDE}{chr}FS");  // Código JDE
            sb.AppendLine($"{chr}FN7{chr}FD{cantidadBultos}{chr}FS");  // Bultos
            sb.AppendLine($"{chr}FN8{chr}FD{codBarrasH}{chr}FS");  // Código barras H
            sb.AppendLine($"{chr}FN9{chr}FD{codBarrasV}{chr}FS");  // Código barras V

            // Usar la cantidad de etiquetas configurada
            sb.AppendLine($"{chr}PQ{_labelQuantity},0,1,Y");  // Cantidad de etiquetas configurada

            sb.AppendLine($"{chr}XZ");  // Finalizar formato

            return sb.ToString();
        }

        private void PrintWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var worker = (BackgroundWorker)sender;
            var jobData = (PrintJobData)e.Argument;

            try
            {
                if (worker.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }

                // Reportar progreso
                worker.ReportProgress(10, $"Conectando con la impresora para imprimir {_labelQuantity} etiqueta(s)...");

                // Usar el token de cancelación
                var token = _cancellationTokenSource.Token;

                // Conectar a la impresora
                using (TcpClient client = new TcpClient())
                {
                    try
                    {
                        worker.ReportProgress(20, $"Estableciendo conexión con {_printerIp}:{_printerPort}...");

                        // Intento de conexión
                        var connectTask = client.ConnectAsync(_printerIp, _printerPort);

                        int timeoutMs = 10000; // 10 segundos
                        bool connectSuccess = false;

                        // Esperar la conexión con posibilidad de cancelación
                        var timeoutTask = Task.Delay(timeoutMs, token);
                        var completedTask = Task.WhenAny(connectTask, timeoutTask).Result;

                        if (completedTask == connectTask)
                        {
                            // La conexión se completó antes del timeout
                            connectTask.Wait(token); // Propagar excepciones
                            connectSuccess = true;
                        }
                        else
                        {
                            throw new TimeoutException($"Tiempo de espera agotado al conectar con la impresora {_printerIp}:{_printerPort}");
                        }

                        if (!connectSuccess)
                        {
                            throw new Exception("No se pudo establecer la conexión con la impresora");
                        }

                        if (worker.CancellationPending)
                        {
                            e.Cancel = true;
                            return;
                        }

                        worker.ReportProgress(40, $"Conexión establecida. Preparando datos para {_labelQuantity} etiqueta(s)...");

                        // Convertir el texto a bytes
                        byte[] data = Encoding.ASCII.GetBytes(jobData.ZplCommand);

                        worker.ReportProgress(60, $"Enviando datos a la impresora ({data.Length} bytes)...");

                        // Enviar los datos usando un método síncrono dentro del thread asíncrono
                        using (NetworkStream stream = client.GetStream())
                        {
                            // Configurar timeout
                            stream.WriteTimeout = 5000;

                            // Dividir en bloques más pequeños para reportar progreso
                            const int blockSize = 1024;
                            int totalBytes = data.Length;
                            int bytesSent = 0;

                            while (bytesSent < totalBytes)
                            {
                                if (worker.CancellationPending)
                                {
                                    e.Cancel = true;
                                    return;
                                }

                                int bytesToSend = Math.Min(blockSize, totalBytes - bytesSent);
                                stream.Write(data, bytesSent, bytesToSend);

                                bytesSent += bytesToSend;
                                int progressPercentage = (int)((double)bytesSent / totalBytes * 30) + 60; // 60-90%

                                worker.ReportProgress(
                                    Math.Min(progressPercentage, 90),
                                    $"Enviando datos: {bytesSent}/{totalBytes} bytes");
                            }

                            worker.ReportProgress(95, $"Finalizando y verificando la impresión de {_labelQuantity} etiqueta(s)...");

                            // Flush para asegurar que todos los datos se enviaron
                            stream.Flush();
                        }

                        worker.ReportProgress(100, $"¡Impresión de {_labelQuantity} etiqueta(s) completada exitosamente!");

                        // Devolver resultado exitoso
                        e.Result = true;
                    }
                    catch (OperationCanceledException)
                    {
                        _logService.Warning("Operación de impresión cancelada por el usuario");
                        e.Cancel = true;
                    }
                    catch (Exception ex)
                    {
                        _logService.Error(ex, "Error durante el proceso de impresión");
                        e.Result = ex;
                    }
                    finally
                    {
                        // Asegurarse de que el cliente se cierra
                        if (client.Connected)
                        {
                            client.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error inesperado en el proceso de impresión");
                e.Result = ex;
            }
        }

        private void PrintWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            string message = e.UserState as string ?? "Procesando...";
            OnPrintProgress(e.ProgressPercentage, message);
        }

        // Métodos para levantar eventos
        protected virtual void OnPrintProgress(int percentage, string message)
        {
            PrintProgress?.Invoke(this, new PrintProgressEventArgs(percentage, message));
        }

        protected virtual void OnPrintCompleted(bool success, string message, Exception error = null)
        {
            PrintCompleted?.Invoke(this, new PrintCompletedEventArgs(success, message, error));
        }

        private void PrintWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            bool success = false;
            string message;
            Exception error = null;

            if (e.Cancelled)
            {
                message = "La impresión fue cancelada por el usuario.";
                _logService.Information(message);
            }
            else if (e.Error != null)
            {
                message = $"Error durante la impresión: {e.Error.Message}";
                error = e.Error;
                _logService.Error(e.Error, message);
            }
            else if (e.Result is Exception resultEx)
            {
                message = $"Error durante la impresión: {resultEx.Message}";
                error = resultEx;
                _logService.Error(resultEx, message);
            }
            else
            {
                success = (bool)e.Result;
                message = success ? $"Impresión de {_labelQuantity} etiqueta(s) completada exitosamente." : "La impresión no pudo completarse.";
                _logService.Information(message);
            }

            // Limpiar recursos
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            // Notificar a la UI
            OnPrintCompleted(success, message, error);
        }

        public void CancelarImpresion()
        {
            if (_printWorker.IsBusy)
            {
                _logService.Information("Cancelando impresión...");
                _printWorker.CancelAsync();
                _cancellationTokenSource?.Cancel();
            }
        }

        // Método para pruebas que simula la impresión sin enviar a una impresora real
        public bool SimularImpresion(OrdenProduccion orden, EtiquetaGenerada etiqueta, string codigoBarras)
        {
            try
            {
                string comandoZpl = GenerarComandoZPL(orden, etiqueta, codigoBarras);
                GuardarComandoZplParaDebug(comandoZpl);

                _logService.Information($"Simulación de impresión exitosa para {_labelQuantity} etiqueta(s)");
                return true;
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error en simulación de impresión");
                return false;
            }
        }

        private class PrintJobData
        {
            public string ZplCommand { get; set; }
            public OrdenProduccion Orden { get; set; }
            public EtiquetaGenerada Etiqueta { get; set; }
            public string CodigoBarras { get; set; }
        }

        public class PrintProgressEventArgs : EventArgs
        {
            public int Percentage { get; }
            public string Message { get; }

            public PrintProgressEventArgs(int percentage, string message)
            {
                Percentage = percentage;
                Message = message;
            }
        }

        public class PrintCompletedEventArgs : EventArgs
        {
            public bool Success { get; }
            public string Message { get; }
            public Exception Error { get; }

            public PrintCompletedEventArgs(bool success, string message, Exception error = null)
            {
                Success = success;
                Message = message;
                Error = error;
            }
        }
    }
}