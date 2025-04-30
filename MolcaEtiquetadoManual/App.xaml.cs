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
        // Fragmento de ConfigureServices en App.xaml.cs
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

            // Registrar servicio de códigos de barras
            services.AddSingleton<IBarcodeService, ZXingBarcodeService>();

            // Obtener la cadena de conexión desde la configuración
            string connectionString = Configuration.GetConnectionString("DefaultConnection");

            // Configurar DbContext
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString));

            // Registrar repositorios
            services.AddTransient<UsuarioRepository>();
            services.AddTransient<EtiquetadoRepository>();

            // Registrar servicios
            services.AddTransient<IUsuarioService, UsuarioService>();
            services.AddTransient<IEtiquetadoService, EtiquetadoService>();
            services.AddTransient<ITurnoService, TurnoService>();

            // Configurar servicio de impresión
            var useMockPrinter = Configuration.GetSection("PrinterSettings").GetValue<bool>("UseMockPrinter");

            if (useMockPrinter)
            {
                // Usar la versión de prueba del servicio de impresión
                services.AddSingleton<IPrintService, TestPrintService>();
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
            loginWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information("Cerrando aplicación");
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}