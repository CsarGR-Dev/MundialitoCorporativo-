# Ítem 5 – Result Pattern (Obligatorio)

**Criterio transversal: impacta Clean Architecture, REST e idempotencia.**

## Requerimiento

No se permite usar excepciones como control de flujo.

Debe implementarse `Result` y `Result<T>` con: Success, Failure, Error Message, Error Code opcional.

La API debe mapear correctamente: 200 OK, 201 Created, 400 BadRequest, 404 NotFound, 409 Conflict.

## Diseño (Domain/Common)

### Error

```csharp
public enum ErrorType { None, Validation, NotFound, Conflict, Failure }

// Mensaje obligatorio, código opcional (textual del requerimiento: "Error Message, Error Code opcional").
public record Error(ErrorType Type, string Message, string? Code = null)
{
    public static readonly Error None = new(ErrorType.None, string.Empty);

    public static Error Validation(string message, string? code = null) => new(ErrorType.Validation, message, code);
    public static Error NotFound(string message, string? code = null)   => new(ErrorType.NotFound, message, code);
    public static Error Conflict(string message, string? code = null)   => new(ErrorType.Conflict, message, code);
    public static Error Failure(string message, string? code = null)    => new(ErrorType.Failure, message, code);
}
```

`Result` expone además los atajos `ErrorMessage` y `ErrorCode` (nulos cuando el resultado es exitoso). Implementación real: `src/MundialitoCorporativo.Domain/Common/`.

### Result y Result&lt;T&gt;

```csharp
public class Result
{
    protected Result(bool isSuccess, Error error) { ... } // valida coherencia éxito/error
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    public static Result<T> Success<T>(T value) => new(value, true, Error.None);
    public static Result<T> Failure<T>(Error error) => new(default, false, error);
}

public sealed class Result<T> : Result
{
    public T Value => IsSuccess ? _value! : throw new InvalidOperationException(...);
    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure<T>(error);
}
```

- `ErrorMessage` = `Error.Message`; `ErrorCode` = `Error.Code` (opcional, puede quedar vacío).
- Para validaciones con varios campos: `ValidationError : Error` con un arreglo de errores por propiedad.
- Acceder a `Value` en un resultado fallido es un error de programación (no de flujo), por eso sí lanza.

### Catálogo de errores por módulo

```csharp
public static class EquipoErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound($"No existe el equipo '{id}'.", "Equipo.NotFound");

    public static Error DuplicateNombre(string nombre) =>
        Error.Conflict($"Ya existe un equipo con el nombre '{nombre}'.", "Equipo.DuplicateNombre");

    public static readonly Error HasPartidos =
        Error.Conflict("El equipo tiene partidos asociados y no puede eliminarse.", "Equipo.HasPartidos");
}
```

Catálogos equivalentes: `TorneoErrors`, `JugadorErrors`, `PartidoErrors`, `PosicionJuegoErrors` (Domain) e `IdempotencyErrors` (Api, porque la idempotencia es un asunto HTTP).

## Mapeo a HTTP (Api/Http/ErrorHttpMapping.cs y ApiProblemDetails.cs)

| Situación | Código HTTP |
|---|---|
| GET exitoso, PUT/PATCH exitoso con cuerpo | 200 OK |
| POST exitoso | 201 Created + header `Location` |
| DELETE exitoso | 204 No Content |
| `ErrorType.Validation` | 400 Bad Request |
| `ErrorType.NotFound` | 404 Not Found |
| `ErrorType.Conflict` | 409 Conflict |
| `ErrorType.Failure` (y `None`, que sería un error de programación) | 500 (error técnico controlado) |

Formato de error: **ProblemDetails (RFC 9457)**, incluyendo extensiones:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.10",
  "title": "Conflict",
  "status": 409,
  "detail": "Ya existe un equipo con el nombre 'Finanzas FC'.",
  "instance": "/api/equipos",
  "traceId": "4bf92f3577b34da6a3ce929d0e0e4736",
  "correlationId": "6d1f0b8e-2c4a-4f7e-9a3b-5e8d7c6b4a21",
  "errorCode": "Equipo.DuplicateNombre"
}
```

El `traceId` es el mismo del header `X-Trace-Id` y de los logs del request; el `correlationId`, el del header `X-Correlation-Id` (ítem 7).

Para 400 con múltiples campos se usa `ValidationProblemDetails` con el diccionario `errors`.

## Reglas de uso

- Métodos de dominio devuelven `Result` / `Result<T>`.
- Handlers devuelven `Result` / `Result<T>`.
- Controladores solo traducen el `Result` a respuesta HTTP con los métodos de `ApiController` (`QueryAsync`, `CreatedAsync`, `SendThenQueryAsync`, `SendNoContentAsync`, `ErrorResult`).
- Los errores de formato que detecta MVC antes del despachador (JSON mal formado, texto en un campo numérico) responden con el mismo formato: 400, `ValidationProblemDetails` y `errorCode = Validation.Failed`.
- El middleware global de excepciones solo atrapa errores **inesperados** (500) y los registra con nivel Error.

## Pruebas requeridas (ver ítem 15)

- `Result.Success()` → `IsSuccess = true`, `Error = None`.
- `Result.Failure(error)` → `IsFailure = true`, conserva código y mensaje.
- No se puede crear un éxito con error ni un fallo sin error.
- Acceder a `Value` en fallo lanza `InvalidOperationException`.
- Conversiones implícitas desde `T` y desde `Error`.
- Mapeo de cada `ErrorType` al código HTTP esperado.

## Criterios de aceptación

- [ ] `Result` y `Result<T>` implementados en Domain.
- [ ] Ningún `throw` en handlers ni entidades para reglas de negocio.
- [ ] Los cinco códigos HTTP exigidos se producen en escenarios reales reproducibles en Postman.
- [ ] Respuestas de error con ProblemDetails, `errorCode` y `traceId`.

## Implementación real

- **Result Pattern** en `Domain/Common/` (`Result`, `Result<T>`, `Error`, `ErrorType`, `ValidationError`); los catálogos de errores viven junto a cada modelo (`EquipoErrors`, `TorneoErrors`, …).
- **Mapeo a HTTP**: `Api/Http/ErrorHttpMapping.cs` (tipo de error → código) y `Api/Http/ApiProblemDetails.cs` (ProblemDetails con `errorCode`, `traceId` e `instance`; `ValidationProblemDetails` con `errors` por campo).
- **Controladores**: heredan de `Api/Controllers/ApiController.cs`, cuyos métodos convierten el `Result` en 200, 201 (con `Location`), 204 o el error correspondiente. No hay `try/catch` en los controladores.
- **Errores inesperados**: `Api/Http/GlobalExceptionHandler.cs` (`IExceptionHandler`) los registra con nivel Error y responde 500 sin exponer detalles internos (`errorCode = Server.Unexpected`).
- **Pruebas**: `UnitTests/Api/ErrorHttpMappingTests.cs` (cada `ErrorType` y formato ProblemDetails) e `IntegrationTests/Api/ApiSqlServerTests.cs` (400, 404 y 409 reales por HTTP).
