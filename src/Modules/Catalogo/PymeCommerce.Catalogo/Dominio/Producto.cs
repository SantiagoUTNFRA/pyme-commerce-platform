namespace PymeCommerce.Catalogo.Dominio;

internal sealed class Producto
{
    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = null!;

    // Constructor para EF Core, que materializa la entidad desde la base.
    private Producto() { }

    public Producto(string nombre)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        Id = Guid.CreateVersion7();
        Nombre = nombre.Trim();
    }
}
