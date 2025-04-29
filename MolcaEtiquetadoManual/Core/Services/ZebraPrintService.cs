using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// Core/Services/ZebraPrintService.cs
using MolcaEtiquetadoManual.Core.Interfaces;
using MolcaEtiquetadoManual.Core.Models;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace MolcaEtiquetadoManual.Core.Services
{
    public class ZebraPrintService : IPrintService
    {
        private readonly string _printerIp;
        private readonly int _printerPort;


        public ZebraPrintService(string printerIp, int printerPort)
        {
            _printerIp = printerIp;
            _printerPort = printerPort;
        }

        public bool ImprimirEtiqueta(OrdenProduccion orden, string codigoBarras)
        {
            try
            {
                // Generar el texto plano con las variables para enviar a la impresora Zebra
                string textoPlano = GenerarTextoEtiqueta(orden, codigoBarras);

                // Enviar a la impresora
                EnviarAImpresora(textoPlano);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al imprimir: {ex.Message}");
                return false;
            }
        }

        private string GenerarTextoEtiqueta(OrdenProduccion orden, string codigoBarras)
        {
            // Formato de fecha para la etiqueta: DDMMAA
            string fechaVencimiento = orden.FechaProduccionInicio.AddDays(orden.DiasCaducidad).ToString("ddMMyy");
            string fechaDeclaracion = DateTime.Now.ToString("ddMMyy");
            string horaDeclaracion = DateTime.Now.ToString("HHmmss");

            // Este es el texto plano que enviaremos a la impresora Zebra
            // La impresora ya tiene el formato guardado, solo enviamos las variables

            // Ejemplo de formato de texto para una impresora Zebra
            StringBuilder sb = new StringBuilder();

            // Formato para ZPL (Zebra Programming Language)
            sb.AppendLine("^XA"); // Inicio del formato

            // Variables para el formato guardado en la impresora
            sb.AppendLine($"^FN1^FD{orden.NumeroArticulo}^FS"); // Número de artículo
            sb.AppendLine($"^FN2^FD{orden.Descripcion}^FS"); // Descripción
            sb.AppendLine($"^FN3^FD{orden.CantidadPorPallet}^FS"); // Cantidad por pallet
            sb.AppendLine($"^FN4^FD{fechaVencimiento}^FS"); // Fecha de vencimiento
            sb.AppendLine($"^FN5^FD{fechaDeclaracion}^FS"); // Fecha de declaración
            sb.AppendLine($"^FN6^FD{horaDeclaracion}^FS"); // Hora de declaración
            sb.AppendLine($"^FN7^FD{orden.Lote}^FS"); // Lote (Programa + Turno)
            sb.AppendLine($"^FN8^FD{codigoBarras}^FS"); // Código de barras como texto

            // Para el código de barras en sí, asumiendo que se usa el campo 9 en el formato guardado
            sb.AppendLine($"^FN9^FD{codigoBarras}^FS");

            sb.AppendLine("^XZ"); // Fin del formato

            return sb.ToString();
        }

        private async Task EnviarAImpresora(string textoPlano)
        {
            using (TcpClient client = new TcpClient())
            {
                try
                {
                    // Conectar a la impresora
                    await client.ConnectAsync(_printerIp, _printerPort);

                    // Convertir el texto a bytes
                    byte[] data = Encoding.ASCII.GetBytes(textoPlano);

                    // Enviar los datos
                    using (NetworkStream stream = client.GetStream())
                    {
                        await stream.WriteAsync(data, 0, data.Length);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Error al conectar con la impresora: {ex.Message}", ex);
                }
            }
        }
    }
}