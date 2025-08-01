using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using MolcaEtiquetadoManual.Core.Interfaces;
using System.Linq;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Windows.Media;

namespace MolcaEtiquetadoManual.Core.Services
{
    public class KioskManager
    {
        private readonly ILogService _logService;
        private bool _kioskModeEnabled = false;
        private System.Windows.Threading.DispatcherTimer _watchdogTimer;
        private string _kioskPassword = "";
        private readonly ProcessBlocker _processBlocker;

        // ✅ NUEVO: Variables para el hook del teclado a nivel de sistema
        private LowLevelKeyboardProc _keyboardProc = null;
        private IntPtr _keyboardHookID = IntPtr.Zero;

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

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // ✅ NUEVO: APIs para hook de teclado a nivel de sistema
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn,
            IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        // ✅ NUEVO: Constantes adicionales para el hook
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        // ✅ NUEVO: Códigos de teclas virtuales
        private const int VK_LWIN = 0x5B;
        private const int VK_RWIN = 0x5C;
        private const int VK_TAB = 0x09;
        private const int VK_MENU = 0x12; // Alt key
        private const int VK_ESCAPE = 0x1B;
        private const int VK_CONTROL = 0x11;

        // Constantes existentes
        private const int SW_HIDE = 0;
        private const int SW_SHOW = 1;
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;

        // Constantes para hotkeys
        private const int HOTKEY_ID_ALT_F4 = 9001;
        private const int HOTKEY_ID_CTRL_ALT_DEL = 9002;
        private const int HOTKEY_ID_EMERGENCY_EXIT = 9003;
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CTRL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint VK_F4 = 0x73;
        private const uint VK_DELETE = 0x2E;
        private const uint VK_F12 = 0x7B;

        private Window _mainWindow;
        private HwndSource _hwndSource;

        // ✅ NUEVO: Delegate para el hook del teclado
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        public KioskManager(ILogService logService, ProcessBlocker processBlocker = null)
        {
            _logService = logService;
            _processBlocker = processBlocker;
            GenerarContraseñaKiosk();

            // ✅ NUEVO: Inicializar el procedimiento del hook
            _keyboardProc = HookCallback;
        }

        private void GenerarContraseñaKiosk()
        {
            _kioskPassword = DateTime.Now.ToString("ddMMyy");
            _logService.Information("Contraseña de modo Kiosk generada para fecha {Date}: {Password}",
                DateTime.Now.ToString("dd/MM/yyyy"), _kioskPassword);
        }

        public void EnableKioskMode(Window mainWindow)
        {
            try
            {
                _mainWindow = mainWindow;
                _kioskModeEnabled = true;

                _logService.Information("=== INICIANDO ACTIVACIÓN DE MODO KIOSK ===");

                ConfigureMainWindow(mainWindow);
                RegisterSystemHotKeys();
                ConfigureWindowClosingEvents();
                HideTaskbar();
                DisableTaskManager();
                SetupKeyInterceptor(mainWindow);

                // Instalar hook de teclado a nivel de sistema
                InstallSystemKeyboardHook();

                // ✅ NUEVO: Habilitar bloqueador de procesos
                if (_processBlocker != null)
                {
                    _processBlocker.EnableProcessBlocking();
                    _logService.Information("Bloqueador de procesos activado");
                }
                else
                {
                    _logService.Warning("ProcessBlocker no disponible - continuando sin bloqueo de procesos");
                }

                StartWatchdog();

                _logService.Information("=== MODO KIOSK ACTIVADO EXITOSAMENTE ===");
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "ERROR CRÍTICO al activar modo Kiosk");
                throw;
            }
        }

