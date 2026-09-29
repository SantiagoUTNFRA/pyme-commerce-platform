using Microsoft.EntityFrameworkCore;
using PymeCommerce.Catalogo.Dominio;

namespace PymeCommerce.Catalogo.Persistencia;

internal sealed class CatalogoDbContext(DbContextOptions<CatalogoDbContext> options) : DbContext(options)
{
    // Cada módulo tiene su propio esquema en la base: ningún otro módulo lee estas tablas.
    public const string Esquema = "catalogo";

    public DbSet<Producto> Productos => Set<Producto>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Esquema);

        modelBuilder.Entity<Producto>(producto =>
        {
            producto.HasKey(p => p.Id);
            producto.Property(p => p.Nombre).HasMaxLength(200).IsRequired();
        });
    }
}
