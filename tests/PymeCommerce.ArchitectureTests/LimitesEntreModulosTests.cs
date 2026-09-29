using ArchUnitNET.Domain;
using ArchUnitNET.Loader;
using ArchUnitNET.xUnitV3;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace PymeCommerce.ArchitectureTests;

/// <summary>
/// Hace cumplir los límites del monolito modular (ADR-0001 y ADR-0002):
/// un módulo solo puede usar la API pública (Contracts) de otro, nunca su interior.
/// </summary>
public sealed class LimitesEntreModulosTests
{
    private static readonly IReadOnlyList<Modulo> Modulos = Modulo.DescubrirTodos();

    private static readonly Architecture Arquitectura = new ArchLoader()
        .LoadAssemblies(Modulos.SelectMany(m => new[] { m.Interior, m.Contracts }).ToArray())
        .Build();

    public static TheoryData<string> NombresDeModulos() => new(Modulos.Select(m => m.Nombre));

    public static TheoryData<string, string> ParesDeModulos()
    {
        var pares = new TheoryData<string, string>();
        foreach (var origen in Modulos)
        {
            foreach (var destino in Modulos.Where(m => m != origen))
            {
                pares.Add(origen.Nombre, destino.Nombre);
            }
        }
        return pares;
    }

    [Fact]
    public void Se_descubren_los_modulos()
    {
        // Sin esto, si el descubrimiento fallara, las demás reglas pasarían sin verificar nada.
        Assert.True(Modulos.Count >= 2, $"Se esperaban al menos 2 módulos y se encontraron {Modulos.Count}.");
    }

    [Theory]
    [MemberData(nameof(ParesDeModulos))]
    public void El_interior_de_un_modulo_no_depende_del_interior_de_otro(string origen, string destino)
    {
        var regla = Types().That().ResideInAssembly(Buscar(origen).Interior)
            .Should().NotDependOnAny(Types().That().ResideInAssembly(Buscar(destino).Interior))
            .Because($"{origen} solo puede usar {destino} a través de PymeCommerce.{destino}.Contracts");

        regla.Check(Arquitectura);
    }

    [Theory]
    [MemberData(nameof(NombresDeModulos))]
    public void Los_contracts_no_dependen_del_interior_de_ningun_modulo(string nombre)
    {
        var interiores = Modulos.Select(m => m.Interior).ToArray();

        var regla = Types().That().ResideInAssembly(Buscar(nombre).Contracts)
            .Should().NotDependOnAny(Types().That().ResideInAssembly(interiores[0], interiores[1..]))
            .Because("la API pública de un módulo no puede filtrar detalles internos");

        regla.Check(Arquitectura);
    }

    private static Modulo Buscar(string nombre) => Modulos.Single(m => m.Nombre == nombre);
}
