using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MolcaEtiquetadoManual.Core.Interfaces;

namespace MolcaEtiquetadoManual.Core.Services
{
    /// <summary>
    /// Servicio para bloquear procesos del sistema que puedan interferir con el modo Kiosk
    /// Versión corregida sin dependencias de System.Management
    /// </summary>
    public class ProcessBlocker
    {
        private readonly ILogService _logService;
        private readonly Timer _processWatcher;
        private bool _isEnabled = false;

        // Lista de procesos que deben ser bloqueados en modo Kiosk
        private readonly string[] _blockedProcesses = {
            "taskmgr",          // Task Manager
            "regedit",          // Registry Editor  
            "cmd",              // Command Prompt
            "powershell",       // PowerShell
            "powershell_ise",   // PowerShell ISE
            "mmc",              // Microsoft Management Console
            "control",          // Control Panel
            "msconfig",         // System Configuration
            "winver",           // Windows Version
            "dxdiag",           // DirectX Diagnostic
            "msinfo32",         // System Information
            "eventvwr",         // Event Viewer
            "services",         // Services Manager
            "devmgmt",          // Device Manager
            "compmgmt",         // Computer Management
            "gpedit",           // Group Policy Editor
            "secpol",           // Local Security Policy
            "lusrmgr",          // Local Users and Groups
            "perfmon",          // Performance Monitor
            "resmon",           // Resource Monitor
            "sysdm",            // System Properties
            "appwiz",           // Programs and Features
            "desk",             // Display Settings
            "timedate",         // Date and Time
            "intl",             // Regional Settings
            "joy",              // Game Controllers
            "main",             // Mouse Properties
            "mmsys",            // Sound
            "powercfg",         // Power Options
            "telephon",         // Phone and Modem Options
            "hdwwiz",           // Add Hardware Wizard
            "notepad",          // Notepad (opcional, puedes removerlo si necesitas permitirlo)
            "calc",             // Calculator (opcional)
            "mspaint",          // Paint (opcional)
            "write"             // WordPad (opcional)
        };

        // Procesos críticos que NUNCA deben ser terminados
        private readonly string[] _criticalProcesses = {
            "winlogon",
            "csrss",
            "smss",
            "wininit",
            "services",
            "lsass",
            "dwm",
            "audiodg",
            "conhost"
        };

        public ProcessBlocker(ILogService logService)
        {
            _logService = logService;

            // Configurar timer para verificar procesos cada 2 segundos
            _processWatcher = new Timer(CheckForBlockedProcesses, null,
                Timeout.Infinite, Timeout.Infinite);
        }

        public void EnableProcessBlocking()
        {
            try
            {
                _isEnabled = true;
                _processWatcher.Change(TimeSpan.Zero, TimeSpan.FromSeconds(2));
                _logService.Information("Bloqueador de procesos habilitado");
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al habilitar bloqueador de procesos");
            }
        }

        public void DisableProcessBlocking()
        {
            try
            {
                _isEnabled = false;
                _processWatcher.Change(Timeout.Infinite, Timeout.Infinite);
                _logService.Information("Bloqueador de procesos deshabilitado");
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al deshabilitar bloqueador de procesos");
            }
        }

