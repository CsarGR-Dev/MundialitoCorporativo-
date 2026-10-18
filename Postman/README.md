# Colección de Postman – Mundialito Corporativo

| Archivo | Contenido |
|---|---|
| `MundialitoCorporativo.postman_collection.json` | 64 requests en 10 carpetas, cada una con pruebas |
| `Mundialito.Local.postman_environment.json` | Entorno `Mundialito Local`: `baseUrl = http://localhost:5000` |

## Uso

1. Iniciar la API: `dotnet run --project MundialitoCorp_Back/src/MundialitoCorporativo.Api`. Con la base vacía o inexistente, la crea y carga los datos iniciales.
2. En Postman: **Import** → los dos archivos → seleccionar el entorno **Mundialito Local**.
3. Ejecutar la colección completa **en orden** (*Run collection*). Cada request guarda en variables los ids que usan los siguientes.

Desde la terminal, con Node.js:

```bash
npx newman run Postman/MundialitoCorporativo.postman_collection.json -e Postman/Mundialito.Local.postman_environment.json
```

Resultado esperado: 64 requests y 215 aserciones sin fallos.

Cada ejecución crea su propio torneo, equipos y jugadores con un sufijo único que se genera en el primer request. Por eso la colección se puede ejecutar varias veces seguidas y no cambia la tabla de posiciones del seed.

## Carpetas

| Carpeta | Qué muestra |
|---|---|
| 00 Datos del seed | Torneo, tabla de posiciones, goleadores y partidos programados cargados al iniciar (ítem 9) |
| 01 Torneos | Crear, obtener, listar con filtro y orden, PUT (dos veces, idempotente), PATCH de estado |
| 02 Equipos | CRUD completo e inscripción en el torneo |
| 03 Jugadores | Registro por equipo, integrantes, filtros, PUT y PATCH |
| 04 Partidos y resultados | Programar, cambiar sede, registrar resultado con goleadores, tabla y goleadores del torneo |
| 05 Idempotencia | Clave nueva (201), repetición (201 idéntico con `Idempotent-Replayed: true`), misma clave con otro cuerpo (409), sin clave (400), comprobación de que no se duplicó |
| 06 Filtros y paginación | `pageNumber`, `pageSize`, `sortBy`, `sortDirection`, página fuera de rango, partidos por fecha, estado y equipo, tabla filtrada |
| 07 Errores | 404, 400 de validación, JSON mal formado, `sortBy` no permitido, `pageSize` > 100, 409 por nombre duplicado, inscripción duplicada, resultado ya registrado, partido jugado que no se puede eliminar, ruta inexistente |
| 08 Trazabilidad | `X-Correlation-Id` propio que vuelve en la respuesta; `traceId` y `correlationId` del ProblemDetails iguales a los headers |
| 09 Eliminación idempotente | DELETE dos veces (204 y 204), id que nunca existió (404) |

Pruebas a nivel de colección, en cada respuesta:
- vienen los headers `X-Correlation-Id` y `X-Trace-Id`;
- todo error es `application/problem+json` y trae `traceId`.

## Variables

| Variable | Ámbito | Uso |
|---|---|---|
| `baseUrl` | Entorno | URL de la API |
| `idempotencyKey` | Request | GUID nuevo para cada POST (script de la colección) |
| `claveIdempotencia` | Colección | Clave fija de la carpeta 05, para comprobar la repetición |
| `sufijo`, `abreviatura*` | Colección | Valores únicos de la ejecución |
| `seedTorneoId`, `seedPartidoProgramadoId` | Colección | Datos del seed |
| `torneoId`, `equipoLocalId`, `equipoVisitanteId`, `jugadorLocalId`, `jugadorVisitanteId`, `partidoId`, `equipoIdempotenteId` | Colección | Ids capturados al crear |
