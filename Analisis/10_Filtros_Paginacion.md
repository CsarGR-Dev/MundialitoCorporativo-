# Ítem 10 – Filtros y Paginación (Obligatorio)

## Requerimiento

Todos los GET deben soportar: `pageNumber`, `pageSize`, `sortBy`, `sortDirection` y filtros por entidad.

Respuesta:

```json
{
  "data": [],
  "pageNumber": 1,
  "pageSize": 10,
  "totalRecords": 50,
  "totalPages": 5
}
```

No se permite traer todo a memoria.

## Diseño

### Request base (Application/Common/Paging)

```csharp
public abstract record PagedQuery
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? SortBy { get; init; }
    public SortDirection SortDirection { get; init; } = SortDirection.Asc;
}
```

Cada query de colección hereda y agrega sus filtros (`GetEquiposQuery : PagedQuery, IQuery<PagedResponse<TeamDto>>`).

### Respuesta

```csharp
public sealed record PagedResponse<T>(
    IReadOnlyList<T> Data, int PageNumber, int PageSize, int TotalRecords)
{
    public int TotalPages => (int)Math.Ceiling(TotalRecords / (double)PageSize);
}
```

Se serializa en camelCase con exactamente las cinco propiedades exigidas.

### Validación de parámetros (FluentValidation → 400)

| Parámetro | Regla |
|---|---|
| `pageNumber` | ≥ 1 |
| `pageSize` | 1 – 100 (tope para evitar cargas masivas) |
| `sortBy` | Debe estar en la lista blanca de la entidad |
| `sortDirection` | `asc` o `desc` (sin distinguir mayúsculas) |
| Fechas | `fechaDesde` ≤ `fechaHasta` |

### Ordenamiento dinámico seguro

Cada consulta define un diccionario de columnas permitidas:

```csharp
private static readonly Dictionary<string, string> SortColumns = new(StringComparer.OrdinalIgnoreCase)
{
    ["nombre"]        = "e.Nombre",
    ["departamento"]  = "e.Departamento",
    ["fechaCreacion"] = "e.FechaCreacion"
};
```

El parámetro público de la API (`sortBy=name`) es independiente del nombre real de la columna (`e.Nombre`): la lista blanca hace la traducción. Solo el valor mapeado se inserta en `ORDER BY`; el valor del usuario nunca se concatena. Se agrega siempre una columna de desempate (`Id`) para paginación estable.

### Paginación en base de datos

```sql
SELECT e.Id, e.Nombre, e.Abreviatura, e.Departamento, e.FechaCreacion,
       COUNT(*) OVER() AS TotalRecords
FROM Equipos e
WHERE e.Eliminado = 0
  AND (@Nombre IS NULL OR e.Nombre LIKE '%' + @Nombre + '%')
  AND (@Departamento IS NULL OR e.Departamento = @Departamento)
ORDER BY {columnaPermitida} {ASC|DESC}, e.Id
OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;
```

- `@Skip = (pageNumber - 1) * pageSize`, `@Take = pageSize`.
- Si la página solicitada excede el total, se devuelve `data: []` con el `totalRecords` real (se usa una consulta de conteo separada con `QueryMultipleAsync` para no perder el total en ese caso).

### Filtros por entidad

| Endpoint | Filtros | sortBy permitidos |
|---|---|---|
| `GET /api/torneos` | `nombre`, `estado` | `fechaInicio`, `nombre` |
| `GET /api/torneos/{id}/equipos` | `nombre` | `nombre`, `fechaInscripcion` |
| `GET /api/posiciones-juego` | `activo` | `orden` (por defecto), `nombre` |
| `GET /api/equipos` | `nombre`, `departamento` | `nombre`, `departamento`, `fechaCreacion` |
| `GET /api/jugadores` | `equipoId`, `nombre`, `posicionJuegoId`, `activo` | `apellidos`, `nombres`, `numeroCamiseta`, `equipo` |
| `GET /api/equipos/{id}/jugadores` | `nombre`, `posicionJuegoId` | `apellidos`, `numeroCamiseta` |
| `GET /api/partidos` | `torneoId`, `fechaDesde`, `fechaHasta`, `equipoId`, `estado` | `fechaProgramada`, `estado` |
| `GET /api/torneos/{id}/tabla-posiciones` | `equipo` | `puntos` (por defecto, aplica el orden oficial), `golesFavor`, `nombre` |
| `GET /api/torneos/{id}/goleadores` | `equipoId`, `jugador` | `goles` (por defecto), `jugador`, `equipo` |

### Binding en la API

`[FromQuery]` sobre un record de request; el controlador lo mapea a la Query. Enums aceptados como texto (`JsonStringEnumConverter`).

## Frontend

El frontend replica los parámetros en la URL (`?pageNumber=2&pageSize=10&sortBy=name`) para que filtros y página sean compartibles y se mantengan al recargar.

## Criterios de aceptación

- [ ] Todos los GET de colección aceptan los cuatro parámetros base y sus filtros.
- [ ] Todas las respuestas paginadas tienen el formato exacto exigido.
- [ ] Paginación con `OFFSET/FETCH` en SQL; ningún `ToList()` previo a paginar.
- [ ] `sortBy` no permitido devuelve 400.
- [ ] `pageSize` mayor al tope devuelve 400.
- [ ] Pruebas de integración de filtros y paginación.
