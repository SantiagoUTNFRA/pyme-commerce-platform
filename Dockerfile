# syntax=docker/dockerfile:1

# ---------- Etapa 1: build (imagen grande, con el SDK) ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Primero solo los archivos que definen dependencias: mientras no cambien,
# Docker reutiliza la capa cacheada del restore y el build es mucho más rápido.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY --parents src/**/*.csproj ./
RUN dotnet restore src/Api/PymeCommerce.Api/PymeCommerce.Api.csproj

# Recién ahora el código fuente.
COPY .editorconfig ./
COPY src/ src/
RUN dotnet publish src/Api/PymeCommerce.Api/PymeCommerce.Api.csproj \
    --configuration Release --no-restore --output /app/publish

# ---------- Etapa 2: runtime (imagen chica, solo el runtime de ASP.NET) ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Las imágenes de .NET traen un usuario sin privilegios; no corremos como root.
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "PymeCommerce.Api.dll"]
