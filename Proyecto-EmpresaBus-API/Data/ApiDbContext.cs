using Proyecto_EmpresaBus_API.Models;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_EmpresaBus_API.Data;
public class ApiDbContext : DbContext
{
    public ApiDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<Empresa> Empresas { get; set; }
    public DbSet<Autobus> Autobuses { get; set; }
    public DbSet<Asiento> Asientos { get; set; }
    public DbSet<Provincia> Provincias { get; set; }
    public DbSet<Localidad> Localidades { get; set; }
    public DbSet<Parada> Paradas { get; set; }
    public DbSet<Ruta> Rutas { get; set; }
    public DbSet<RutaParada> RutaParadas { get; set; }
    public DbSet<Viaje> Viajes { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Boleto> Boletos { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<RutaParada>()
            .HasKey(rp => new { rp.RutaID, rp.ParadaID });


        modelBuilder.Entity<Asiento>()
            .HasIndex(a => new { a.AutobusID, a.NumeroAsiento }).IsUnique();


        modelBuilder.Entity<Boleto>()
            .HasIndex(b => new { b.ViajeID, b.AsientoID }).IsUnique();


        modelBuilder.Entity<Ruta>()
            .HasOne(r => r.Origen)
            .WithMany()
            .HasForeignKey(r => r.OrigenID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Ruta>()
            .HasOne(r => r.Destino)
            .WithMany()
            .HasForeignKey(r => r.DestinoID)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Autobus>().HasQueryFilter(a => !a.IsDeleted);
        modelBuilder.Entity<Viaje>().HasQueryFilter(v => !v.IsDeleted);
        modelBuilder.Entity<Usuario>().HasQueryFilter(u => !u.IsDeleted);

        modelBuilder.Entity<Usuario>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<Usuario>().HasIndex(u => u.DNI).IsUnique();
        modelBuilder.Entity<Usuario>().HasIndex(u => u.Telefono).IsUnique();
    }
}