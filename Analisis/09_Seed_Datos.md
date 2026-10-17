# Ítem 9 – Seed de Datos (Obligatorio)

## Requerimiento

Automático al iniciar si la base de datos está vacía:
- 4 equipos
- 5 jugadores por equipo
- 6 partidos
- 3 con resultados

Debe estar en Infrastructure, funcionar con Docker y permitir probar la tabla inmediatamente.

## Diseño

### Ubicación

`Infrastructure/Seed/DatabaseSeeder.cs` + `Infrastructure/Seed/SeedData.cs` (datos estáticos).

### Ejecución

1. En el arranque de la API (`Program.cs`) se invoca una extensión `await app.InitializeDatabaseAsync();` definida en Infrastructure.
2. Esta extensión:
   - Crea la base y su esquema con los scripts de `Db/` (re-ejecutables), con política de reintentos (SQL Server puede tardar en estar listo dentro de Docker). Ver *Implementación real*: no se usan migraciones de EF Core.
   - Verifica si existen torneos o equipos (`AnyAsync`). Si hay datos, no hace nada (idempotente).
   - Si está vacía, inserta los datos usando las entidades de dominio y sus métodos de fábrica (respetando invariantes) y confirma con el **Unit of Work** en una sola transacción.
3. Registra con log Information: `Seed ejecutado: {Tournaments} torneo, {Teams} equipos, {Players} jugadores, {Matches} partidos`.
4. Se controla con configuración `Database:SeedOnStartup` (true por defecto en Docker y Development).

Se descarta `HasData` de EF Core porque no permite usar la lógica de dominio ni condicionar a “base vacía”.

### Datos propuestos

**Torneo (1)**: *Mundialito Corporativo 2026*, estado `EnCurso`, con inicio en el pasado. Los 4 equipos quedan inscritos en él. El catálogo de posiciones no forma parte del seed: lo crea el script de estructura (`Db/001`).

**Equipos (4)**

| Equipo | Abreviatura | Departamento |
|---|---|---|
| Tecnología FC | TEC | Tecnología |
| Finanzas United | FIN | Finanzas |
| Operaciones City | OPE | Operaciones |
| Recursos Humanos Athletic | RRHH | Recursos Humanos |

**Jugadores (5 por equipo = 20)**: un portero, dos defensas, un mediocampista, un delantero; números de camiseta 1, 4, 5, 8, 9.

**Partidos (6)** – todos contra todos a una vuelta:

| # | Local | Visitante | Estado | Marcador |
|---|---|---|---|---|
| 1 | Tecnología FC | Finanzas United | Played | 2 – 1 |
| 2 | Operaciones City | Recursos Humanos Athletic | Played | 1 – 1 |
| 3 | Tecnología FC | Operaciones City | Played | 0 – 3 |
| 4 | Finanzas United | Recursos Humanos Athletic | Scheduled | — |
| 5 | Tecnología FC | Recursos Humanos Athletic | Scheduled | — |
| 6 | Finanzas United | Operaciones City | Scheduled | — |

Los 3 partidos jugados incluyen sus goles con goleador y minuto (total 8 goles), para que la tabla de goleadores tenga datos.

**Tabla esperada después del seed**

| Pos | Equipo | PJ | G | E | P | GF | GC | DG | Pts |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Operaciones City | 2 | 1 | 1 | 0 | 4 | 1 | +3 | 4 |
| 2 | Tecnología FC | 2 | 1 | 0 | 1 | 2 | 4 | −2 | 3 |
| 3 | Recursos Humanos Athletic | 1 | 0 | 1 | 0 | 1 | 1 | 0 | 1 |
| 4 | Finanzas United | 1 | 0 | 0 | 1 | 1 | 2 | −1 | 0 |

Esta tabla queda documentada en el README y sirve como caso de prueba de integración.

Fechas: los partidos jugados con fechas pasadas relativas a la fecha de ejecución; los pendientes con fechas futuras, para que los filtros por fecha se puedan comprobar.

## Docker

- El servicio `api` depende de `db` con `condition: service_healthy`.
- Al ejecutar `docker-compose up` en un volumen nuevo, la API crea la base y siembra sola.
- Para reiniciar datos: `docker-compose down -v`.

## Criterios de aceptación

- [x] Con base vacía, al iniciar quedan 1 torneo en curso con 4 equipos inscritos, 20 jugadores, 6 partidos, 3 jugados.
- [x] Script equivalente en `Db/003_DML_SeedInicial.sql` para cargar los mismos datos desde SSMS.
- [x] Al reiniciar la API no se duplican datos.
- [x] El código vive en Infrastructure.
- [ ] Funciona con `docker-compose up` sin pasos manuales. Ya funciona con un servidor SQL Server sin la base, que es lo que encuentra la API en Docker; falta el `docker-compose.yml` (ítem 12).
- [x] `GET /api/torneos/{id}/tabla-posiciones` devuelve la tabla esperada inmediatamente.

