# Frontend – Mundialito Corporativo

Next.js 16 (App Router) con TypeScript. Consume la API REST del Mundialito (ítem 11).

## Requisitos

- Node.js 20 o superior.
- La API en ejecución: `dotnet run --project MundialitoCorp_Back/src/MundialitoCorporativo.Api` (http://localhost:5000).

## Ejecutar

```bash
cd MundialitoCorp_Front
npm install
npm run dev            # http://localhost:3000
```

Producción:

```bash
npm run build
npm start
```

La URL de la API se configura con la variable `API_URL`; por defecto es `http://localhost:5000`. Ver `.env.example`.

## Pantallas

| Ruta | Contenido |
|---|---|
| `/` | Inicio: resumen del torneo (tabla, próximos partidos y goleadores) con un carrusel para recorrer los torneos, y acceso a cada sección |
| `/tabla-posiciones` | Tabla del torneo en el orden oficial (puntos, diferencia de gol, goles a favor), con filtro por equipo |
| `/goleadores` | Ranking de goleadores con filtro por equipo y jugador, orden por columna |
| `/partidos` | Calendario con filtros por rango de fechas, equipo y estado; orden y paginación |
| `/partidos/[id]` | Detalle del partido y sus goles |
| `/partidos/[id]/resultado` | Registro del resultado: marcador y un goleador con minuto por cada gol |
| `/equipos` | Equipos con filtros por nombre y departamento, orden por columna y tamaño de página |
| `/equipos/[id]` | Datos del equipo e integrantes, con filtro por posición |

Partidos, tabla y goleadores se muestran por torneo: por defecto el que está en curso, o el que se elija en el selector.

## Estilos

CSS propio en `src/app/globals.css`, sin dependencias de interfaz: un solo juego de variables de color en `:root` con `light-dark()` y una clase por componente (`.filters`, `.table-wrap`, `.badge`, `.card`…), así el marcado de las páginas se mantiene limpio.

- **Modo claro y oscuro.** Por omisión manda la preferencia del sistema; el botón de la cabecera la fija con `data-theme` en `<html>` y la recuerda en `localStorage`. Un script en línea lo aplica antes de pintar, así no se ve un parpadeo claro al entrar en modo oscuro.
- Tipografía Inter con `next/font/google`: se descarga durante el build y se sirve desde el mismo dominio.
- Las tablas van dentro de `.table-wrap`. En escritorio entran completas (hasta 1024 px de ancho de ventana no hay barra horizontal); por debajo de eso, solo esa tarjeta se desplaza en horizontal, nunca la página.
- La navegación marca la sección visible con `aria-current="page"`. Como lee la ruta en el navegador, el layout la envuelve en `<Suspense>`: con Cache Components, un hook de URL fuera de un `<Suspense>` impide prerenderizar el layout.
- Animaciones desactivadas con `prefers-reduced-motion`.

## Arquitectura

```
src/
├── app/            Páginas (Server Components): leen la URL, llaman a lib/api y componen la vista
├── components/     Presentación reutilizable; no conoce la API
│   └── forms/      ResultadoForm (Client Component)
└── lib/
    ├── api/        Única capa que conoce la API: http-client.ts y un módulo por recurso
    ├── types/      Modelos con los mismos nombres que la API y contratos (PagedResponse, ApiError, ApiResult)
    └── …           Lectura de la URL, formato de fechas, torneo actual, Idempotency-Key
```

- **El navegador no llama a la API.** Las páginas son Server Components y el registro de resultado es una Server Action. Por eso no hace falta CORS y la URL de la API no se publica. `lib/api` usa `server-only`, así que importarlo desde un componente del navegador es un error de compilación.
- **Filtros, página y orden en la URL** (`?torneo=…&estado=Jugado&pageNumber=2&sortBy=fechaProgramada&sortDirection=desc`), con los mismos nombres que la API: se pueden compartir, recargar y usar con el botón "atrás".
- **Solo se recarga la tabla.** Filtrar, ordenar, paginar o cambiar de torneo navega del lado del cliente (`FilterForm`, `TorneoSelector` y los enlaces de `SortableHeader` y `Pagination`), sin recargar la página. Cada lista pide sus datos dentro de un `<Suspense>` con clave en los parámetros de la URL, así el esqueleto de carga aparece solo en el bloque de la tabla y el resto de la página no se mueve. La consulta se lanza antes de `await` y viaja a ese bloque, para que siga en paralelo con lo que necesita el formulario.
- **Errores como valor:** igual que el Result Pattern del backend, cada llamada devuelve `ApiResult<T>` y la página decide cómo mostrar el error.
  - 404 abre la página "No encontrado".
  - 400 y 409 muestran el mensaje de la API y los errores por campo.
  - Si la API está caída, aparece un mensaje claro.
  - Siempre se muestra el `traceId` para soporte.
  - `error.tsx` queda para lo inesperado, con un botón de reintento.
- **Idempotency-Key en el registro de resultado:**
  - La clave se crea en el primer envío y se conserva mientras ese intento está en curso, así un doble envío no registra dos veces.
  - Tras una respuesta de error se usa una clave nueva, porque la API guarda también las respuestas de error.
- **Trazabilidad:** cada llamada envía un `X-Correlation-Id` con prefijo `web-`. Con él se encuentran en los logs de la API y en `EventosDominio` las operaciones hechas desde el frontend.
- **Cache Components (Next 16):** los datos se piden en cada request, sin caché. `http-client` llama a `connection()` antes de cada llamada, porque el `X-Correlation-Id` se arma al vuelo y no es prerenderizable; así lo que depende de la API queda bajo su `<Suspense>` y el resto de la página se prerenderiza. El build no necesita la API, lo que permite construir la imagen Docker sin la base. Como ese contenido se transmite después del esqueleto, las páginas con datos necesitan JavaScript en el navegador para mostrarlo.
