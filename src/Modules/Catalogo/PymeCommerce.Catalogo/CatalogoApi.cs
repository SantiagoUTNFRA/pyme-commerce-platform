using Microsoft.EntityFrameworkCore;
using PymeCommerce.Catalogo.Contracts;
using PymeCommerce.Catalogo.Persistencia;

namespace PymeCommerce.Catalogo;

internal sealed class CatalogoApi(CatalogoDbContext db) : ICatalogoApi
{
    public Task<ProductoResumen?> ObtenerProductoAsync(Guid productoId, CancellationToken ct = default) =>
        db.Productos
            .AsNoTracking()
            .Where(p => p.Id == productoId)
            .Select(p => new ProductoResumen(p.Id, p.Nombre))
            .SingleOrDefaultAsync(ct);
}
