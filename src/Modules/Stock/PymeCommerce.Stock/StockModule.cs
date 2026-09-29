using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PymeCommerce.Stock.Contracts;
using PymeCommerce.Stock.Persistencia;

namespace PymeCommerce.Stock;

/// <summary>
/// Único punto público del interior del módulo: el host lo usa para registrarlo.
/// </summary>
public static class StockModule
{
    public static IServiceCollection AddStockModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Falta la connection string 'Postgres'.");

        services.AddDbContext<StockDbContext>(options => options
            .UseNpgsql(connectionString, npgsql =>
                // Historial de migraciones propio, dentro del esquema del módulo.
                npgsql.MigrationsHistoryTable(HistoryRepository.DefaultTableName, StockDbContext.Esquema))
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IStockApi, StockApi>();

        services.AddHealthChecks().AddDbContextCheck<StockDbContext>("stock-db");

        return services;
    }

    public static IEndpointRouteBuilder MapStockEndpoints(this IEndpointRouteBuilder app)
    {
        StockEndpoints.Map(app);
        return app;
    }

    public static async Task MigrarStockAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<StockDbContext>();
        await db.Database.MigrateAsync(ct);
    }
}
