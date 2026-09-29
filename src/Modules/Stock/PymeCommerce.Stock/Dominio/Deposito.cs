namespace PymeCommerce.Stock.Dominio;

internal sealed class Deposito
{
    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = null!;

    // Constructor para EF Core, que materializa la entidad desde la base.
    private Deposito() { }

    public Deposito(string nombre)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nombre);
        Id = Guid.CreateVersion7();
        Nombre = nombre.Trim();
    }
}
