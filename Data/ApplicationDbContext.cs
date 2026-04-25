using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PlataformaCreditos.Models;

namespace PlataformaCreditos.Data;

public class ApplicationDbContext : IdentityDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Cliente> Clientes { get; set; }
    public DbSet<SolicitudCredito> SolicitudesCredito { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // 1. Inyectar datos de clientes
        builder.Entity<Cliente>().HasData(
            new Cliente { Id = 1, UsuarioId = "user-mock-1", IngresosMensuales = 2000, Activo = true },
            new Cliente { Id = 2, UsuarioId = "user-mock-2", IngresosMensuales = 3500, Activo = true }
        );

        // 2. Inyectar datos de solicitudes (Una aprobada, una pendiente)
        builder.Entity<SolicitudCredito>().HasData(
            new SolicitudCredito { 
                Id = 1, 
                ClienteId = 1, 
                MontoSolicitado = 1500, 
                FechaSolicitud = new DateTime(2026, 4, 20), // Fecha estática
                Estado = EstadoSolicitud.Aprobado 
            },
            new SolicitudCredito { 
                Id = 2, 
                ClienteId = 2, 
                MontoSolicitado = 5000, 
                FechaSolicitud = new DateTime(2026, 4, 25), // Fecha estática
                Estado = EstadoSolicitud.Pendiente 
            }
        );
    }
}