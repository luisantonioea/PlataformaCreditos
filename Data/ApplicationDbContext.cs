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

    // Fuerza a PostgreSQL a usar el tipo BOOLEAN correcto en lugar de INTEGER
    builder.Entity<Microsoft.AspNetCore.Identity.IdentityUser>().Property(u => u.EmailConfirmed).HasColumnType("boolean");
    builder.Entity<Microsoft.AspNetCore.Identity.IdentityUser>().Property(u => u.PhoneNumberConfirmed).HasColumnType("boolean");
    builder.Entity<Microsoft.AspNetCore.Identity.IdentityUser>().Property(u => u.TwoFactorEnabled).HasColumnType("boolean");
    builder.Entity<Microsoft.AspNetCore.Identity.IdentityUser>().Property(u => u.LockoutEnabled).HasColumnType("boolean");
}
}