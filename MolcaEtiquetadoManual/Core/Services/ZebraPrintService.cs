// Core/Services/ZebraPrintService.cs
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MolcaEtiquetadoManual.Core.Interfaces;
using MolcaEtiquetadoManual.Core.Models;
using Microsoft.Extensions.Configuration;

namespace MolcaEtiquetadoManual.Core.Services
{
    public class ZebraPrintService : IPrintService
    {
        private readonly string _printerIp;
        private readonly int _printerPort;
        private readonly string _formatName;
        private readonly string _formatUnit;
        private readonly ILogService _logService;

        public ZebraPrintService(IConfiguration configuration, ILogService logService)
        {
            // Obtener configuración de appSettings.json
            var printerSection = configuration.GetSection("PrinterSettings");
            _printerIp = printerSection["IpAddress"];
            _printerPort = int.Parse(printerSection["Port"]);
            _formatName = printerSection["FormatName"] ?? "MOLCA.ZPL";
            _formatUnit = printerSection["FormatUnit"] ?? "E";
            _logService = logService;

            _logService.Information($"Inicializando servicio de impresión: IP={_printerIp}, Puerto={_printerPort}, Formato={_formatName}");
        }

        public bool ImprimirEtiqueta(OrdenProduccion orden, EtiquetaGenerada etiqueta, string codigoBarras)
        {
            try
            {
                // Generar el ZPL con las variables para enviar a la impresora Zebra
                string comandoZpl = GenerarComandoZPL(orden, etiqueta, codigoBarras);

                // Guardar copia del comando ZPL para debugging
                GuardarComandoZplParaDebug(comandoZpl);

                _logService.Information($"Enviando comando ZPL a impresora {_printerIp}:{_printerPort}");

                // Enviar a la impresora
                EnviarAImpresora(comandoZpl).Wait();

                _logService.Information("Etiqueta enviada a la impresora correctamente");
                return true;
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al imprimir etiqueta");
                return false;
            }
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
            string fechaVencimiento = etiqueta.URDT.ToString("ddMMyy");  // Vencimiento FN1
            string fechaProduccion = DateTime.Now.ToString("yyMMdd HH:mm");  // Fecha Producción FN2
            string lote1 = etiqueta.EDDT;  // Lote 1 - FN3 (Julian Date)
            string lote2 = $"{etiqueta.DOCO}{etiqueta.SEC}";  // Lote 2 - FN4
            string descripcion = orden.Descripcion;  // Descripción - FN5
            string codigoJDE = orden.NumeroArticulo.PadLeft(8, '0');  // Código JDE - FN6
            string cantidadBultos = $"{orden.CantidadPorPallet} Bultos";  // Bultos - FN7
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
            sb.AppendLine($"{chr}PQ1,0,1,Y");  // Cantidad de etiquetas (1)
            sb.AppendLine($"{chr}XZ");  // Finalizar formato

            return sb.ToString();
        }

        private async Task EnviarAImpresora(string comandoZpl)
        {
            using (TcpClient client = new TcpClient())
            {
                try
                {
                    // Configurar timeouts razonables
                    var timeoutMs = 3000; // 3 segundos
                    var connectTask = client.ConnectAsync(_printerIp, _printerPort);

                    // Esperar con timeout
                    if (await Task.WhenAny(connectTask, Task.Delay(timeoutMs)) != connectTask)
                    {
                        throw new TimeoutException($"Tiempo de espera agotado al conectar con la impresora {_printerIp}:{_printerPort}");
                    }

                    // Verificar que la conexión se completó sin errores
                    await connectTask;

                    _logService.Debug($"Conexión establecida con la impresora {_printerIp}:{_printerPort}");

                    // Convertir el texto a bytes
                    byte[] data = Encoding.ASCII.GetBytes(comandoZpl);

                    // Enviar los datos
                    using (NetworkStream stream = client.GetStream())
                    {
                        // Configurar timeout para el stream
                        stream.WriteTimeout = timeoutMs;

                        await stream.WriteAsync(data, 0, data.Length);
                        await stream.FlushAsync();

                        _logService.Debug($"Enviados {data.Length} bytes a la impresora");
                    }
                }
                catch (Exception ex)
                {
                    _logService.Error(ex, "Error al enviar datos a la impresora");
                    throw new Exception($"Error al conectar con la impresora: {ex.Message}", ex);
                }
            }
        }

        // Método para pruebas que simula la impresión sin enviar a una impresora real
        public bool SimularImpresion(OrdenProduccion orden, EtiquetaGenerada etiqueta, string codigoBarras)
        {
            try
            {
                string comandoZpl = GenerarComandoZPL(orden, etiqueta, codigoBarras);
                GuardarComandoZplParaDebug(comandoZpl);

                _logService.Information("Simulación de impresión exitosa");
                return true;
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error en simulación de impresión");
                return false;
            }
        }
    }
}