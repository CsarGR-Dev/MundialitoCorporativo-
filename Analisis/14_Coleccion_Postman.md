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

- [x] Colección y environment exportados (formato v2.1) en el repositorio.
- [x] Todos los endpoints cubiertos y agrupados por recurso.
- [x] Carpetas específicas de idempotencia, paginación/filtros y errores.
- [x] Ninguna URL fija: todo usa `{{baseUrl}}` y variables.
- [x] La colección corre completa en verde contra el entorno Docker recién levantado.

## Implementación real

Archivos en `Postman/`: la colección (64 requests en 9 carpetas), el entorno `Mundialito Local` y un `README.md` con el uso.

### Estructura

Las cinco primeras carpetas reproducen el listado de Scalar (`/scalar`): **Equipos, Jugadores, Partidos, PosicionesJuego y Torneos**, en orden alfabético, con un request por endpoint (33 en total), nombrado con su ruta y en el mismo orden. Los parámetros de ruta son variables de Postman (`:id`) y los filtros y la paginación aparecen en *Params*, desactivados los que no se usan. Después vienen los casos: **Idempotencia, Filtros y paginación, Errores y Trazabilidad**.

### Ajustes respecto al diseño inicial

| Diseño inicial | Implementado | Motivo |
|---|---|---|
| Carpeta `postman/` | `Postman/` | Mismo estilo que `Analisis/`. |
| Entorno `Mundialito.Docker` | No se agrega: el entorno `Mundialito Local` sirve para los dos | En Docker la API se publica en el mismo puerto (5000). La colección corre en verde contra la API en Docker. |
| `00 Health` con `GET /health` | No se incluye | La API no tiene endpoint de health. Los datos del seed se verifican en las consultas del torneo (carpeta Torneos). |
| Carpetas numeradas con nombres propios | Carpetas con los mismos grupos, nombres y orden que Scalar | La colección y la documentación de la API muestran el mismo listado. |
| Flujo en orden (crear antes de usar) | Pre-request que prepara los datos cuando el orden de Scalar no lo permite | `POST /api/partidos` crea su torneo con dos equipos inscritos; cada `DELETE` crea antes el registro que elimina. |
| Ids sueltos (`equipoId`, `matchId`) | Variables de colección con nombres del dominio (`equipoLocalId`, `partidoId`, …) | Se capturan en las pruebas de cada request. |
| Datos fijos | Sufijo único por ejecución, generado en el primer request | La colección se puede ejecutar varias veces sin chocar con datos anteriores y sin cambiar la tabla del seed. |

### Pruebas

- Cada request valida su código HTTP y lo relevante del cuerpo: formato paginado, `errorCode`, orden, conteos y el header `Idempotent-Replayed`.
- A nivel de colección, toda respuesta debe traer `X-Correlation-Id` y `X-Trace-Id`, y todo error debe ser `application/problem+json` con `traceId`.
- Validación con Newman contra la API con la base recién creada por el seed: 64 requests, 219 aserciones, sin fallos, en dos ejecuciones seguidas.

### Criterios del requerimiento

| Pide | Dónde |
|---|---|
| Endpoints organizados | Carpetas Equipos, Jugadores, Partidos, PosicionesJuego y Torneos: todos los recursos y verbos, igual que Scalar |
| Idempotencia | Carpeta Idempotencia (POST con clave, PUT repetido y DELETE repetido) |
| Filtros | Carpeta Filtros y paginación, y los GET de colección de cada recurso (nombre, estado, equipo, posición, fecha) |
| Paginación | Carpeta Filtros y paginación (páginas, tamaño, orden, fuera de rango) y la validación del formato en cada GET de colección |
| Errores | Carpeta Errores (400, 404, 409) y Trazabilidad (traceId y correlationId en el error) |
| Variables de entorno | `baseUrl` en el entorno; ids y claves como variables de colección |
