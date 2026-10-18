# Ítem 11 – Frontend (Next.js Obligatorio)

**No se evalúa diseño visual ni experiencia de usuario; sí funcionalidad, desacoplamiento y manejo de errores.**

## Requerimiento

Debe incluir: visualización de equipos, visualización de partidos, registro de resultados, tabla de posiciones, goleadores, filtros + paginación, manejo correcto de errores. Arquitectura limpia y desacoplada.

Del requerimiento general también: visualización de integrantes de equipos.

## Stack

- Next.js (App Router) con TypeScript.
- Estilos mínimos (CSS Modules o Tailwind básico).
- Validación de formularios con `zod`.
- Sin librerías de estado global; estado de filtros en la URL (`searchParams`).

## Estructura

```
MundialitoCorp_Front/
├── src/
│   ├── app/
│   │   ├── layout.tsx              (menú de navegación)
│   │   ├── page.tsx                (inicio: resumen + tabla de posiciones)
│   │   ├── teams/page.tsx          (listado de equipos)
│   │   ├── teams/[id]/page.tsx     (detalle + integrantes)
│   │   ├── matches/page.tsx        (calendario con filtros)
│   │   ├── matches/[id]/page.tsx   (detalle del partido)
│   │   ├── matches/[id]/result/page.tsx (formulario de resultado)
│   │   ├── standings/page.tsx      (tabla de posiciones)
│   │   ├── scorers/page.tsx        (goleadores)
│   │   ├── error.tsx               (error boundary global)
│   │   └── not-found.tsx
│   ├── lib/
│   │   ├── api/
│   │   │   ├── http-client.ts      (fetch base, URL desde env, headers, parseo de ProblemDetails)
│   │   │   ├── teams.api.ts
│   │   │   ├── players.api.ts
│   │   │   ├── matches.api.ts
│   │   │   ├── standings.api.ts
│   │   │   └── scorers.api.ts
│   │   └── types/                  (DTOs, PagedResponse<T>, ApiError)
│   └── components/
│       ├── DataTable.tsx
│       ├── Pagination.tsx
│       ├── Filters/
│       ├── ErrorMessage.tsx
│       └── forms/MatchResultForm.tsx
├── Dockerfile
├── .env.example                    (NEXT_PUBLIC_API_URL / API_URL)
└── package.json
```

### Capas del frontend

| Capa | Responsabilidad |
|---|---|
| `app/` (páginas) | Componer la vista; leer `searchParams`; llamar a servicios. |
| `components/` | Presentación reutilizable sin conocimiento de la API. |
| `lib/api/` | Única capa que conoce URLs y HTTP. Devuelve objetos tipados o un `ApiError`. |
| `lib/types/` | Contratos que reflejan los DTOs de la API. |

Las páginas nunca construyen URLs ni llaman `fetch` directamente.

## Pantallas

| Pantalla | Funcionalidad |
|---|---|
| Selector de torneo | Lista de torneos (por defecto el que está en curso). Partidos, tabla y goleadores se muestran para el torneo seleccionado. |
| Equipos | Tabla paginada; filtro por nombre y departamento; orden por columnas; crear equipo (opcional). |
| Detalle de equipo | Datos del equipo + integrantes paginados con su posición (catálogo `/api/posiciones-juego`). |
| Partidos | Calendario paginado; filtros por rango de fechas, equipo y estado. |
| Registrar resultado | Formulario: marcador + lista de goles (jugador del equipo, minuto). Envía `Idempotency-Key` generado con `crypto.randomUUID()` una vez por intento de formulario (si el usuario hace doble clic, se reutiliza la misma key). |
| Tabla de posiciones | PJ, G, E, P, GF, GC, DG, Pts en el orden oficial. |
| Goleadores | Ranking paginado, filtro por equipo. |

## Manejo de errores

- `http-client.ts` interpreta ProblemDetails y lanza/retorna un `ApiError { status, title, detail, errorCode, traceId, errors }`.
- 400: mostrar errores de validación por campo en el formulario.
- 404: página `not-found`.
- 409: mensaje claro (ej. "El partido ya tiene resultado registrado").
- 5xx o API caída: mensaje genérico + `traceId` para soporte; `error.tsx` con botón de reintento.
- Estados de carga (`loading.tsx`) y estados vacíos ("No hay partidos para los filtros seleccionados").

## Configuración

