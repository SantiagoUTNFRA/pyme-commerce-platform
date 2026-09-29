namespace PymeCommerce.Catalogo.Contracts;

/// <summary>
/// API pública del módulo Catálogo: lo único que otros módulos pueden usar de él.
/// </summary>
public interface ICatalogoApi
{
    Task<ProductoResumen?> ObtenerProductoAsync(Guid productoId, CancellationToken ct = default);
}

public sealed record ProductoResumen(Guid Id, string Nombre);
