# Colección de Postman – Mundialito Corporativo

| Archivo | Contenido |
|---|---|
| `MundialitoCorporativo.postman_collection.json` | 64 requests en 9 carpetas, cada una con pruebas |
| `Mundialito.Local.postman_environment.json` | Entorno `Mundialito Local`: `baseUrl = http://localhost:5000` |

## Uso

1. Iniciar la API: `dotnet run --project MundialitoCorp_Back/src/MundialitoCorporativo.Api`. Con la base vacía o inexistente, la crea y carga los datos iniciales.
2. En Postman: **Import** → los dos archivos → seleccionar el entorno **Mundialito Local**.
3. Ejecutar la colección completa **en orden** (*Run collection*). Cada request guarda en variables los ids que usan los siguientes.

Desde la terminal, con Node.js:

```bash
npx newman run Postman/MundialitoCorporativo.postman_collection.json -e Postman/Mundialito.Local.postman_environment.json
```

Resultado esperado: 64 requests y 219 aserciones sin fallos. Newman informa 74 requests porque suma las 10 llamadas de preparación que hacen algunos pre-request.

Cada ejecución crea sus propios torneos, equipos y jugadores con un sufijo único que se genera en el primer request. Por eso la colección se puede ejecutar varias veces seguidas y no cambia la tabla de posiciones del seed.

## Endpoints: el mismo listado que Scalar

Las cinco primeras carpetas siguen el listado de Scalar (`/scalar`): mismos grupos, en orden alfabético, y un request por endpoint con el mismo nombre (la ruta) y en el mismo orden. Los parámetros de ruta usan variables de Postman (`:id`) y los filtros, la paginación y el orden aparecen en la pestaña *Params*; los que no se usan quedan desactivados para activarlos a mano.

| Carpeta | Endpoints |
|---|---|
| Equipos | `GET` `POST` `/api/equipos` · `GET` `PUT` `PATCH` `DELETE` `/api/equipos/{id}` · `GET` `POST` `/api/equipos/{id}/jugadores` |
| Jugadores | `GET` `/api/jugadores` · `GET` `PUT` `PATCH` `DELETE` `/api/jugadores/{id}` |
| Partidos | `GET` `POST` `/api/partidos` · `GET` `PUT` `PATCH` `DELETE` `/api/partidos/{id}` · `POST` `/api/partidos/{id}/resultado` |
| PosicionesJuego | `GET` `/api/posiciones-juego` |
| Torneos | `GET` `POST` `/api/torneos` · `GET` `PUT` `PATCH` `DELETE` `/api/torneos/{id}` · `GET` `POST` `/api/torneos/{id}/equipos` · `DELETE` `/api/torneos/{id}/equipos/{equipoId}` · `GET` `/api/torneos/{id}/partidos` · `GET` `/api/torneos/{id}/tabla-posiciones` · `GET` `/api/torneos/{id}/goleadores` |

Como el orden es el de Scalar y no el de un flujo, algunos requests preparan sus datos en el pre-request:

- **`POST /api/partidos`** crea el equipo rival con un integrante y un torneo propio de la ejecución, inscribe los dos equipos y lo pone en curso.
- **Los `DELETE`** de equipos, jugadores, partidos y torneos crean primero un registro solo para eliminarlo, así no se pierden los datos que usan los requests siguientes.
- Las consultas del torneo (equipos inscritos, partidos, tabla de posiciones y goleadores) usan el torneo del seed y verifican sus datos: 4 equipos, 6 partidos y la tabla Operaciones City 4, Tecnología FC 3, Recursos Humanos Athletic 1 y Finanzas United 0.

## Casos

| Carpeta | Qué muestra |
|---|---|
| Idempotencia | Clave nueva (201), repetición (201 idéntico con `Idempotent-Replayed: true`), misma clave con otro cuerpo (409), sin clave (400), comprobación de que no se duplicó, PUT repetido y DELETE repetido (204 y 204) |
| Filtros y paginación | `pageNumber`, `pageSize`, `sortBy`, `sortDirection`, página fuera de rango, partidos por fecha, estado y equipo, tabla filtrada |
| Errores | 404, 400 de validación, JSON mal formado, `sortBy` no permitido, `pageSize` > 100, 409 por nombre duplicado, inscripción duplicada, resultado ya registrado, partido jugado que no se puede eliminar, equipo que nunca existió, retiro de un equipo no inscrito, ruta inexistente |
| Trazabilidad | `X-Correlation-Id` propio que vuelve en la respuesta; `traceId` y `correlationId` del ProblemDetails iguales a los headers |

Pruebas a nivel de colección, en cada respuesta:
- vienen los headers `X-Correlation-Id` y `X-Trace-Id`;
- todo error es `application/problem+json` y trae `traceId`.

## Variables

| Variable | Ámbito | Uso |
|---|---|---|
| `baseUrl` | Entorno | URL de la API |
| `idempotencyKey` | Request | GUID nuevo para cada POST (script de la colección) |
| `claveIdempotencia` | Colección | Clave fija de la carpeta Idempotencia, para comprobar la repetición |
| `sufijo`, `abreviatura*` | Colección | Valores únicos de la ejecución |
| `seedTorneoId`, `seedPartidoProgramadoId` | Colección | Datos del seed |
| `equipoLocalId`, `equipoVisitanteId`, `jugadorLocalId`, `jugadorVisitanteId`, `torneoId`, `torneoPartidosId`, `partidoId`, `equipoIdempotenteId` | Colección | Ids capturados al crear |
| `equipoEliminarId`, `jugadorEliminarId`, `torneoEliminarId`, `partidoEliminarId` | Colección | Registros creados solo para probar los `DELETE` |
