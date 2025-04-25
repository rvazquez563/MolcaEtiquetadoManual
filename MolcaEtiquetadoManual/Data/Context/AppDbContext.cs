using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
// Data/Context/AppDbContext.cs
// Data/Context/AppDbContext.cs
using Microsoft.EntityFrameworkCore;
using MolcaEtiquetadoManual.Core.Models;

namespace MolcaEtiquetadoManual.Data.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<OrdenProduccion> OrdenesProduccion { get; set; }
        public DbSet<EtiquetaGenerada> EtiquetasGeneradas { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configuración de tablas
            modelBuilder.Entity<Usuario>().ToTable("Usuarios");
            modelBuilder.Entity<OrdenProduccion>().ToTable("ENTRADA");
            modelBuilder.Entity<EtiquetaGenerada>().ToTable("SALIDA"); // Cambiado a SALIDA

            // Configuración específica de columnas para la tabla ENTRADA
            modelBuilder.Entity<OrdenProduccion>(entity =>
            {
                entity.Property(e => e.IdLogico).HasColumnName("ID");
                entity.Property(e => e.Id).HasColumnName("ITM");
                entity.Property(e => e.NumeroArticulo).HasColumnName("LITM");
                entity.Property(e => e.Descripcion).HasColumnName("DSC1");                
                entity.Property(e => e.UnidadMedida).HasColumnName("UOM");
                entity.Property(e => e.ProgramaProduccion).HasColumnName("DOCO");
                entity.Property(e => e.DiasCaducidad).HasColumnName("SLD");
                entity.Property(e => e.DUN14).HasColumnName("CITM");
                entity.Property(e => e.FechaProduccionInicio).HasColumnName("STRT");
                entity.Property(e => e.FechaProduccionFin).HasColumnName("DRQJ");
                entity.Property(e => e.CantidadPorPallet).HasColumnName("MULT");


            });

            // Configuración específica de columnas para la tabla SALIDA
            modelBuilder.Entity<EtiquetaGenerada>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");
                entity.Property(e => e.EDUS).HasColumnName("EDUS");
                entity.Property(e => e.EDDT).HasColumnName("EDDT");
                entity.Property(e => e.EDTN).HasColumnName("EDTN");
                entity.Property(e => e.EDLN).HasColumnName("EDLN");
                entity.Property(e => e.DOCO).HasColumnName("DOCO");
                entity.Property(e => e.LITM).HasColumnName("LITM");
                entity.Property(e => e.SOQS).HasColumnName("SOQS");
                entity.Property(e => e.UOM1).HasColumnName("UOM");
                entity.Property(e => e.LOTN).HasColumnName("LOTN");
                entity.Property(e => e.EXPR).HasColumnName("EXPR");
                entity.Property(e => e.TDAY).HasColumnName("TDAY");
                entity.Property(e => e.SHFT).HasColumnName("SHFT");
                entity.Property(e => e.URDT).HasColumnName("URDT");
                entity.Property(e => e.SEC).HasColumnName("SEC");
                entity.Property(e => e.ESTADO).HasColumnName("ESTADO");
                entity.Property(e => e.URRF).HasColumnName("URRF");
            });
            // Configuración de clave primaria
            modelBuilder.Entity<Usuario>().HasKey(u => u.Id);
            modelBuilder.Entity<OrdenProduccion>().HasKey(o => o.IdLogico);
            modelBuilder.Entity<EtiquetaGenerada>().HasKey(e => e.Id);

            // Configuración de restricciones
            modelBuilder.Entity<Usuario>().Property(u => u.NombreUsuario).IsRequired().HasMaxLength(50);
            modelBuilder.Entity<Usuario>().Property(u => u.Contraseña).IsRequired().HasMaxLength(100);

            modelBuilder.Entity<OrdenProduccion>().Property(o => o.DUN14).IsRequired().HasMaxLength(15);
            modelBuilder.Entity<OrdenProduccion>().Property(o => o.NumeroArticulo).IsRequired().HasMaxLength(8);

            modelBuilder.Entity<EtiquetaGenerada>().Property(e => e.EDUS).IsRequired().HasMaxLength(4);
            modelBuilder.Entity<EtiquetaGenerada>().Property(e => e.DOCO).IsRequired().HasMaxLength(6);
        }
    }
}