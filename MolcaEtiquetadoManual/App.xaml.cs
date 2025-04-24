using System.Configuration;
using System.Data;
using System.Windows;

using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using MolcaEtiquetadoManual.Core.Interfaces;
using MolcaEtiquetadoManual.Core.Services;
using MolcaEtiquetadoManual.UI.Views;
using MolcaEtiquetadoManual.Data.Context;
using Microsoft.EntityFrameworkCore;
using MolcaEtiquetadoManual.Data;

namespace MolcaEtiquetadoManual
{
    public partial class App : Application
    {
        private ServiceProvider serviceProvider;

        public App()
        {
            ServiceCollection services = new ServiceCollection();
            ConfigureServices(services);
            serviceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(ServiceCollection services)
        {
            // Configurar la conexión a la base de datos (ajusta la cadena de conexión según tu entorno)
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer("Server=.;Database=EtiquetadoManual;Trusted_Connection=True;"));

            // Registrar repositorios
            services.AddTransient<Data.Repositories.UsuarioRepository>();
            services.AddTransient<Data.Repositories.EtiquetadoRepository>();

            // Registrar servicios
         
            services.AddTransient<IUsuarioService, UsuarioService>();
            services.AddTransient<IEtiquetadoService, EtiquetadoService>();
            //services.AddTransient<IPrintService, ZebraPrintService>();
            services.AddTransient<LoginWindow>();
            // Registrar el servicio de impresión Zebra
            // Ajusta la IP y el puerto según tu impresora
            services.AddSingleton<IPrintService>(new ZebraPrintService("192.168.1.100", 9100));

            // Registrar ventanas
            services.AddTransient<LoginWindow>();
            services.AddTransient<MainWindow>();
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
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al inicializar la base de datos: {ex.Message}");
                }
            }

            var loginWindow = serviceProvider.GetRequiredService<LoginWindow>();
            loginWindow.Show();
        }
    }
}