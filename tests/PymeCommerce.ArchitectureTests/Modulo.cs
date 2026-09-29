using System.Reflection;

namespace PymeCommerce.ArchitectureTests;

/// <summary>
/// Un módulo son dos ensamblados: PymeCommerce.&lt;Nombre&gt;.Contracts (su API pública)
/// y PymeCommerce.&lt;Nombre&gt; (su interior).
/// </summary>
internal sealed record Modulo(string Nombre, Assembly Interior, Assembly Contracts)
{
    private const string Prefijo = "PymeCommerce.";
    private const string SufijoContracts = ".Contracts";

    /// <summary>
    /// Descubre los módulos a partir de los *.Contracts.dll que hay junto a los tests.
    /// </summary>
    public static IReadOnlyList<Modulo> DescubrirTodos() =>
        Directory.GetFiles(AppContext.BaseDirectory, $"{Prefijo}*{SufijoContracts}.dll")
            .Select(archivo => Path.GetFileNameWithoutExtension(archivo)[..^SufijoContracts.Length])
            .Order(StringComparer.Ordinal)
            .Select(interior => new Modulo(
                Nombre: interior[Prefijo.Length..],
                Interior: Assembly.Load(interior),
                Contracts: Assembly.Load(interior + SufijoContracts)))
            .ToList();
}