        private void CheckForBlockedProcesses(object state)
        {
            if (!_isEnabled) return;

            try
            {
                var currentProcesses = Process.GetProcesses();
                var ourProcessId = Process.GetCurrentProcess().Id;

                foreach (var process in currentProcesses)
                {
                    try
                    {
                        // No interferir con nuestro propio proceso
                        if (process.Id == ourProcessId)
                            continue;

                        string processName = process.ProcessName.ToLower();

                        // NUNCA terminar procesos críticos del sistema
                        if (_criticalProcesses.Contains(processName))
                            continue;

                        // Verificar si es un proceso bloqueado
                        if (_blockedProcesses.Contains(processName))
                        {
                            // Hacer excepciones especiales
                            if (ShouldAllowProcess(process, processName))
                                continue;

                            _logService.Warning("Terminando proceso bloqueado: {ProcessName} (PID: {ProcessId})",
                                processName, process.Id);

                            // Intentar cerrar el proceso de forma elegante primero
                            bool closedGracefully = false;
                            try
                            {
                                if (process.MainWindowHandle != IntPtr.Zero)
                                {
                                    closedGracefully = process.CloseMainWindow();
                                }
                            }
                            catch (Exception ex)
                            {
                                _logService.Debug("No se pudo cerrar elegantemente {ProcessName}: {Error}",
                                    processName, ex.Message);
                            }

                            // Si no se puede cerrar elegantemente, forzar terminación después de un tiempo
                            if (!closedGracefully)
                            {
                                Task.Delay(1500).ContinueWith(_ =>
                                {
                                    try
                                    {
                                        // Verificar si el proceso aún existe antes de intentar terminarlo
                                        var stillRunning = Process.GetProcesses()
                                            .FirstOrDefault(p => p.Id == process.Id);

                                        if (stillRunning != null && !stillRunning.HasExited)
                                        {
                                            stillRunning.Kill();
                                            _logService.Warning("Proceso {ProcessName} terminado forzadamente", processName);
                                            stillRunning.Dispose();
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _logService.Debug("No se pudo terminar proceso {ProcessName}: {Error}",
                                            processName, ex.Message);
                                    }
                                });
                            }
                        }
                        // Verificar procesos sospechosos adicionales
                        else if (IsSuspiciousProcess(process, processName))
                        {
                            _logService.Warning("Proceso sospechoso detectado: {ProcessName} (PID: {ProcessId})",
                                processName, process.Id);

                            // Solo registrar, no terminar automáticamente los procesos sospechosos
                            // Puedes cambiar esto si quieres una política más agresiva
                        }
                    }
                    catch (Exception ex)
                    {
                        // Algunos procesos pueden no ser accesibles debido a permisos
                        _logService.Debug("Error al verificar proceso {ProcessName}: {Error}",
                            process?.ProcessName ?? "Unknown", ex.Message);
                    }
                    finally
                    {
                        process?.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error en verificación de procesos bloqueados");
            }
        }

        private bool ShouldAllowProcess(Process process, string processName)
        {
            try
            {
                // Permitir el proceso principal de explorer.exe (escritorio)
                if (processName == "explorer")
                {
                    // Obtener todos los procesos explorer
                    var explorerProcesses = Process.GetProcessesByName("explorer");

                    if (explorerProcesses.Length <= 1)
                    {
                        // Solo hay uno, probablemente el principal del escritorio
                        return true;
                    }

                    // Si hay múltiples, permitir solo el más antiguo (normalmente el del escritorio)
                    try
                    {
                        var oldestExplorer = explorerProcesses
                            .Where(p => !p.HasExited)
                            .OrderBy(p => p.StartTime)
                            .FirstOrDefault();

                        if (oldestExplorer != null && oldestExplorer.Id == process.Id)
                        {
                            return true; // Permitir el proceso más antiguo
                        }
                    }
                    catch
                    {
                        // Si no podemos determinar el más antiguo, permitir el primero
                        return explorerProcesses[0].Id == process.Id;
                    }
                    finally
                    {
                        // Limpiar referencias
                        foreach (var p in explorerProcesses)
                        {
                            p?.Dispose();
                        }
                    }
                }

                // Para otros procesos en la lista de bloqueados, no permitir excepciones
                return false;
            }
            catch (Exception ex)
            {
                _logService.Debug("Error al evaluar si permitir proceso {ProcessName}: {Error}",
                    processName, ex.Message);
                return false; // En caso de error, bloquear por seguridad
            }
        }

        private bool IsSuspiciousProcess(Process process, string processName)
        {
            try
            {
                // Detectar posibles herramientas de administración o hacking
                string[] suspiciousNames = {
                    "netstat", "ipconfig", "ping", "telnet", "ftp",
                    "net", "sc", "wmic", "reg", "bcdedit",
                    "diskpart", "format", "chkdsk", "sfc",
                    "dism", "robocopy", "xcopy", "attrib",
                    "cipher", "compact", "convert", "defrag",
                    "fsutil", "icacls", "takeown", "cacls",
                    "runas", "psexec", "pslist", "pskill"
                };

                if (suspiciousNames.Contains(processName))
                {
                    return true;
                }

                // Detectar procesos que se ejecutan desde ubicaciones sospechosas
                try
                {
                    string fileName = process.MainModule?.FileName?.ToLower() ?? "";

                    // Ubicaciones sospechosas
                    string[] suspiciousLocations = {
                        @"\temp\",
                        @"\tmp\",
                        @"\downloads\",
                        @"\desktop\",
                        @"\documents\",
                        @"\appdata\roaming\",
                        @"\users\public\"
                    };

                    if (suspiciousLocations.Any(location => fileName.Contains(location)))
                    {
                        return true;
                    }
                }
                catch
                {
                    // Si no podemos acceder a la información del módulo, no es necesariamente sospechoso
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            try
            {
                DisableProcessBlocking();
                _processWatcher?.Dispose();
                _logService?.Information("ProcessBlocker liberado correctamente");
            }
            catch (Exception ex)
            {
                _logService?.Error(ex, "Error al liberar recursos del ProcessBlocker");
            }
        }
    }
}