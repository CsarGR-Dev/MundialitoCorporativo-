# Ítem 6 – REST Completo + Idempotencia (Obligatorio)

## Requerimiento

Uso correcto de verbos HTTP:
- GET → lectura
- POST → creación
- PUT → reemplazo completo (idempotente)
- PATCH → modificación parcial
- DELETE → eliminación idempotente

Idempotencia en POST:
- Header `Idempotency-Key`.
- Persistencia de la key.
- Evitar duplicación de registros.
- Retornar la misma respuesta original ante repetición.

Debe manejar correctamente los conflictos y los estados HTTP.

## Semántica por verbo

| Verbo | Comportamiento | Éxito | Errores |
|---|---|---|---|
| GET colección | Paginado, filtrado, ordenado | 200 | 400 (parámetros inválidos) |
| GET por id | Recurso o detalle | 200 | 404 |
| POST | Crea recurso; requiere `Idempotency-Key` | 201 + `Location` | 400, 404 (padre inexistente), 409 |
| PUT | Reemplaza todos los campos editables; no crea (no upsert) | 200 con recurso / 204 | 400, 404, 409 |
| PATCH | Modifica solo campos enviados (`application/merge-patch+json`) | 200 con recurso | 400, 404, 409 |
| DELETE | Soft delete; repetirlo devuelve el mismo resultado | 204 | 404 (id nunca existió), 409 (tiene dependencias) |

### Decisiones

- **PATCH con JSON Merge Patch (RFC 7396)**: más simple de consumir que JSON Patch y suficiente para el caso. Los campos `null` explícitos se validan según regla de cada propiedad.
- **PUT sin upsert**: los Ids se generan en servidor, por lo tanto PUT sobre un id inexistente es 404.
- **DELETE idempotente**: con soft delete, la segunda llamada encuentra el registro con `Eliminado = 1` y devuelve 204 sin cambios. Estado final idéntico en ambas llamadas.
- **Concurrencia optimista**: `RowVersion` en Team y Match. Opcionalmente se expone como `ETag` y se acepta `If-Match` en PUT/PATCH; si no coincide → 409 (o 412 si se usa `If-Match`).
- **Registro de resultado** como sub-recurso: `POST /api/partidos/{id}/resultado`. Si el partido ya tiene resultado → 409.

## Idempotencia en POST

### Tabla `RegistrosIdempotencia`

| Columna | Tipo | Nota |
|---|---|---|
| ClaveIdempotencia | nvarchar(100) | Parte de la PK |
| RutaSolicitud | nvarchar(300) | Parte de la PK (la misma key en rutas distintas no colisiona) |
| MetodoSolicitud | varchar(10) | |
| HashSolicitud | char(64) | SHA-256 del cuerpo normalizado |
| Estado | varchar(20) | `EnProceso` / `Completado` |
| CodigoEstadoRespuesta | smallint | Código HTTP original |
| CuerpoRespuesta | nvarchar(max) | Cuerpo original serializado |
| EncabezadosRespuesta | nvarchar(max) | JSON con al menos `Location` y `Content-Type` |
| FechaCreacion / FechaCompletado / FechaExpiracion | datetime2(3) | Expiración sugerida: 24 h |

PK compuesta (`ClaveIdempotencia`, `RutaSolicitud`) para que dos requests concurrentes no puedan insertar la misma key. Definición completa en `Analisis/16_Analisis_Base_Datos.md`.

### Flujo (filtro `IdempotencyFilter` en Api, almacenamiento en Infrastructure)

1. Si el POST no trae `Idempotency-Key` → **400** (`Idempotency.KeyRequired`). Se valida formato (máx. 100 caracteres; se recomienda GUID).
2. Calcular hash del cuerpo.
3. Buscar la key:
   - **No existe** → insertar registro `EnProceso` (si choca por el índice único, otra petición la ganó → **409** `Idempotency.InProgress`). Ejecutar la acción. Guardar `StatusCode`, cuerpo y headers; marcar `Completed`.
   - **Existe, `Completed`, mismo hash** → devolver exactamente el mismo código, cuerpo y headers originales, más header `Idempotent-Replayed: true`. No se ejecuta el handler.
   - **Existe, mismo key, hash distinto** → **409** (`Idempotency.KeyReused`) (también aceptable 422; se documenta 409 por coherencia con el documento).
   - **Existe, `EnProceso`** → **409** (`Idempotency.InProgress`).
4. Si la acción termina en 5xx, se elimina el registro para permitir reintento con la misma key. Las respuestas 4xx sí se guardan (repetir la misma petición inválida da la misma respuesta).
5. Limpieza de registros expirados mediante `BackgroundService` periódico.

### Contrato

```csharp
public interface IIdempotencyStore
{
    Task<IdempotencyRecord?> GetAsync(string key, string path, CancellationToken ct);
    Task<bool> TryBeginAsync(string key, string path, string method, string requestHash, CancellationToken ct);
    Task CompleteAsync(string key, string path, int statusCode, string body, string headers, CancellationToken ct);
    Task ReleaseAsync(string key, string path, CancellationToken ct);
}
```

### Aplicación

- Se aplica a todos los POST mediante atributo `[Idempotent]` o filtro global para POST.
- El store usa Dapper/SQL directo (es infraestructura transversal, fuera del UoW de negocio) para que el registro sea visible aunque la transacción del comando falle.

## Escenarios a verificar en Postman

1. POST equipo con key K1 → 201.
2. Repetir POST con K1 y el mismo cuerpo → 201 idéntico, mismo Id, header `Idempotent-Replayed: true`, no se duplica el equipo.
3. POST con K1 y cuerpo distinto → 409.
4. POST sin key → 400.
5. POST con nombre duplicado y key nueva → 409.
6. PUT dos veces con el mismo cuerpo → mismo estado y mismo resultado.
7. DELETE dos veces → 204 y 204.
8. DELETE de id inexistente → 404.
9. Registrar resultado dos veces con keys distintas → 201 y luego 409.

## Criterios de aceptación

- [ ] Cada verbo cumple su semántica y códigos documentados.
- [ ] POST sin `Idempotency-Key` es rechazado.
- [ ] Repetir un POST no crea registros duplicados y devuelve la respuesta original.
- [ ] Keys persistidas en base de datos (sobreviven reinicio de la API).
- [ ] Peticiones concurrentes con la misma key no duplican datos.
- [ ] Pruebas unitarias e integración del flujo de idempotencia.
