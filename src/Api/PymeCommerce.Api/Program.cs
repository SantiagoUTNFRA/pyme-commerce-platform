using PymeCommerce.Catalogo;
using PymeCommerce.Stock;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddCatalogoModule(builder.Configuration)
    .AddStockModule(builder.Configuration);

var app = builder.Build();

// Modo migrador: aplica las migraciones de todos los módulos y termina.
// En Compose lo ejecuta el servicio `migrator` antes de que arranque la API.
if (args.Contains("--migrate"))
{
    await app.Services.MigrarCatalogoAsync();
    await app.Services.MigrarStockAsync();
    return;
}

app.MapHealthChecks("/health");

app.MapCatalogoEndpoints();
app.MapStockEndpoints();

app.Run();