        public void DisableKioskMode()
        {
            try
            {
                _kioskModeEnabled = false;
                _logService.Information("Desactivando modo Kiosk");

                _watchdogTimer?.Stop();
                _watchdogTimer = null;

                // ✅ NUEVO: Deshabilitar bloqueador de procesos
                if (_processBlocker != null)
                {
                    _processBlocker.DisableProcessBlocking();
                    _logService.Information("Bloqueador de procesos desactivado");
                }

                // Desinstalar hook de teclado
                UninstallSystemKeyboardHook();

                UnregisterSystemHotKeys();
                RemoveEventHooks();
                RestoreTaskbar();
                EnableTaskManager();

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

        // ✅ NUEVO: Método para instalar hook de teclado a nivel de sistema
        private void InstallSystemKeyboardHook()
        {
            try
            {
                _keyboardHookID = SetHook(_keyboardProc);
                if (_keyboardHookID != IntPtr.Zero)
                {
                    _logService.Information("Hook de teclado a nivel de sistema instalado exitosamente");
                }
                else
                {
                    _logService.Warning("No se pudo instalar el hook de teclado a nivel de sistema");
                }
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al instalar hook de teclado a nivel de sistema");
            }
        }

        // ✅ NUEVO: Configurar el hook
        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        // ✅ NUEVO: Callback del hook para interceptar teclas a nivel de sistema
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (nCode >= 0 && _kioskModeEnabled && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
                {
                    int vkCode = Marshal.ReadInt32(lParam);

                    // ✅ MEJORADO: Bloqueo más agresivo de teclas del sistema
                    if (ShouldBlockSystemKey(vkCode))
                    {
                        _logService.Debug("Tecla del sistema bloqueada por hook: {VirtualKey}", vkCode);
                        return (IntPtr)1; // Bloquear la tecla
                    }

                    // ✅ NUEVO: Bloquear combinaciones específicas
                    if (IsBlockedCombination(vkCode))
                    {
                        _logService.Debug("Combinación de teclas bloqueada por hook: {VirtualKey}", vkCode);
                        return (IntPtr)1; // Bloquear la combinación
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error en hook callback");
            }

            return CallNextHookEx(_keyboardHookID, nCode, wParam, lParam);
        }

        // ✅ NUEVO: Determinar si una tecla del sistema debe ser bloqueada
        private bool ShouldBlockSystemKey(int vkCode)
        {
            switch (vkCode)
            {
                case VK_LWIN:       // Tecla Windows izquierda
                case VK_RWIN:       // Tecla Windows derecha
                    return true;

                case VK_TAB:        // Tab (solo si Alt está presionado)
                    return IsKeyPressed(VK_MENU); // Alt + Tab

                case VK_ESCAPE:     // Escape (solo si Ctrl está presionado)
                    return IsKeyPressed(VK_CONTROL); // Ctrl + Esc

                default:
                    return false;
            }
        }

        // ✅ NUEVO: Verificar si una tecla está presionada
        private bool IsKeyPressed(int vkCode)
        {
            return (GetKeyState(vkCode) & 0x8000) != 0;
        }

        // ✅ NUEVO: Verificar combinaciones específicas que deben ser bloqueadas
        private bool IsBlockedCombination(int vkCode)
        {
            // Bloquear Ctrl+Alt+Del
            if (vkCode == VK_DELETE && IsKeyPressed(VK_CONTROL) && IsKeyPressed(VK_MENU))
            {
                return true;
            }
            if (vkCode ==  (int)VK_F4 && IsKeyPressed(VK_MENU))
            {
                return true;
            }
            // Bloquear Ctrl+Shift+Esc (Task Manager)
            if (vkCode == VK_ESCAPE && IsKeyPressed(VK_CONTROL) && IsKeyPressed(0x10)) // 0x10 = VK_SHIFT
            {
                return true;
            }

            // Permitir Ctrl+Shift+Alt+F12 para salida de emergencia
            if (vkCode == (int)VK_F12 && IsKeyPressed(VK_CONTROL) &&
                IsKeyPressed(0x10) && IsKeyPressed(VK_MENU))
            {
                // Esta combinación está permitida para salida de emergencia
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    SolicitarContraseñaParaSalir("Salida de emergencia (Ctrl+Shift+Alt+F12)");
                }));
                return true; // Bloquear para que no llegue a la aplicación
            }

            return false;
        }

