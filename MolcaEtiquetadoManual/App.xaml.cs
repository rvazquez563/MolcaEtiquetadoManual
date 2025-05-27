using System;
using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MolcaEtiquetadoManual.Core.Interfaces;
using MolcaEtiquetadoManual.Core.Services;
using MolcaEtiquetadoManual.Data;
using MolcaEtiquetadoManual.Data.Context;
using MolcaEtiquetadoManual.Data.Repositories;
using MolcaEtiquetadoManual.UI.Views;
using Serilog;

namespace MolcaEtiquetadoManual
{
    public partial class App : Application
    {

        private ServiceProvider serviceProvider;
        private KioskManager _kioskManager;           // ← NUEVA LÍNEA
        private bool _kioskModeEnabled;               // ← NUEVA LÍNEA

        public static IConfiguration Configuration { get; private set; }
        public App()
        {
            // Configurar acceso al archivo de configuración
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            Configuration = builder.Build();

            // Configurar Serilog
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .CreateLogger();

            try
            {
                Log.Information("Iniciando aplicación");

                // Configurar servicios
                ServiceCollection services = new ServiceCollection();
                ConfigureServices(services);
                serviceProvider = services.BuildServiceProvider();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "La aplicación falló al iniciar");
            }
        }

        private void ConfigureServices(ServiceCollection services)
        {
            // Agregar Serilog a los servicios
            services.AddLogging(loggingBuilder =>
                loggingBuilder.AddSerilog(dispose: true));

            // Registrar servicio de logging
            services.AddSingleton<ILogService, LogService>();

            services.AddSingleton(Configuration);

            // Registrar servicio de fechas julianas
            services.AddSingleton<IJulianDateService, JulianDateService>();

            // Obtener la cadena de conexión desde la configuración
            string connectionString = Configuration.GetConnectionString("DefaultConnection");

            // Configurar DbContext
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString));

            // Registrar repositorios
            services.AddTransient<UsuarioRepository>();
            services.AddTransient<EtiquetadoRepository>();
            services.AddTransient<LineaProduccionRepository>();
            // Registrar servicios
            services.AddTransient<IUsuarioService, UsuarioService>();
            services.AddTransient<IEtiquetadoService, EtiquetadoService>();
            services.AddTransient<ITurnoService, TurnoService>();
            services.AddTransient<ILineaProduccionService, LineaProduccionService>();
            // Registrar servicio de códigos de barras
            services.AddSingleton<IBarcodeService, ZXingBarcodeService>();

            // Registrar servicio de vista previa de etiquetas
            services.AddSingleton<IEtiquetaPreviewService, EtiquetaPreviewService>();

            // Configurar servicio de impresión
            var useMockPrinter = Configuration.GetSection("PrinterSettings").GetValue<bool>("UseMockPrinter");

                if (_kioskModeEnabled)
            {
                // Crear el KioskManager aquí para asegurar que esté disponible
                services.AddSingleton<KioskManager>(sp => 
                {
                    var logService = sp.GetRequiredService<ILogService>();
                    return new KioskManager(logService);
                });
            }


            if (useMockPrinter)
            {
                // Usar la versión de prueba del servicio de impresión
                services.AddSingleton<IPrintService>(sp =>
                    new TestPrintService(sp.GetRequiredService<ILogService>(), Configuration));
                Log.Information("Utilizando servicio de impresión para PRUEBAS");
            }
            else
            {
                // Usar el servicio real de impresión
                services.AddSingleton<IPrintService>(sp =>
                    new ZebraPrintService(Configuration, sp.GetRequiredService<ILogService>()));
                Log.Information("Utilizando servicio de impresión REAL para Zebra");
            }

            // Registrar ventanas
            services.AddTransient<LoginWindow>();
            services.AddTransient<MainWindow>();
            services.AddTransient<UserManagementWindow>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            // Inicializar la base de datos con datos
            using (var scope = serviceProvider.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<AppDbContext>();
                    DbInitializer.Initialize(context);
                    Log.Information("Base de datos inicializada correctamente");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al inicializar la base de datos");
                    MessageBox.Show($"Error al inicializar la base de datos: {ex.Message}");
                }
            }

            var loginWindow = serviceProvider.GetRequiredService<LoginWindow>();

            
            //// Si el modo Kiosk está habilitado, configurar la ventana
            //if (_kioskModeEnabled && _kioskManager != null)
            //{
            //    loginWindow.Loaded += LoginWindow_Loaded;
            //    Log.Information("Ventana de login configurada para modo Kiosk");
            //}

            loginWindow.Show();
        }
        private void LoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (_kioskManager != null && sender is Window window)
            {
                try
                {
                    _kioskManager.EnableKioskMode(window);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error al activar modo Kiosk en ventana de login");
                }
            }
        }

        // Manejar excepciones no controladas
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Fatal((Exception)e.ExceptionObject, "Excepción no controlada en el dominio de aplicación");

            if (_kioskManager != null)
            {
                _kioskManager.DisableKioskMode();
            }
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            Log.Error(e.Exception, "Excepción no controlada en el dispatcher");

            // Manejar la excepción para evitar que la aplicación se cierre
            e.Handled = true;

            MessageBox.Show($"Ha ocurrido un error inesperado:\n{e.Exception.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                Log.Information("Cerrando aplicación");

                // AGREGAR ESTAS LÍNEAS:
                // Deshabilitar modo Kiosk si estaba activo
                if (_kioskModeEnabled && _kioskManager != null)
                {
                    _kioskManager.DisableKioskMode();
                    Log.Information("Modo Kiosk deshabilitado al cerrar aplicación");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error al limpiar recursos durante el cierre");
            }
            finally
            {
                Log.CloseAndFlush();
                serviceProvider?.Dispose();        // ← AGREGAR ESTA LÍNEA
                base.OnExit(e);
            }
        }
    }
}