# Ítem 12 – Dockerización (Obligatorio)

## Requerimiento

Debe incluir:
- Dockerfile API.
- Dockerfile Frontend.
- `docker-compose.yml` con API, DB y Frontend.

Debe levantarse con: `docker-compose up`.

## Dockerfile API (`src/MundialitoCorporativo.Api/Dockerfile`)

Multi-stage:

1. **build** – `mcr.microsoft.com/dotnet/sdk:10.0`
   - Copiar `.slnx` y `.csproj` primero, `dotnet restore` (aprovecha caché de capas).
   - Copiar el resto, `dotnet publish -c Release -o /app/publish --no-restore`.
2. **runtime** – `mcr.microsoft.com/dotnet/aspnet:10.0`
   - Usuario no root (`USER app`).
   - `ASPNETCORE_URLS=http://+:8080`, `EXPOSE 8080`.
   - `ENTRYPOINT ["dotnet", "MundialitoCorporativo.Api.dll"]`.

Contexto de build: raíz del repositorio (para incluir todos los proyectos referenciados).

## Dockerfile Frontend (`frontend/mundialito-web/Dockerfile`)

Multi-stage:

1. **deps** – `node:20-alpine`, `npm ci`.
2. **build** – `npm run build` con `output: 'standalone'`.
3. **runner** – `node:20-alpine`, usuario no root, copiar `.next/standalone`, `.next/static` y `public`; `EXPOSE 3000`; `CMD ["node", "server.js"]`.

`NEXT_PUBLIC_API_URL` se pasa como `ARG` en build (se incrusta en el bundle del cliente).

## docker-compose.yml (raíz)

```yaml
services:
  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_SA_PASSWORD: "${DB_PASSWORD:-Mundialito#2026}"
    ports: ["1433:1433"]
    volumes: [mundialito-data:/var/opt/mssql]
    healthcheck:
      test: ["CMD-SHELL", "/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P \"$$MSSQL_SA_PASSWORD\" -C -Q 'SELECT 1' || exit 1"]
      interval: 10s
      timeout: 5s
      retries: 10
      start_period: 20s

  api:
    build:
      context: .
      dockerfile: src/MundialitoCorporativo.Api/Dockerfile
    environment:
      ASPNETCORE_ENVIRONMENT: Docker
      ConnectionStrings__Default: "Server=db;Database=Mundialito;User Id=sa;Password=${DB_PASSWORD:-Mundialito#2026};TrustServerCertificate=True"
      Database__SeedOnStartup: "true"
      Cors__AllowedOrigins__0: "http://localhost:3000"
      OTEL_EXPORTER_OTLP_ENDPOINT: "http://jaeger:4317"
    ports: ["5000:8080"]
    depends_on:
      db:
        condition: service_healthy
    healthcheck:
      test: ["CMD-SHELL", "curl -f http://localhost:8080/health || exit 1"]

  frontend:
    build:
      context: frontend/mundialito-web
      args:
        NEXT_PUBLIC_API_URL: "http://localhost:5000"
    environment:
      API_URL: "http://api:8080"
    ports: ["3000:3000"]
    depends_on: [api]

  jaeger:            # opcional valorable
    image: jaegertracing/all-in-one:latest
    ports: ["16686:16686", "4317:4317"]

volumes:
  mundialito-data:
```

Notas:
- La contraseña se toma de un archivo `.env` (se versiona `.env.example`, no `.env`).
- `docker-compose up` (o `docker compose up`) levanta todo; la API crea la base con los scripts de `Db/` y siembra automáticamente (ítem 9).
- Endpoint `/health` en la API con `AddHealthChecks().AddSqlServer(...)`.
- `.dockerignore` en API y frontend (`bin/`, `obj/`, `node_modules/`, `.next/`, `.vs/`).

## URLs tras levantar

| Servicio | URL |
|---|---|
| Frontend | http://localhost:3000 |
| API (Swagger/Scalar) | http://localhost:5000/swagger |
| Health | http://localhost:5000/health |
| Jaeger UI | http://localhost:16686 |
| SQL Server | localhost,1433 |

## Pre-requisito local

Docker no está disponible actualmente en el PATH del equipo de desarrollo: instalar Docker Desktop (con WSL2) antes de esta fase y validar con `docker compose version`.

## Criterios de aceptación

- [ ] `docker-compose up --build` en una máquina limpia levanta DB, API y frontend sin pasos manuales.
- [ ] La API espera a que la DB esté saludable.
- [ ] El frontend muestra datos del seed al abrir http://localhost:3000.
- [ ] Imágenes multi-stage y ejecutadas con usuario no root.
- [ ] Sin secretos reales versionados.
