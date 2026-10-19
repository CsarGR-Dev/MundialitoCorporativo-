# API del Mundialito (ítem 12). Contexto de build: esta carpeta (MundialitoCorp_Back).
# Multi-stage: el SDK compila y publica; la imagen final solo lleva el runtime de ASP.NET.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Primero solo los proyectos: el restore queda en caché mientras no cambien las dependencias.
COPY Directory.Build.props ./
COPY src/MundialitoCorporativo.Domain/MundialitoCorporativo.Domain.csproj src/MundialitoCorporativo.Domain/
COPY src/MundialitoCorporativo.Application/MundialitoCorporativo.Application.csproj src/MundialitoCorporativo.Application/
COPY src/MundialitoCorporativo.Infrastructure/MundialitoCorporativo.Infrastructure.csproj src/MundialitoCorporativo.Infrastructure/
COPY src/MundialitoCorporativo.Api/MundialitoCorporativo.Api.csproj src/MundialitoCorporativo.Api/
RUN dotnet restore src/MundialitoCorporativo.Api/MundialitoCorporativo.Api.csproj

# El código, incluidos los scripts SQL del esquema que Infrastructure lleva en su ensamblado.
COPY src/ src/
RUN dotnet publish src/MundialitoCorporativo.Api/MundialitoCorporativo.Api.csproj \
    --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Usuario sin privilegios que trae la imagen oficial; ASP.NET escucha en el puerto 8080.
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "MundialitoCorporativo.Api.dll"]
