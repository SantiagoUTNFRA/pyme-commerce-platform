using Microsoft.EntityFrameworkCore;
using PymeCommerce.Stock.Contracts;
using PymeCommerce.Stock.Persistencia;

namespace PymeCommerce.Stock;

internal sealed class StockApi(StockDbContext db) : IStockApi
{
    public Task<DepositoResumen?> ObtenerDepositoAsync(Guid depositoId, CancellationToken ct = default) =>
        db.Depositos
            .AsNoTracking()
            .Where(d => d.Id == depositoId)
            .Select(d => new DepositoResumen(d.Id, d.Nombre))
            .SingleOrDefaultAsync(ct);
}
