using Microsoft.EntityFrameworkCore;
using PymeCommerce.Stock.Dominio;

namespace PymeCommerce.Stock.Persistencia;

internal sealed class StockDbContext(DbContextOptions<StockDbContext> options) : DbContext(options)
{
    // Cada módulo tiene su propio esquema en la base: ningún otro módulo lee estas tablas.
    public const string Esquema = "stock";

    public DbSet<Deposito> Depositos => Set<Deposito>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Esquema);

        modelBuilder.Entity<Deposito>(deposito =>
        {
            deposito.HasKey(d => d.Id);
            deposito.Property(d => d.Nombre).HasMaxLength(200).IsRequired();
        });
    }
}
