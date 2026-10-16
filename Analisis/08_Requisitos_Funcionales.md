# Ítem 8 – Requisitos Funcionales

## Requerimiento

- **Equipos**: CRUD completo, filtros, paginación, ordenamiento dinámico.
- **Jugadores**: registro por equipo, filtros, paginación.
- **Partidos**: creación, registro de resultado, filtros por fecha, equipo y estado, paginación.
- **Lógica del torneo**: tabla automática ordenada por (1) puntos, (2) diferencia de gol, (3) goles a favor; goleadores; consultas optimizadas con Dapper.

Del requerimiento general: ingreso de equipos participantes, integrantes de equipos, calendario de partidos, resultado de partidos y goleadores; visualización de equipos, integrantes, partidos, registro de resultados, tabla de posiciones y goleadores.

---

## RF-00 Torneos y catálogo de posiciones (criterio propio)

El requerimiento habla de "un torneo"; para soportar varias ediciones del Mundialito sin borrar datos se modelan torneos con inscripción de equipos. La tabla de posiciones y los goleadores se calculan por torneo. Formato: liga de todos contra todos.

| Operación | Endpoint | Detalle |
|---|---|---|
| Listar torneos | `GET /api/torneos` | Filtros: `nombre`, `estado`. Orden: `fechaInicio` (por defecto), `nombre`. Paginado. |
| Detalle | `GET /api/torneos/{id}` | Incluye cantidad de equipos inscritos y de partidos. |
| Crear | `POST /api/torneos` | Body: `nombre`, `descripcion`, `fechaInicio`, `fechaFin`. Estado inicial `Planificado`. |
| Reemplazar / Parcial | `PUT / PATCH /api/torneos/{id}` | PATCH permite cambiar `estado` (`Planificado → EnCurso → Finalizado`, sin retroceso). |
| Eliminar | `DELETE /api/torneos/{id}` | Soft delete; 409 si tiene partidos jugados. |
| Equipos inscritos | `GET /api/torneos/{id}/equipos` | Paginado. |
| Inscribir equipo | `POST /api/torneos/{id}/equipos` | Body: `equipoId`. 409 si ya está inscrito o el torneo está finalizado. |
| Retirar equipo | `DELETE /api/torneos/{id}/equipos/{equipoId}` | 409 si el equipo ya tiene partidos en el torneo. |
| Posiciones de juego | `GET /api/posiciones-juego` | Catálogo (Portero, Defensa, Mediocampista, Delantero), paginado. Solo lectura. |

Validaciones: nombre requerido (3–100) y único; `endDate ≥ startDate`; transiciones de estado válidas (RN-19, RN-23).

## RF-01 Equipos

| Operación | Endpoint | Detalle |
|---|---|---|
| Listar | `GET /api/equipos` | Filtros: `nombre` (contiene), `departamento`. Orden: `nombre`, `fechaCreacion`, `departamento`. Paginado. |
| Detalle | `GET /api/equipos/{id}` | Incluye cantidad de jugadores. |
| Crear | `POST /api/equipos` | Body: `nombre`, `abreviatura`, `departamento`. Emite `EquipoCreatedDomainEvent`. |
| Reemplazar | `PUT /api/equipos/{id}` | Todos los campos obligatorios. |
| Modificar parcial | `PATCH /api/equipos/{id}` | Solo campos enviados. |
| Eliminar | `DELETE /api/equipos/{id}` | Soft delete; 409 si tiene partidos. |

Validaciones: `nombre` requerido (3–100), único; `abreviatura` 2–5 caracteres en mayúsculas; `departamento` opcional (máx. 100).

## RF-02 Jugadores

| Operación | Endpoint | Detalle |
|---|---|---|
| Listar por equipo | `GET /api/equipos/{equipoId}/jugadores` | Filtros: `nombre`, `posicionJuegoId`. Paginado. |
| Búsqueda global | `GET /api/jugadores` | Filtros: `equipoId`, `nombre`, `posicionJuegoId`, `activo`. Paginado. |
| Detalle | `GET /api/jugadores/{id}` | Incluye goles anotados. |
| Registrar | `POST /api/equipos/{equipoId}/jugadores` | Body: `nombres`, `apellidos`, `numeroCamiseta`, `posicionJuegoId`. |
| Reemplazar / Parcial / Eliminar | `PUT/PATCH/DELETE /api/jugadores/{id}` | Mismas reglas REST. |

Validaciones: nombre y apellido requeridos (2–60); `numeroCamiseta` 1–99 único por equipo; `posicionJuegoId` debe existir en el catálogo `PosicionesJuego` y estar activa (RN-22); el equipo debe existir.

## RF-03 Partidos (calendario)