## Implementación real

### Ajustes respecto al diseño inicial

| Diseño inicial | Implementado | Motivo |
|---|---|---|
| `Database.MigrateAsync()` (migraciones de EF Core) | Los scripts `Db/001` y `Db/002`, incluidos en el ensamblado de Infrastructure, se ejecutan al iniciar | El esquema ya estaba definido y validado en los scripts (índices filtrados, CHECK, FK compuestas, vistas, descripciones). Con migraciones habría dos definiciones del mismo esquema que podrían diferir. Los scripts son re-ejecutables, así que correrlos en cada arranque no cambia una base existente. |
| Control con `Database:SeedOnStartup` | `Database:ApplySchemaOnStartup` y `Database:SeedOnStartup` (ambos `true`), más `ConnectionRetries` (10) y `ConnectionRetryDelay` (5 s) | Se puede apagar cada paso por separado; las pruebas de la API los apagan porque usan su propia base. |
| Torneo con fecha de inicio fija | Fechas relativas al día del seed: el torneo empezó hace 14 días y termina en 30 | Los partidos jugados siempre quedan en el pasado y los pendientes en el futuro, el día que se ejecute. |
| `AnyAsync` de EF para saber si está vacía | Una consulta SQL directa: `EXISTS` en `Torneos` o `Equipos`, incluidos los eliminados | Si alguien borró los datos del seed, no se vuelven a cargar. |

### Flujo al iniciar la API

1. `Program.cs` llama a `app.Services.InitializeDatabaseAsync()` antes de atender peticiones.
2. `DatabaseInitializer` se conecta a `master`, con reintentos si SQL Server todavía no responde, y ejecuta `001` y `002` por lotes (separador `GO`), adaptados al nombre de la base de la cadena de conexión.
3. `DatabaseSeeder` revisa si la base está vacía. Si lo está, crea los datos con las fábricas del dominio (`Torneo.Create`, `Equipo.Create`, `Jugador.Create`, `Partido.Schedule`, `RegisterResult`) y los confirma con el **Unit of Work** en una sola transacción.
4. Como pasa por el dominio y el Unit of Work, quedan registrados los eventos (4 equipos creados, 20 jugadores, 6 partidos programados, 3 resultados), con sus logs.
5. Log final: `Seed ejecutado: 1 torneo, 4 equipos, 20 jugadores, 6 partidos (3 con resultado)`. En un reinicio: `La base ya tiene datos; no se ejecuta el seed`.

Si una regla de dominio rechazara un dato del seed, se lanza una excepción: los datos son fijos, así que sería un error de programación y no de negocio. Si dos instancias siembran al mismo tiempo, los índices únicos rechazan la segunda; el Unit of Work devuelve un Conflict, se registra como Warning y la API sigue.

### Datos

- 4 equipos, cada uno con portero (1), dos defensas (4 y 5), mediocampista (8) y delantero (9).
- Partidos a las 18:00 UTC (12:00 en El Salvador), en los días −9, −6 y −3 (jugados) y +3, +6 y +9 (programados).
- Goleadores: Alejandro Morales (Operaciones City) con 3; Diego Flores, Andrés Castillo, Daniel Rivera, Eduardo Vásquez y Paola Guzmán con 1.

### Archivos

| Archivo | Contenido |
|---|---|
| `Infrastructure/Initialization/DatabaseInitializer.cs` | Reintentos de conexión, esquema y llamada al seed |
| `Infrastructure/Initialization/SqlScripts.cs` | Carga de los scripts incluidos, cambio del nombre de la base (validado) y división por `GO` |
| `Infrastructure/Initialization/DatabaseOptions.cs` | Sección `Database` de appsettings |
| `Infrastructure/Seed/SeedData.cs` | Datos del seed |
| `Infrastructure/Seed/DatabaseSeeder.cs` | Carga con el dominio y el Unit of Work |
| `Db/003_DML_SeedInicial.sql` | Los mismos datos para SSMS (no genera eventos de dominio) |

### Pruebas

- `IntegrationTests/Seed/DatabaseSeedSqlServerTests.cs`:
  - A partir de un servidor **sin la base**, como en Docker, se crean la base, el esquema y las vistas, y se cargan los conteos exactos, los eventos procesados, la tabla esperada y el goleador líder.
  - Un segundo arranque no duplica datos.
  - `Db/003` carga lo mismo, es re-ejecutable y deja la misma tabla.
- `UnitTests/Infrastructure/SqlScriptsTests.cs`: scripts incluidos en orden, cambio del nombre de la base, rechazo de nombres inseguros y división por `GO`.
- El fixture de integración crea su base con los mismos scripts incluidos, así que todas las pruebas de integración validan el camino que usa la API.
