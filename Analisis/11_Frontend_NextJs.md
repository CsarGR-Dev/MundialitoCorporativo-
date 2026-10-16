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
frontend/mundialito-web/
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

- [ ] Las cinco vistas exigidas más integrantes de equipo funcionan contra la API real.
- [ ] Filtros y paginación operativos y reflejados en la URL.
- [ ] El registro de resultado envía `Idempotency-Key` y maneja 400/409.
- [ ] Ningún componente de presentación llama a la API directamente.
- [ ] Errores mostrados con mensaje útil, sin pantallas en blanco.