- URL de la API por variable de entorno: `API_URL` (lado servidor, nombre del servicio Docker `http://api:8080`) y `NEXT_PUBLIC_API_URL` (lado cliente, `http://localhost:5000`).
- CORS habilitado en la API para el origen del frontend.
- `output: 'standalone'` en `next.config` para una imagen Docker liviana.

## Criterios de aceptación

- [x] Las cinco vistas exigidas más integrantes de equipo funcionan contra la API real.
- [x] Filtros y paginación operativos y reflejados en la URL.
- [x] El registro de resultado envía `Idempotency-Key` y maneja 400/409.
- [x] Ningún componente de presentación llama a la API directamente.
- [x] Errores mostrados con mensaje útil, sin pantallas en blanco.

## Implementación real

Next.js 16.4 (App Router, TypeScript, Cache Components) en `MundialitoCorp_Front`. Detalle de uso y arquitectura en su `README.md`.

### Ajustes respecto al diseño inicial

| Diseño inicial | Implementado | Motivo |
|---|---|---|
| Rutas en inglés (`teams`, `matches`, `standings`, `scorers`) | `/equipos`, `/partidos`, `/tabla-posiciones`, `/goleadores` | Mismo vocabulario que el requerimiento y que las rutas de la API. |
| `API_URL` en el servidor y `NEXT_PUBLIC_API_URL` en el navegador, con CORS en la API | Solo `API_URL`: el navegador nunca llama a la API | Las páginas son Server Components y el registro de resultado es una Server Action. No hace falta CORS, la URL interna (`http://api:8080` en Docker) no se publica y `server-only` impide importar el cliente HTTP desde el navegador. |
| Validación con `zod` en el formulario | Validación del navegador (`required`, `min`, `max`) y la de la API como única fuente de reglas | Evita duplicar las reglas del dominio. Los errores 400 de la API se muestran por campo. |
| `lib/api/players`, `standings`, `scorers` | `torneos.api.ts` (tabla, goleadores, equipos y partidos del torneo), `equipos.api.ts`, `partidos.api.ts` | Siguen la forma de la API: tabla y goleadores son subrecursos del torneo. |
| `DataTable` y `Filters` genéricos | Tablas y formularios GET por página, más `Pagination`, `SortableHeader`, `TorneoSelector`, `ErrorMessage`, `EmptyState` | Cada vista tiene columnas y filtros distintos; un componente genérico agregaba más configuración que la que ahorraba. |
| Estilos mínimos (CSS Modules o Tailwind básico) | Hoja de estilos propia en `app/globals.css`: variables de color y una clase por componente | Sin dependencias de interfaz ni utilidades repartidas por el marcado. El mismo archivo define el modo claro y el oscuro. |
| Idempotency-Key fija por formulario | Se crea en el primer envío, se reutiliza mientras ese intento está en curso y se renueva tras un error | La API guarda también las respuestas 4xx: corregir los datos con la misma clave respondería 409. |

### Decisiones propias

- **Errores como valor.** Cada llamada devuelve `ApiResult<T>`, igual que el Result Pattern del backend:
  - 404 abre `notFound()`;
  - 400 y 409 muestran el detalle y los errores por campo;
  - si la API está caída (estado 0), aparece un mensaje claro;
  - siempre se muestra el `traceId`;
  - `error.tsx` solo atiende lo inesperado.
- **Torneo actual.** Se usa el de la URL (`?torneo=`); si no viene, el que está en curso y, en su defecto, el más reciente.
- **URL sin errores 400.** `pageSize` se limita a 5, 10, 20 o 50, y `sortBy` a la lista de cada vista, así una URL editada a mano no termina en un 400.
- **Fechas.** Se muestran en la hora de El Salvador; la API las entrega en UTC.
- **Trazabilidad.** Cada llamada envía `X-Correlation-Id: web-<guid>`, que queda en los logs de la API y en `EventosDominio`.
- **Presentación.** Cabecera fija con la sección visible marcada, encabezado por página, filtros y tablas en tarjeta, insignias de estado, marcador del partido y estados de carga con esqueleto. Tipografía Inter con `next/font`, servida desde el mismo dominio.
- **Inicio con el estado del torneo.** La portada muestra el torneo en curso: equipos inscritos, partidos jugados y por jugar, las primeras posiciones de la tabla, los próximos partidos y los goleadores. Va bajo `<Suspense>`, así la portada se ve de inmediato y los datos llegan después.
- **Solo se recarga la tabla.** Filtrar, ordenar, paginar y cambiar de torneo navegan del lado del cliente; cada lista pide sus datos dentro de un `<Suspense>` con clave en los parámetros de la URL. Los filtros siguen en la URL y la consulta se lanza antes del `await` para que no se serialice con lo que necesita el formulario.
- **Modo claro y oscuro.** Un solo juego de variables con `light-dark()`; por omisión manda el sistema y el botón de la cabecera fija el tema (`data-theme` en `<html>`, guardado en `localStorage` y aplicado por un script en línea antes de pintar).
- **`connection()` antes de cada llamada a la API.** El `X-Correlation-Id` se arma con `crypto.randomUUID()`, un valor que Next no puede prerenderizar; marcando la llamada como dato de request, lo que depende de la API queda bajo su `<Suspense>` y el resto de la página se prerenderiza.

