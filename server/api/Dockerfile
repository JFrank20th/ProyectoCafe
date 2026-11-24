# =============================================================================
# ProyectoCafe API - Dockerfile multi-etapa
# =============================================================================
# Contexto de build: server/api
#   docker build -t pc-api:local server/api
#
# La configuracion (cadena de conexion, clave JWT, CORS) NO se hornea en la
# imagen: se inyecta en runtime con variables de entorno. Ver README.md.
# =============================================================================

# --- Etapa 1: restore (capa cacheable con solo los .csproj) -----------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

ENV DOTNET_NOLOGO=1 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1

COPY ProyectoCafe.API/ProyectoCafe.API.csproj ./ProyectoCafe.API/
COPY ProyectoCafe.Application/ProyectoCafe.Application.csproj ./ProyectoCafe.Application/
COPY ProyectoCafe.Domain/ProyectoCafe.Domain.csproj ./ProyectoCafe.Domain/
COPY ProyectoCafe.Infrastructure/ProyectoCafe.Infrastructure.csproj ./ProyectoCafe.Infrastructure/

RUN dotnet restore ProyectoCafe.API/ProyectoCafe.API.csproj

# --- Etapa 2: publish Release ----------------------------------------------
COPY . .

RUN dotnet publish ProyectoCafe.API/ProyectoCafe.API.csproj \
        --configuration Release \
        --output /app/publish \
        --no-restore \
        /p:UseAppHost=false \
    && rm -f /app/publish/appsettings.Development.json

# --- Etapa 3: runtime -------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# El puerto 8080 es el esperado por Azure Container Apps.
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080

COPY --from=build /app/publish ./

# Imagen oficial de .NET: usuario no-root "app" (UID $APP_UID).
USER $APP_UID

ENTRYPOINT ["dotnet", "ProyectoCafe.API.dll"]