| Operación | Endpoint | Detalle |
|---|---|---|
| Listar | `GET /api/partidos` | Filtros: `torneoId`, `fechaDesde`, `fechaHasta`, `equipoId` (local o visitante), `estado`. Orden: `fechaProgramada` (por defecto), `estado`. Paginado. |
| Detalle | `GET /api/partidos/{id}` | Torneo, equipos, marcador, estado y lista de goles con goleador y minuto. |
| Programar | `POST /api/partidos` | Body: `torneoId`, `equipoLocalId`, `equipoVisitanteId`, `fechaProgramada`, `sede`. |
| Reprogramar | `PUT /api/partidos/{id}` | Solo si `Programado`. |
| Parcial | `PATCH /api/partidos/{id}` | Cambiar `sede`, `fechaProgramada` o `estado` a `Cancelado`. |
| Eliminar | `DELETE /api/partidos/{id}` | 409 si ya fue jugado. |

Validaciones: equipos distintos, existentes e inscritos en el torneo (RN-18); torneo no finalizado (RN-19); fecha dentro del rango del torneo (RN-21); un equipo no puede tener dos partidos en la misma fecha y hora.

## RF-04 Registro de resultado y goleadores

`POST /api/partidos/{id}/resultado` (con `Idempotency-Key`)

```json
{
  "golesLocal": 2,
  "golesVisitante": 1,
  "goles": [
    { "jugadorId": "…", "equipoId": "…", "minuto": 12 },
    { "jugadorId": "…", "equipoId": "…", "minuto": 54 },
    { "jugadorId": "…", "equipoId": "…", "minuto": 77 }
  ]
}
```

Reglas:
- Partido en estado `Programado` (si no → 409 `Partido.AlreadyPlayed` o `Partido.IsCancelled`).
- `golesLocal` y `golesVisitante` ≥ 0.
- Cantidad de goles de cada equipo en `goles` = marcador del equipo (400 `Partido.GolesMismatch`).
- Cada `jugadorId` debe pertenecer al `equipoId` indicado, y ese equipo debe ser local o visitante del partido (400).
- Minuto 1–130.
- Cambia estado a `Jugado`, guarda `FechaJugado`, emite `PartidoResultRegisteredDomainEvent`.
- Respuesta 201 con el detalle del partido.

## RF-05 Tabla de posiciones

`GET /api/torneos/{id}/tabla-posiciones`

| Columna | Cálculo |
|---|---|
| PJ (PartidosJugados) | Partidos con estado `Jugado` |
| G / E / P | Ganados / empatados / perdidos |
| GF / GC | Goles a favor / en contra |
| DG | GF − GC |
| Pts | G×3 + E×1 |

Se calcula para el torneo indicado en la ruta. Orden: Pts ↓, DG ↓, GF ↓, nombre ↑. Incluye a todos los equipos inscritos, aunque no hayan jugado (con ceros). Se calcula al vuelo con SQL (Dapper); no se guarda en una tabla, por lo que siempre está actualizada ("tabla automática"). Paginado para cumplir el ítem 10 (por defecto `pageSize` cubre todos los equipos del torneo). Se agrega la columna `Position` calculada con `ROW_NUMBER()`.

## RF-06 Goleadores

`GET /api/torneos/{id}/goleadores`

- Agrupación de la tabla `Goles` por jugador, solo de partidos jugados del torneo indicado: `JugadorId`, nombre, posición, equipo, total de goles, partidos con gol.
- Filtros: `equipoId`, `nombre`.
- Orden por defecto: goles ↓, apellido ↑.
- Paginado, Dapper con `GROUP BY` y `OFFSET/FETCH`.

## Lógica de dominio testeable

Además del cálculo SQL, se implementa `TablaPosicionesCalculator` en Domain (puro, sin base de datos) con la misma regla de puntos y orden. Sirve para:
- Pruebas unitarias de tabla, puntos y diferencia de gol (ítem 15).
- Documentar la regla de negocio en código.

La prueba de integración valida que la consulta SQL y el calculador en memoria producen el mismo orden para el mismo conjunto de datos.

## Criterios de aceptación

- [ ] Torneos con inscripción de equipos; tabla y goleadores aislados por torneo.
- [ ] Posiciones de juego desde el catálogo `GET /api/posiciones-juego`.
- [ ] CRUD de equipos completo con filtros, paginación y orden dinámico.
- [ ] Jugadores registrados por equipo con filtros y paginación.
- [ ] Partidos programables y filtrables por fecha, equipo y estado.
- [ ] Registro de resultado con goleadores y validación de consistencia.
- [ ] Tabla de posiciones con el orden exacto exigido.
- [ ] Tabla de goleadores con Dapper.
