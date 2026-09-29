using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PymeCommerce.Stock.Contracts;
using PymeCommerce.Stock.Persistencia;

namespace PymeCommerce.Stock;

internal static class StockEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/stock");

        grupo.MapGet("/depositos", async (StockDbContext db, CancellationToken ct) =>
            await db.Depositos
                .AsNoTracking()
                .OrderBy(d => d.Nombre)
                .Select(d => new DepositoResumen(d.Id, d.Nombre))
                .ToListAsync(ct));
    }
}
