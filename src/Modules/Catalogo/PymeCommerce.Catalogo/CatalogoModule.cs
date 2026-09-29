using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PymeCommerce.Catalogo.Contracts;
using PymeCommerce.Catalogo.Persistencia;

namespace PymeCommerce.Catalogo;

/// <summary>
/// Único punto público del interior del módulo: el host lo usa para registrarlo.
/// </summary>
public static class CatalogoModule
{
    public static IServiceCollection AddCatalogoModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Falta la connection string 'Postgres'.");

        services.AddDbContext<CatalogoDbContext>(options => options
            .UseNpgsql(connectionString, npgsql =>
                // Historial de migraciones propio, dentro del esquema del módulo.
                npgsql.MigrationsHistoryTable(HistoryRepository.DefaultTableName, CatalogoDbContext.Esquema))
            .UseSnakeCaseNamingConvention());

        services.AddScoped<ICatalogoApi, CatalogoApi>();

        services.AddHealthChecks().AddDbContextCheck<CatalogoDbContext>("catalogo-db");

        return services;
    }

    public static IEndpointRouteBuilder MapCatalogoEndpoints(this IEndpointRouteBuilder app)
    {
        CatalogoEndpoints.Map(app);
        return app;
    }

    public static async Task MigrarCatalogoAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogoDbContext>();
        await db.Database.MigrateAsync(ct);
    }
}
