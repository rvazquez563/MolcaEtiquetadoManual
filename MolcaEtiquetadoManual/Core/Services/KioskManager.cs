using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using MolcaEtiquetadoManual.Core.Interfaces;
using System.Linq;

namespace MolcaEtiquetadoManual.Core.Services
{
    public class KioskManager
    {
        private readonly ILogService _logService;
        private bool _kioskModeEnabled = false;
        private System.Windows.Threading.DispatcherTimer _watchdogTimer;

        // APIs de Windows para controlar el sistema
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
            int X, int Y, int cx, int cy, uint uFlags);

        // ✅ CONSTANTES CORREGIDAS
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 1;
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;

        private Window _mainWindow;

        public KioskManager(ILogService logService)
        {
            _logService = logService;
        }

        /// <summary>
        /// Activa el modo Kiosk en la ventana especificada
        /// </summary>
        public void EnableKioskMode(Window mainWindow)
        {
            try
            {
                _mainWindow = mainWindow;
                _kioskModeEnabled = true;

                _logService.Information("=== INICIANDO ACTIVACIÓN DE MODO KIOSK ===");

                // 1. Configurar ventana principal para pantalla completa
                ConfigureMainWindow(mainWindow);

                // 2. Ocultar barra de tareas
                HideTaskbar();

                // 3. Deshabilitar Task Manager
                DisableTaskManager();

                // 4. Configurar interceptor de teclas peligrosas
                SetupKeyInterceptor(mainWindow);

                // 5. Iniciar watchdog para mantener ventana activa
                StartWatchdog();

                _logService.Information("=== MODO KIOSK ACTIVADO EXITOSAMENTE ===");
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "ERROR CRÍTICO al activar modo Kiosk");
                throw;
            }
        }

        /// <summary>
        /// Configura la ventana principal para modo Kiosk
        /// </summary>
        private void ConfigureMainWindow(Window mainWindow)
        {
            try
            {
                _logService.Information("Configurando ventana para modo Kiosk...");

                // ✅ CONFIGURACIÓN MEJORADA
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Configuraciones básicas
                    mainWindow.WindowStyle = WindowStyle.None;
                    mainWindow.ResizeMode = ResizeMode.NoResize;
                    mainWindow.Topmost = true;

                    // Obtener dimensiones de pantalla
                    var screenWidth = SystemParameters.PrimaryScreenWidth;
                    var screenHeight = SystemParameters.PrimaryScreenHeight;

                    // Configurar posición y tamaño
                    mainWindow.Left = 0;
                    mainWindow.Top = 0;
                    mainWindow.Width = screenWidth;
                    mainWindow.Height = screenHeight;

                    // Maximizar
                    mainWindow.WindowState = WindowState.Maximized;

                    _logService.Information("Ventana configurada: {Width}x{Height}", screenWidth, screenHeight);
                });

                // ✅ USAR API DE WINDOWS DESPUÉS DE QUE LA VENTANA ESTÉ CONFIGURADA
                mainWindow.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        var hwnd = new System.Windows.Interop.WindowInteropHelper(mainWindow).Handle;
                        if (hwnd != IntPtr.Zero)
                        {
                            var screenWidth = (int)SystemParameters.PrimaryScreenWidth;
                            var screenHeight = (int)SystemParameters.PrimaryScreenHeight;

                            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, screenWidth, screenHeight, SWP_NOMOVE | SWP_NOSIZE);
                            _logService.Information("API de Windows aplicada correctamente");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logService.Warning(ex, "Error al aplicar API de Windows, continuando...");
                    }
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al configurar ventana principal para Kiosk");
                throw;
            }
        }

        /// <summary>
        /// Oculta la barra de tareas de Windows
        /// </summary>
        private void HideTaskbar()
        {
            try
            {
                // Buscar y ocultar la barra de tareas principal
                IntPtr taskbarHandle = FindWindow("Shell_TrayWnd", null);
                if (taskbarHandle != IntPtr.Zero)
                {
                    ShowWindow(taskbarHandle, SW_HIDE);
                    _logService.Information("Barra de tareas ocultada");
                }
                else
                {
                    _logService.Warning("No se pudo encontrar la barra de tareas");
                }

                // También ocultar botón de inicio si existe
                IntPtr startButtonHandle = FindWindow("Button", null);
                if (startButtonHandle != IntPtr.Zero)
                {
                    ShowWindow(startButtonHandle, SW_HIDE);
                }
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al ocultar barra de tareas");
            }
        }

        /// <summary>
        /// Deshabilita el Task Manager a través del registro
        /// </summary>
        private void DisableTaskManager()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Policies\System"))
                {
                    key?.SetValue("DisableTaskMgr", 1, RegistryValueKind.DWord);
                }
                _logService.Information("Task Manager deshabilitado");
            }
            catch (Exception ex)
            {
                _logService.Warning(ex, "No se pudo deshabilitar Task Manager (requiere permisos)");
            }
        }

        /// <summary>
        /// Configura el interceptor de teclas peligrosas
        /// </summary>
        private void SetupKeyInterceptor(Window mainWindow)
        {
            try
            {
                // Interceptor principal en la ventana
                mainWindow.KeyDown += MainWindow_KeyDown;
                mainWindow.PreviewKeyDown += MainWindow_PreviewKeyDown;

                _logService.Information("Interceptor de teclas configurado");
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al configurar interceptor de teclas");
            }
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                // Bloquear combinaciones de teclas peligrosas
                if (ShouldBlockKey(e))
                {
                    e.Handled = true;
                    _logService.Debug("Combinación de teclas bloqueada: {Key} con modificadores: {Modifiers}",
                        e.Key, e.KeyboardDevice.Modifiers);
                    return;
                }

                // Verificar salida de emergencia: Ctrl+Shift+Alt+F12
                if (IsEmergencyExit(e))
                {
                    HandleEmergencyExit();
                }
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error en interceptor de teclas KeyDown");
            }
        }

        /// <summary>
        /// Maneja el evento PreviewKeyDown de la ventana principal
        /// </summary>
        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (ShouldBlockKey(e))
                {
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error en interceptor de teclas PreviewKeyDown");
            }
        }

        /// <summary>
        /// Determina si una combinación de teclas debe ser bloqueada
        /// </summary>
        private bool ShouldBlockKey(KeyEventArgs e)
        {
            var modifiers = e.KeyboardDevice.Modifiers;

            // Bloquear Alt+Tab, Alt+F4
            if (modifiers.HasFlag(ModifierKeys.Alt))
            {
                if (e.Key == Key.Tab || e.Key == Key.F4)
                    return true;
            }

            // Bloquear tecla Windows
            if (modifiers.HasFlag(ModifierKeys.Windows))
                return true;

            // Bloquear algunas combinaciones con Ctrl
            if (modifiers.HasFlag(ModifierKeys.Control))
            {
                if (e.Key == Key.Escape)
                    return true;
            }

            // Bloquear Ctrl+Shift+Esc (Task Manager)
            if (modifiers.HasFlag(ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.Escape)
                return true;

            return false;
        }

        /// <summary>
        /// Verifica si se presionó la combinación de salida de emergencia
        /// </summary>
        private bool IsEmergencyExit(KeyEventArgs e)
        {
            return e.KeyboardDevice.Modifiers.HasFlag(ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt)
                   && e.Key == Key.F12;
        }

        /// <summary>
        /// Maneja la salida de emergencia del modo Kiosk
        /// </summary>
        private void HandleEmergencyExit()
        {
            var result = MessageBox.Show(
                "¿Desea salir del modo Kiosk?\n\n" +
                "Esta acción cerrará la aplicación y restaurará el sistema normal.\n\n" +
                "ADVERTENCIA: Use solo en caso de emergencia.",
                "Salida de Emergencia - Sistema de Etiquetado",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _logService.Warning("Salida de emergencia activada por el usuario");
                DisableKioskMode();
                Application.Current.Shutdown();
            }
        }

        /// <summary>
        /// Inicia el watchdog para mantener la ventana siempre activa
        /// </summary>
        private void StartWatchdog()
        {
            try
            {
                _watchdogTimer = new System.Windows.Threading.DispatcherTimer();
                _watchdogTimer.Interval = TimeSpan.FromMilliseconds(500);
                _watchdogTimer.Tick += WatchdogTimer_Tick;
                _watchdogTimer.Start();

                _logService.Information("Watchdog iniciado - intervalo: 500ms");
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al iniciar watchdog");
            }
        }

        /// <summary>
        /// Tick del watchdog - verifica que nuestra ventana esté activa
        /// </summary>
        private void WatchdogTimer_Tick(object sender, EventArgs e)
        {
            if (!_kioskModeEnabled || _mainWindow == null)
                return;

            try
            {
                // Asegurar que nuestra ventana esté siempre al frente
                IntPtr foregroundWindow = GetForegroundWindow();
                IntPtr ourWindow = new System.Windows.Interop.WindowInteropHelper(_mainWindow).Handle;

                if (foregroundWindow != ourWindow)
                {
                    SetForegroundWindow(ourWindow);
                    _mainWindow.Activate();
                    _mainWindow.Focus();
                }

                // Asegurar que siga siendo topmost
                _mainWindow.Topmost = true;
            }
            catch (Exception ex)
            {
                _logService.Debug(ex, "Error menor en watchdog timer");
            }
        }

        /// <summary>
        /// Deshabilita el modo Kiosk y restaura el sistema normal
        /// </summary>
        public void DisableKioskMode()
        {
            try
            {
                _kioskModeEnabled = false;
                _logService.Information("Desactivando modo Kiosk");

                // Detener watchdog
                _watchdogTimer?.Stop();
                _watchdogTimer = null;

                // Restaurar barra de tareas
                RestoreTaskbar();

                // Rehabilitar Task Manager
                EnableTaskManager();

                // Restaurar ventana a modo normal
                if (_mainWindow != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _mainWindow.Topmost = false;
                        _mainWindow.WindowStyle = WindowStyle.SingleBorderWindow;
                        _mainWindow.WindowState = WindowState.Normal;
                        _mainWindow.ResizeMode = ResizeMode.CanResize;
                    });
                }

                _logService.Information("Modo Kiosk desactivado correctamente");
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al desactivar modo Kiosk");
            }
        }

        /// <summary>
        /// Restaura la barra de tareas de Windows
        /// </summary>
        private void RestoreTaskbar()
        {
            try
            {
                IntPtr taskbarHandle = FindWindow("Shell_TrayWnd", null);
                if (taskbarHandle != IntPtr.Zero)
                {
                    ShowWindow(taskbarHandle, SW_SHOW);
                }
                _logService.Information("Barra de tareas restaurada");
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al restaurar barra de tareas");
            }
        }

        /// <summary>
        /// Rehabilita el Task Manager
        /// </summary>
        private void EnableTaskManager()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Policies\System", true))
                {
                    key?.DeleteValue("DisableTaskMgr", false);
                }
                _logService.Information("Task Manager rehabilitado");
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al rehabilitar Task Manager");
            }
        }

        /// <summary>
        /// Propiedad que indica si el modo Kiosk está activo
        /// </summary>
        public bool IsKioskModeEnabled => _kioskModeEnabled;
    }
}