### Verificación

- `npm run lint` y `npm run build` sin errores. El build se ejecuta sin la API: los datos se piden en cada request.
- Prueba con un navegador real (Edge sin interfaz, con Playwright) contra la API y la base del seed. Pasaron las 15 verificaciones:
  - tabla en orden oficial y filtrada conservando la posición;
  - goleador líder;
  - partidos y filtro por estado desde el formulario;
  - orden de equipos desc y paginación;
  - integrantes y su filtro por posición;
  - 400 de la API mostrado en el formulario, corrección y registro con una clave nueva (sin 409);
  - aviso en un partido ya jugado;
  - equipo inexistente;
  - ningún error de JavaScript.
- En la base quedaron un solo resultado y dos claves de idempotencia (400 y 201). El evento de dominio guardó el `CorrelationId` `web-…` del frontend.
- Con la API detenida, la tabla muestra "La API no está disponible".

### Alta, edición y baja desde el frontend

El requerimiento general pide que el sistema permita **ingresar** equipos participantes, integrantes de equipos, calendario de partidos, resultados y goleadores. La primera versión del frontend solo registraba resultados; ahora cubre todas las escrituras que expone la API:

| Recurso | Operaciones en pantalla | Verbo |
|---|---|---|
| Torneos | Crear, editar, iniciar y finalizar, eliminar; inscribir y retirar equipos | POST, PUT, PATCH (`estado`), DELETE |
| Equipos | Crear, editar, eliminar | POST, PUT, DELETE |
| Integrantes | Registrar por equipo, editar (incluye activo/inactivo), eliminar | POST, PUT, DELETE |
| Partidos | Programar, reprogramar, cancelar, eliminar | POST, PUT, PATCH (`estado: Cancelado`), DELETE |
| Resultado | Registrar marcador y goleadores | POST |

- **Server Actions por sección** (`app/torneos/actions.ts`, `app/equipos/actions.ts`, `app/partidos/actions.ts`): leen el formulario, llaman a `lib/api` y redirigen a la página que muestra el cambio. Un error de la API vuelve al formulario como valor.
- **`ActionForm`** generaliza lo que ya hacía el formulario de resultado: envío con `onSubmit` para no perder los datos, Idempotency-Key por intento en los POST y ProblemDetails en pantalla.
- **`ConfirmActionButton`** para operaciones sin datos (eliminar, retirar, cancelar, cambiar de estado): pide confirmación y muestra el 409 junto al botón (por ejemplo, "El equipo tiene partidos asociados y no puede eliminarse").
- **Las reglas solo están en la API.** El frontend oculta acciones que no aplican (editar un torneo finalizado, reprogramar un partido jugado), pero quien decide es el dominio y su respuesta se muestra tal cual.
- **Fecha del partido:** `datetime-local` en hora de El Salvador (UTC-6, sin horario de verano), enviada a la API en UTC.

**Verificación:** prueba E2E con Edge sin interfaz y Playwright contra la API sobre una base temporal creada por el seed. Pasaron las 31 verificaciones:
- torneo: 400 por fechas y campos conservados, alta, 409 por nombre duplicado, edición, inicio, finalización y 409 al eliminar con partidos jugados;
- equipos: tres altas, 409 por nombre duplicado, edición, 409 al eliminar con partidos y baja de un equipo sin partidos;
- integrantes: alta, 409 por camiseta repetida, edición (número e inactivo) y baja;
- inscripción de dos equipos y 409 al retirar un equipo con partidos;
- partidos: error por fecha fuera del torneo, alta con hora local correcta, reprogramación, resultado 1-0, cancelación y baja;
- tabla de posiciones con el ganador en 3 puntos;
- ningún error de JavaScript.

En la base quedaron las claves de idempotencia de cada POST, también las respuestas 400 y 409, y los eventos de dominio con el `CorrelationId` `web-…`.
