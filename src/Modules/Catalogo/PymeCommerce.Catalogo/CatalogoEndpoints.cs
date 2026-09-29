using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PymeCommerce.Catalogo.Contracts;
using PymeCommerce.Catalogo.Persistencia;

namespace PymeCommerce.Catalogo;

internal static class CatalogoEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/catalogo");

        grupo.MapGet("/productos", async (CatalogoDbContext db, CancellationToken ct) =>
            await db.Productos
                .AsNoTracking()
                .OrderBy(p => p.Nombre)
                .Select(p => new ProductoResumen(p.Id, p.Nombre))
                .ToListAsync(ct));
    }
}