        // ✅ MEJORADO: Método ShouldBlockKey actualizado para trabajar con el hook
        private bool ShouldBlockKey(KeyEventArgs e)
        {
            var modifiers = e.KeyboardDevice.Modifiers;

            // Alt+F4
            if (modifiers.HasFlag(ModifierKeys.Alt) && e.Key == Key.F4)
                return true;

            // Alt+Tab
            if (modifiers.HasFlag(ModifierKeys.Alt) && e.Key == Key.Tab)
                return true;

            // Tecla Windows (cualquier combinación)
            if (modifiers.HasFlag(ModifierKeys.Windows))
                return true;

            // Ctrl+Escape
            if (modifiers.HasFlag(ModifierKeys.Control) && e.Key == Key.Escape)
                return true;

            // Ctrl+Shift+Escape
            if (modifiers.HasFlag(ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.Escape)
                return true;

            // Ctrl+Alt+Delete
            if (modifiers.HasFlag(ModifierKeys.Control | ModifierKeys.Alt) && e.Key == Key.Delete)
                return true;

            return false;
        }

        // Resto de métodos existentes sin cambios...
        private void RegisterSystemHotKeys()
        {
            try
            {
                var hwnd = new WindowInteropHelper(_mainWindow).Handle;
                if (hwnd != IntPtr.Zero)
                {
                    RegisterHotKey(hwnd, HOTKEY_ID_ALT_F4, MOD_ALT, VK_F4);
                    RegisterHotKey(hwnd, HOTKEY_ID_CTRL_ALT_DEL, MOD_CTRL | MOD_ALT, VK_DELETE);
                    RegisterHotKey(hwnd, HOTKEY_ID_EMERGENCY_EXIT, MOD_CTRL | MOD_SHIFT | MOD_ALT, VK_F12);

                    _logService.Information("Hotkeys de sistema registrados");
                }
            }
            catch (Exception ex)
            {
                _logService.Warning(ex, "No se pudieron registrar todos los hotkeys de sistema");
            }
        }

        private void ConfigureWindowClosingEvents()
        {
            if (_mainWindow != null)
            {
                _mainWindow.Closing += MainWindow_Closing;

                if (_hwndSource == null)
                {
                    var hwnd = new WindowInteropHelper(_mainWindow).Handle;
                    if (hwnd != IntPtr.Zero)
                    {
                        _hwndSource = HwndSource.FromHwnd(hwnd);
                        _hwndSource.AddHook(WndProc);
                    }
                }
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            const int WM_SYSCOMMAND = 0x0112;
            const int SC_CLOSE = 0xF060;

            switch (msg)
            {
                case WM_HOTKEY:
                    int hotkeyId = wParam.ToInt32();

                    if (hotkeyId == HOTKEY_ID_ALT_F4)
                    {
                        _logService.Warning("Alt+F4 interceptado y BLOQUEADO completamente");
                        handled = true;
                    }
                    else if (hotkeyId == HOTKEY_ID_EMERGENCY_EXIT)
                    {
                        _logService.Warning("Ctrl+Shift+Alt+F12 interceptado - solicitando contraseña");
                        handled = true;
                        SolicitarContraseñaParaSalir("Salida de emergencia (Ctrl+Shift+Alt+F12)");
                    }
                    break;

                case WM_SYSCOMMAND:
                    int command = wParam.ToInt32() & 0xFFF0;
                    if (command == SC_CLOSE)
                    {
                        _logService.Warning("Comando de cierre de sistema interceptado - BLOQUEADO");
                        handled = true;
                    }
                    break;
            }

            return IntPtr.Zero;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_kioskModeEnabled)
            {
                _logService.Warning("Intento de cierre de ventana en modo Kiosk detectado - BLOQUEADO");
                e.Cancel = true;
            }
        }

        private void SolicitarContraseñaParaSalir(string motivo)
        {
            try
            {
                _logService.Warning("Solicitando contraseña para salir de modo Kiosk. Motivo: {Motivo}", motivo);

                var passwordDialog = new KioskPasswordDialog(_kioskPassword, _mainWindow);
                bool? result = passwordDialog.ShowDialog();

                if (result == true)
                {
                    _logService.Warning("Contraseña correcta ingresada - deshabilitando modo Kiosk");

                    var confirmResult = MessageBox.Show(
                        "¿Está seguro que desea salir del modo Kiosk?\n\nEsta acción cerrará la aplicación.",
                        "Confirmar Salida de Modo Kiosk",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (confirmResult == MessageBoxResult.Yes)
                    {
                        DisableKioskMode();
                        Application.Current.Shutdown();
                    }
                }
                else
                {
                    _logService.Warning("Contraseña incorrecta o diálogo cancelado - manteniendo modo Kiosk");
                }
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al solicitar contraseña de modo Kiosk");
            }
        }

        // Resto de métodos existentes...
        private void ConfigureMainWindow(Window mainWindow)
        {
            try
            {
                _logService.Information("Configurando ventana para modo Kiosk...");

                Application.Current.Dispatcher.Invoke(() =>
                {
                    mainWindow.WindowStyle = WindowStyle.None;
                    mainWindow.ResizeMode = ResizeMode.NoResize;
                    mainWindow.Topmost = true;

                    var screenWidth = SystemParameters.PrimaryScreenWidth;
                    var screenHeight = SystemParameters.PrimaryScreenHeight;

                    mainWindow.Left = 0;
                    mainWindow.Top = 0;
                    mainWindow.Width = screenWidth;
                    mainWindow.Height = screenHeight;
                    mainWindow.WindowState = WindowState.Maximized;

                    _logService.Information("Ventana configurada: {Width}x{Height}", screenWidth, screenHeight);
                });

                mainWindow.Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        var hwnd = new WindowInteropHelper(mainWindow).Handle;
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

        private void HideTaskbar()
        {
            try
            {
                IntPtr taskbarHandle = FindWindow("Shell_TrayWnd", null);
                if (taskbarHandle != IntPtr.Zero)
                {
                    ShowWindow(taskbarHandle, SW_HIDE);
                    _logService.Information("Barra de tareas ocultada");
                }

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

        private void SetupKeyInterceptor(Window mainWindow)
        {
            try
            {
                mainWindow.KeyDown += MainWindow_KeyDown;
                mainWindow.PreviewKeyDown += MainWindow_PreviewKeyDown;

                if (Application.Current.MainWindow != null)
                {
                    Application.Current.MainWindow.KeyDown += MainWindow_KeyDown;
                    Application.Current.MainWindow.PreviewKeyDown += MainWindow_PreviewKeyDown;
                }

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
                if (ShouldBlockKey(e))
                {
                    e.Handled = true;
                    _logService.Debug("Combinación de teclas bloqueada: {Key} con modificadores: {Modifiers}",
                        e.Key, e.KeyboardDevice.Modifiers);
                }
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error en interceptor de teclas KeyDown");
            }
        }

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

        private void WatchdogTimer_Tick(object sender, EventArgs e)
        {
            if (!_kioskModeEnabled || _mainWindow == null)
                return;

            try
            {
                IntPtr foregroundWindow = GetForegroundWindow();
                IntPtr ourWindow = new WindowInteropHelper(_mainWindow).Handle;

                if (foregroundWindow != ourWindow && !IsOwnedByOurApplication(foregroundWindow))
                {
                    SetForegroundWindow(ourWindow);
                    _mainWindow.Activate();
                    _mainWindow.Focus();
                }

                _mainWindow.Topmost = true;
            }
            catch (Exception ex)
            {
                _logService.Debug(ex, "Error menor en watchdog timer");
            }
        }

        private bool IsOwnedByOurApplication(IntPtr windowHandle)
        {
            try
            {
                GetWindowThreadProcessId(windowHandle, out uint processId);
                uint ourProcessId = (uint)Process.GetCurrentProcess().Id;
                return processId == ourProcessId;
            }
            catch
            {
                return false;
            }
        }

       
        // ✅ NUEVO: Desinstalar hook de teclado
        private void UninstallSystemKeyboardHook()
        {
            try
            {
                if (_keyboardHookID != IntPtr.Zero)
                {
                    UnhookWindowsHookEx(_keyboardHookID);
                    _keyboardHookID = IntPtr.Zero;
                    _logService.Information("Hook de teclado a nivel de sistema desinstalado");
                }
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al desinstalar hook de teclado");
            }
        }

        private void UnregisterSystemHotKeys()
        {
            try
            {
                if (_mainWindow != null)
                {
                    var hwnd = new WindowInteropHelper(_mainWindow).Handle;
                    if (hwnd != IntPtr.Zero)
                    {
                        UnregisterHotKey(hwnd, HOTKEY_ID_ALT_F4);
                        UnregisterHotKey(hwnd, HOTKEY_ID_CTRL_ALT_DEL);
                        UnregisterHotKey(hwnd, HOTKEY_ID_EMERGENCY_EXIT);
                    }
                }
            }
            catch (Exception ex)
            {
                _logService.Warning(ex, "Error al desregistrar hotkeys de sistema");
            }
        }

        private void RemoveEventHooks()
        {
            try
            {
                if (_mainWindow != null)
                {
                    _mainWindow.Closing -= MainWindow_Closing;
                    _mainWindow.KeyDown -= MainWindow_KeyDown;
                    _mainWindow.PreviewKeyDown -= MainWindow_PreviewKeyDown;
                }

                if (_hwndSource != null)
                {
                    _hwndSource.RemoveHook(WndProc);
                    _hwndSource = null;
                }
            }
            catch (Exception ex)
            {
                _logService.Warning(ex, "Error al remover event hooks");
            }
        }

        private void RestoreTaskbar()
        {
            try
            {
                IntPtr taskbarHandle = FindWindow("Shell_TrayWnd", null);
                if (taskbarHandle != IntPtr.Zero)
                {
                    ShowWindow(taskbarHandle, SW_SHOW);
                }
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al restaurar barra de tareas");
            }
        }

        private void EnableTaskManager()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Policies\System", true))
                {
                    key?.DeleteValue("DisableTaskMgr", false);
                }
            }
            catch (Exception ex)
            {
                _logService.Error(ex, "Error al rehabilitar Task Manager");
            }
        }

        public bool IsKioskModeEnabled => _kioskModeEnabled;
        public string GetCurrentKioskPassword() => _kioskPassword;
    }

    // KioskPasswordDialog permanece igual...
    public partial class KioskPasswordDialog : Window
    {
        private readonly string _correctPassword;
        private readonly Window _parentWindow;
        private System.Windows.Controls.PasswordBox _passwordBox;
        private bool _isClosing = false;
        private bool _buttonClicked = false;

        public KioskPasswordDialog(string correctPassword, Window parentWindow)
        {
            _correctPassword = correctPassword;
            _parentWindow = parentWindow;
            InitializeComponent();

            this.Title = "Salir de Modo Kiosk";
            this.Width = 450;
            this.Height = 280;
            this.WindowStyle = WindowStyle.ToolWindow;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.ResizeMode = ResizeMode.NoResize;
            this.Topmost = true;
            this.ShowInTaskbar = false;
            this.Owner = parentWindow;

            this.Loaded += (s, e) => _passwordBox?.Focus();
            this.Closing += (s, e) => { _isClosing = true; };
        }

        private void InitializeComponent()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });

            var title = new System.Windows.Controls.TextBlock
            {
                Text = "🔒 MODO KIOSK ACTIVADO\n\nIngrese la contraseña para salir del modo Kiosk:",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(20, 20, 20, 15),
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center
            };
            Grid.SetRow(title, 0);
            grid.Children.Add(title);

            _passwordBox = new System.Windows.Controls.PasswordBox
            {
                Name = "PasswordBox",
                Margin = new Thickness(20, 10, 20, 10),
                FontSize = 18,
                Height = 35,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                HorizontalContentAlignment = HorizontalAlignment.Center
            };
            Grid.SetRow(_passwordBox, 1);
            grid.Children.Add(_passwordBox);

            var helpText = new System.Windows.Controls.TextBlock
            {
                Text = "📝 Ingrese la contraseña correcta para continuar",
                FontSize = 11,
                Foreground = System.Windows.Media.Brushes.DarkBlue,
                Margin = new Thickness(20, 5, 20, 10),
                TextAlignment = TextAlignment.Center,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 248, 255))
            };
            Grid.SetRow(helpText, 2);
            grid.Children.Add(helpText);

