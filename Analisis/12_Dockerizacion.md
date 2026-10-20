# Ítem 12 – Dockerización (Obligatorio)

## Requerimiento

Debe incluir:
- Dockerfile API.
- Dockerfile Frontend.
- `docker-compose.yml` con API, DB y Frontend.

Debe levantarse con: `docker-compose up`.

## Implementación real

### Archivos

```
MundialitoCorporativo/
├── docker-compose.yml          DB + API + Frontend (+ Jaeger)
├── .env.example                variables opcionales: contraseña de sa y puertos
├── MundialitoCorp_Back/
│   ├── Dockerfile              imagen de la API
│   └── .dockerignore
└── MundialitoCorp_Front/
    ├── Dockerfile              imagen del frontend
    └── .dockerignore
```

El compose va en la raíz porque `docker-compose up` se ejecuta desde ahí; cada Dockerfile va junto a su proyecto, que es su contexto de build.

### Dockerfile API (`MundialitoCorp_Back/Dockerfile`)

Multi-stage, contexto `MundialitoCorp_Back`:

1. **build** – `mcr.microsoft.com/dotnet/sdk:10.0`
   - Copia `Directory.Build.props` y los 4 `.csproj`, y hace `dotnet restore` del proyecto Api: esa capa queda en caché mientras no cambien las dependencias.
   - Copia `src/` y hace `dotnet publish -c Release --no-restore /p:UseAppHost=false`. Los scripts SQL del esquema viajan dentro del ensamblado de Infrastructure (recurso incrustado).
2. **runtime** – `mcr.microsoft.com/dotnet/aspnet:10.0`
   - Usuario sin privilegios de la imagen oficial (`USER $APP_UID`).
   - Escucha en el puerto 8080 (valor por defecto de la imagen), `EXPOSE 8080`.
   - `ENTRYPOINT ["dotnet", "MundialitoCorporativo.Api.dll"]`.

El `.dockerignore` deja fuera `bin/`, `obj/`, `logs/`, `.vs/`, `tests/` y la solución: al build solo entran los proyectos de `src/`.

### Dockerfile Frontend (`MundialitoCorp_Front/Dockerfile`)

Multi-stage sobre `node:22-alpine`, contexto `MundialitoCorp_Front`:

1. **deps** – `npm ci` con `package.json` y `package-lock.json`.
2. **build** – `npm run build` con `output: "standalone"`. El build no consulta la API.
3. **runtime** – copia `.next/standalone`, `.next/static` y `public`; usuario `node`; `EXPOSE 3000`; `CMD ["node", "server.js"]`.

`API_URL` se lee al ejecutar, no en el build: la misma imagen sirve para cualquier entorno.

### docker-compose.yml (raíz)

| Servicio | Imagen | Puerto en el equipo | Detalle |
|---|---|---|---|
| `db` | `mcr.microsoft.com/mssql/server:2022-latest` (Developer) | 14330 | Volumen `mundialito-data`. `healthcheck` con `sqlcmd` (`SELECT 1`). |
| `api` | build de `./MundialitoCorp_Back` | 5000 → 8080 | Espera a `db` con `condition: service_healthy`. Cadena de conexión con usuario `sa`, OpenAPI/Scalar activos y trazas hacia Jaeger. |
| `frontend` | build de `./MundialitoCorp_Front` | 3000 | `API_URL=http://api:8080`: Next.js llama a la API por la red interna de Docker. |
| `jaeger` | `jaegertracing/jaeger:2.22.0` (Jaeger v2) | 16686 | Recibe OTLP por gRPC en `jaeger:4317` (solo en la red interna) y guarda las trazas en memoria. |

Variables opcionales en `.env` (se versiona `.env.example`, no `.env`): `DB_PASSWORD`, `DB_PORT`, `API_PORT`, `FRONTEND_PORT`. Sin `.env`, el compose usa los mismos valores por defecto.

Al levantar, la API crea la base con los scripts de Infrastructure y carga los datos iniciales (ítem 9). No hay pasos manuales.

### Comandos

```bash
docker-compose up --build        # construye y levanta todo (-d para segundo plano)
docker-compose ps                # estado; db debe aparecer como healthy
docker-compose logs -f api       # logs de la API en JSON
docker-compose down              # detiene y conserva la base
docker-compose down -v           # detiene y borra la base (el próximo up vuelve a sembrar)
```

### URLs

| Servicio | URL |
|---|---|
| Frontend | http://localhost:3000 |
| API | http://localhost:5000/api |
| Scalar | http://localhost:5000/scalar |
| Documento OpenAPI | http://localhost:5000/openapi/v1.json |
| Jaeger UI | http://localhost:16686 (servicio `mundialito-api`) |
| SQL Server | `localhost,14330`, usuario `sa`, contraseña de `DB_PASSWORD` |

### Ajustes respecto al diseño inicial

| Diseño inicial | Implementado | Motivo |
|---|---|---|
| Dockerfile de la API en `src/MundialitoCorporativo.Api/` con contexto en la raíz | `MundialitoCorp_Back/Dockerfile` con contexto `MundialitoCorp_Back` | Cada Dockerfile junto a su proyecto; el contexto solo incluye lo que necesita la API. |
| `node:20-alpine` | `node:22-alpine` | Versión LTS vigente. |
| `NEXT_PUBLIC_API_URL` como `ARG` de build y CORS en la API | Solo `API_URL` en tiempo de ejecución | El navegador nunca llama a la API (ver ítem 11): no hace falta CORS ni reconstruir la imagen para cambiar la URL. |
| SQL Server en el puerto 1433 | 14330 en el equipo | No choca con un SQL Server instalado en el equipo. Dentro de Docker sigue siendo 1433. |
| `ASPNETCORE_ENVIRONMENT: Docker` | Producción con `OpenApi__Enabled=true` | Scalar disponible en Docker sin activar el modo Development. En `appsettings.json` está apagado; en Development, encendido. |
| `Database__SeedOnStartup` explícito | Valor por defecto de `appsettings.json` | El seed ya está activo por defecto y no duplica datos al reiniciar. |
| Logs a archivo | `Observability__LogFilePath` vacío | En un contenedor los logs van a la consola (`docker-compose logs`). |
| `jaegertracing/all-in-one:latest` con 4317 publicado | `jaegertracing/jaeger:2.22.0`, solo la UI publicada | Jaeger v2, la línea con soporte (la 1.x quedó descontinuada); versión fija para builds reproducibles. La API llega a 4317 por la red interna. |
| Endpoint `/health` y healthcheck de la API | No incluido | Criterio propio: no lo pide el requerimiento. El `depends_on` crítico es API → DB, que sí tiene healthcheck. |

## Criterios de aceptación

- [x] `docker-compose up --build` levanta DB, API, frontend y Jaeger sin pasos manuales (verificado con Docker Desktop sin imágenes ni volúmenes previos).
- [x] La API espera a que la DB esté saludable.
- [x] Al iniciar en un volumen nuevo, la API crea la base y siembra: 1 torneo, 4 equipos, 20 jugadores, 6 partidos (3 con resultado).
- [x] El frontend muestra datos del seed al abrir http://localhost:3000.
- [x] Scalar en http://localhost:5000/scalar y trazas de la API en Jaeger.
- [x] La colección de Postman corre en verde contra la API en Docker.
- [x] Imágenes multi-stage y ejecutadas con usuario no root.
- [x] Sin secretos reales versionados: `.env` está en `.gitignore`; la contraseña por defecto es solo para desarrollo local.
