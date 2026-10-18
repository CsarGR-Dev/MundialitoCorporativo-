# Ítem 14 – Colección de Postman (Obligatorio)

## Requerimiento

Debe incluir:
- Endpoints organizados.
- Casos de: idempotencia, filtros, paginación, errores.
- Variables de entorno.

## Entregables

```
postman/
├── MundialitoCorporativo.postman_collection.json
├── Mundialito.Local.postman_environment.json     (baseUrl http://localhost:5000)
└── Mundialito.Docker.postman_environment.json    (baseUrl http://localhost:5000, mismos puertos)
```

## Variables de entorno

| Variable | Uso |
|---|---|
| `baseUrl` | URL base de la API |
| `equipoId`, `equipoId2` | Ids capturados al crear equipos |
| `jugadorId` | Id capturado al registrar jugador |
| `matchId` | Id capturado al programar partido |
| `idempotencyKey` | Generada en pre-request con `{{$guid}}` o `crypto.randomUUID()` |
| `fixedIdempotencyKey` | Key fija para comprobar el replay |
| `correlationId` | Para enviar `X-Correlation-Id` y verificar que se devuelve |

## Estructura de carpetas

```
Mundialito Corporativo
├── 00 Health
│   └── GET /health
├── 01 Equipos
│   ├── Listar (paginado por defecto)
│   ├── Listar – filtro por nombre
│   ├── Listar – orden por nombre desc
│   ├── Obtener por id
│   ├── Crear
│   ├── Reemplazar (PUT)
│   ├── Modificar parcial (PATCH)
│   └── Eliminar
├── 02 Jugadores
│   ├── Listar por equipo
│   ├── Búsqueda global con filtros
│   ├── Registrar en equipo
│   ├── PUT / PATCH / DELETE
├── 03 Partidos
│   ├── Listar – filtro por rango de fechas
│   ├── Listar – filtro por equipo
│   ├── Listar – filtro por estado
│   ├── Obtener por id
│   ├── Programar
│   ├── Reprogramar (PUT) / Cancelar (PATCH) / Eliminar
│   └── Registrar resultado
├── 04 Torneos y catálogo
│   ├── Listar / crear / PUT / PATCH (cambio de estado) / eliminar torneo
│   ├── Inscribir equipo / inscripción duplicada (→ 409) / retirar equipo
│   ├── Posiciones (catálogo)
│   ├── Tabla de posiciones del torneo
│   └── Goleadores del torneo (con filtro por equipo)
├── 05 Idempotencia
│   ├── POST con key fija (1ª vez → 201)
│   ├── POST con misma key y mismo cuerpo (replay → 201 idéntico)
│   ├── POST con misma key y cuerpo distinto (→ 409)
│   ├── POST sin Idempotency-Key (→ 400)
│   ├── PUT repetido (mismo resultado)
│   └── DELETE repetido (204 y 204)
├── 06 Paginación
│   ├── Página 1, tamaño 2
│   ├── Página 2, tamaño 2
│   ├── Página fuera de rango (data vacía)
│   ├── pageSize > máximo (→ 400)
│   └── sortBy inválido (→ 400)
└── 07 Errores
    ├── 400 – validación (nombre vacío)
    ├── 400 – goles no coinciden con marcador
    ├── 404 – equipo inexistente
    ├── 404 – partido inexistente
    ├── 409 – nombre de equipo duplicado
    ├── 409 – resultado ya registrado
    ├── 409 – eliminar equipo con partidos
    └── 400 – partido con mismo equipo local y visitante
```

## Scripts

- **Pre-request** a nivel de colección: si el request es POST y no tiene key fija, asigna `Idempotency-Key: {{$guid}}`.
- **Tests** en cada request:
  - Código HTTP esperado (`pm.response.to.have.status(201)`).
  - Estructura paginada (`data`, `pageNumber`, `pageSize`, `totalRecords`, `totalPages`).
  - ProblemDetails con `errorCode` y `traceId` en errores.
  - Presencia de headers `X-Correlation-Id` y `X-Trace-Id`.
  - Replay: mismo `id` en ambas respuestas y header `Idempotent-Replayed`.
  - Captura de ids en variables (`pm.environment.set("equipoId", ...)`).

Esto permite ejecutar la colección completa con **Collection Runner** o con Newman:

```
newman run postman/MundialitoCorporativo.postman_collection.json -e postman/Mundialito.Local.postman_environment.json
```

## Criterios de aceptación

- [ ] Colección y environment exportados (formato v2.1) en el repositorio.
- [ ] Todos los endpoints cubiertos y agrupados por recurso.
- [ ] Carpetas específicas de idempotencia, paginación/filtros y errores.
- [ ] Ninguna URL fija: todo usa `{{baseUrl}}` y variables.
- [ ] La colección corre completa en verde contra el entorno Docker recién levantado.

## Implementación real

Archivos en `Postman/`: la colección (64 requests en 10 carpetas), el entorno `Mundialito Local` y un `README.md` con el uso.

### Ajustes respecto al diseño inicial

| Diseño inicial | Implementado | Motivo |
|---|---|---|
| Carpeta `postman/` | `Postman/` | Mismo estilo que `Analisis/`. |
| Entorno `Mundialito.Docker` | Se agrega con la dockerización (ítem 12) | El puerto de la API en Docker se define en ese paso. |
| `00 Health` con `GET /health` | `00 Datos del seed` | La API no tiene endpoint de health todavía. La primera carpeta verifica el seed: torneo, tabla esperada, goleador líder y partidos programados. |
| Carpetas por recurso con Torneos al final | `01 Torneos` primero | El flujo crea primero el torneo y luego inscribe equipos, programa partidos y registra el resultado. Así la colección se ejecuta completa en orden. |
| Ids sueltos (`equipoId`, `matchId`) | Variables de colección con nombres del dominio (`equipoLocalId`, `partidoId`, …) | Se capturan en las pruebas de cada request. |
| Datos fijos | Sufijo único por ejecución, generado en el primer request | La colección se puede ejecutar varias veces sin chocar con datos anteriores y sin cambiar la tabla del seed. |

### Pruebas

- Cada request valida su código HTTP y lo relevante del cuerpo: formato paginado, `errorCode`, orden, conteos y el header `Idempotent-Replayed`.
- A nivel de colección, toda respuesta debe traer `X-Correlation-Id` y `X-Trace-Id`, y todo error debe ser `application/problem+json` con `traceId`.
- Validación con Newman contra la API y la base del seed: 64 requests, 215 aserciones, sin fallos, en dos ejecuciones seguidas.

### Criterios del requerimiento

| Pide | Dónde |
|---|---|
| Endpoints organizados | Carpetas 00 a 04 y 09 (todos los recursos y verbos) |
| Idempotencia | Carpeta 05 (más el PUT repetido en 01 y el DELETE repetido en 09) |
| Filtros | Carpetas 06 y 01 a 03 (nombre, estado, equipo, posición, fecha) |
| Paginación | Carpeta 06 (páginas, tamaño, orden, fuera de rango) y la validación del formato en cada GET de colección |
| Errores | Carpeta 07 (400, 404, 409) y 08 (traceId y correlationId en el error) |
| Variables de entorno | `baseUrl` en el entorno; ids y claves como variables de colección |