            var buttonPanel = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(20, 15, 20, 20)
            };

            var cancelButton = new System.Windows.Controls.Button
            {
                Content = "❌ Cancelar",
                Width = 100,
                Height = 35,
                Margin = new Thickness(0, 0, 15, 0),
                FontSize = 12
            };

            cancelButton.Click += (s, e) =>
            {
                _buttonClicked = true;
                this.DialogResult = false;
                this.Close();
            };

            var okButton = new System.Windows.Controls.Button
            {
                Content = "🔓 Salir",
                Width = 100,
                Height = 35,
                IsDefault = true,
                FontSize = 12,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 53, 69)),
                Foreground = System.Windows.Media.Brushes.White
            };

            okButton.Click += (s, e) =>
            {
                _buttonClicked = true;
                string password = _passwordBox.Password;
                ValidatePassword(password);
            };

            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(okButton);

            Grid.SetRow(buttonPanel, 3);
            grid.Children.Add(buttonPanel);

            this.Content = grid;

            _passwordBox.Loaded += (s, e) =>
            {
                _passwordBox.Focus();
            };

            _passwordBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    _buttonClicked = true;
                    string password = _passwordBox.Password;
                    ValidatePassword(password);
                }
                else if (e.Key == Key.Escape)
                {
                    _buttonClicked = true;
                    this.DialogResult = false;
                    this.Close();
                }
            };

            this.Activated += (s, e) =>
            {
                if (!_isClosing && !_buttonClicked)
                {
                    _passwordBox.Focus();
                }
            };
        }

        private void ValidatePassword(string enteredPassword)
        {
            try
            {
                if (string.IsNullOrEmpty(enteredPassword))
                {
                    ShowPasswordError("No se ingresó ninguna contraseña", enteredPassword);
                    return;
                }

                if (enteredPassword == _correctPassword)
                {
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    ShowPasswordError("Contraseña incorrecta", enteredPassword);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al validar contraseña: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                _passwordBox.Password = "";
                _buttonClicked = false;
                _passwordBox.Focus();
            }
        }

        private void ShowPasswordError(string mensaje, string enteredPassword)
        {
            var errorMsg = $"❌ {mensaje}\n\n" +
                          $"Ingresada: '{enteredPassword}'\n" +
                          $"Intente nuevamente.";

            MessageBox.Show(errorMsg, "Error de Autenticación",
                MessageBoxButton.OK, MessageBoxImage.Warning);

            _passwordBox.Password = "";
            _buttonClicked = false;
            _passwordBox.Focus();
        }

        protected override void OnClosed(EventArgs e)
        {
            _isClosing = true;
            base.OnClosed(e);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            this.Topmost = true;
            this.Activate();
            this.Focus();
        }
    }
}