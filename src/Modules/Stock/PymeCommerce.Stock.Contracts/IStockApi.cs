namespace PymeCommerce.Stock.Contracts;

/// <summary>
/// API pública del módulo Stock: lo único que otros módulos pueden usar de él.
/// </summary>
public interface IStockApi
{
    Task<DepositoResumen?> ObtenerDepositoAsync(Guid depositoId, CancellationToken ct = default);
}

public sealed record DepositoResumen(Guid Id, string Nombre);
