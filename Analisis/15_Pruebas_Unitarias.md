# Ítem 15 – Pruebas Unitarias (Obligatorio)

## Requerimiento

Mínimo: tabla de posiciones, cálculo de puntos, diferencia de gol, Result Pattern, idempotencia, comportamiento de commit del UnitOfWork.

Valorable: tests de integración.

## Herramientas

| Herramienta | Uso |
|---|---|
| xUnit | Framework de pruebas |
| Aserciones de xUnit | Se descarta FluentAssertions: desde la versión 8 requiere licencia comercial |
| NSubstitute | Dobles de prueba para interfaces |
| EF Core SQLite in-memory | Pruebas de UnitOfWork con base relacional real en memoria |
| Testcontainers (MsSql) | Pruebas de integración contra SQL Server real |
| `WebApplicationFactory<Program>` | Pruebas de API de extremo a extremo |
| NetArchTest | Pruebas de reglas de arquitectura |
| coverlet | Cobertura de código |

## Proyectos

```
MundialitoCorp_Back/tests/
├── MundialitoCorporativo.UnitTests/
│   ├── Domain/
│   │   ├── Standings/TablaPosicionesCalculatorTests.cs
│   │   ├── Matches/MatchTests.cs
│   │   ├── Teams/TeamTests.cs
│   │   └── Common/ResultTests.cs
│   ├── Application/
│   │   ├── Teams/CreateEquipoCommandHandlerTests.cs
│   │   ├── Matches/RegisterPartidoResultCommandHandlerTests.cs
│   │   └── Paging/PagedResponseTests.cs
│   ├── Infrastructure/
│   │   ├── UnitOfWorkTests.cs
│   │   └── IdempotencyStoreTests.cs
│   ├── Api/
│   │   ├── IdempotencyFilterTests.cs
│   │   └── ResultExtensionsTests.cs
│   └── Architecture/ArchitectureTests.cs
└── MundialitoCorporativo.IntegrationTests/
    ├── ApiFactory.cs (Testcontainers + WebApplicationFactory)
    ├── TeamsEndpointsTests.cs
    ├── MatchesEndpointsTests.cs
    ├── StandingsQueryTests.cs
    └── IdempotencyTests.cs
```

Convención de nombres: `Metodo_Escenario_ResultadoEsperado`. Estructura Arrange / Act / Assert.

## Casos mínimos

### Cálculo de puntos
- Victoria otorga 3 puntos al ganador y 0 al perdedor.
- Empate otorga 1 punto a cada equipo.
- Partidos `Programado` o `Cancelado` no suman puntos.

### Diferencia de gol
- DG = GF − GC acumulado en varios partidos.
- DG negativa se calcula correctamente.
- Equipo sin partidos tiene DG 0.

### Tabla de posiciones
- Ordena por puntos descendente.
- Con empate en puntos, ordena por diferencia de gol.
- Con empate en puntos y DG, ordena por goles a favor.
- Con empate total, ordena por nombre (determinista).
- Incluye equipos sin partidos jugados.
- Caso del seed: produce la tabla documentada en el ítem 9.
- Uso de `[Theory]` + `[MemberData]` para varios escenarios.

### Result Pattern
- Success sin error; Failure con código y mensaje.
- No permite estados incoherentes.
- `Value` en Failure lanza `InvalidOperationException`.
- Conversiones implícitas.
- Mapeo de `ErrorType` → 400/404/409 y éxito → 200/201/204.

### Idempotencia
- Primera petición con key ejecuta el handler y guarda la respuesta.
- Segunda petición con misma key y mismo cuerpo no ejecuta el handler y devuelve la respuesta guardada.
- Misma key con cuerpo distinto devuelve 409.
- Sin key devuelve 400.
- Key en estado `EnProceso` devuelve 409.
- Respuesta 5xx libera la key.

### UnitOfWork – comportamiento del commit
- Sin `CommitAsync` no se persisten cambios.
- `CommitAsync` persiste cambios de varios repositorios en una sola transacción.
- Fallo en `SaveChanges` → rollback; nada persistido.
- Conflicto de concurrencia → `Result.Failure` tipo Conflict.
- Violación de índice único → `Result.Failure` tipo Conflict.
- Eventos de dominio guardados en `EventosDominio` y despachados solo tras commit exitoso.
- Handler de comando invoca `CommitAsync` exactamente una vez (verificación con NSubstitute).
- Handler que falla por regla de negocio no invoca `CommitAsync`.

### Dominio adicional
- `Partido.RegisterResult` en partido ya jugado → error `Match.AlreadyPlayed`.
- Goles que no cuadran con el marcador → error de validación.
- `Equipo.Create` genera `EquipoCreatedDomainEvent`.
- `Partido.RegisterResult` genera `PartidoResultRegisteredDomainEvent`.

## Integración (valorable)

- Levantar SQL Server con Testcontainers, aplicar migraciones y seed.
- `GET /api/torneos/{id}/tabla-posiciones` devuelve la tabla esperada.
- Filtros y paginación con `OFFSET/FETCH` devuelven totales correctos.
- POST repetido con misma key no duplica filas en la base.
- Flujo completo: crear equipos → programar → registrar resultado → tabla actualizada.

## Ejecución

```
dotnet test
dotnet test --collect:"XPlat Code Coverage"
```

Opcional: workflow de GitHub Actions que compile y ejecute pruebas en cada push.

### Catálogo de pruebas

Al terminar `dotnet test`, cada proyecto de pruebas genera su catálogo en texto (`Catalogo_Pruebas_Unitarias.txt` y `Catalogo_Pruebas_Integracion.txt`):

- resultado global y resumen por grupo (casos, aprobadas, fallidas y omitidas);
- por cada prueba: estado, componente probado, objetivo, duración y variantes de las `[Theory]`;
- motivo del fallo o de la omisión.

| Pieza | Qué hace |
|---|---|
| `tests/Directory.Build.targets` | Pide a VSTest el resultado en `.trx` y, al terminar, ejecuta el generador. Si una prueba falla, `dotnet test` termina con error igual, pero el catálogo se genera y muestra la falla. |
| `tests/Generar_Catalogo_Pruebas.ps1` | Cruza el `.trx` con las descripciones y escribe el informe. También se puede ejecutar a mano. |
| `Catalogo_Descripciones.txt` (en cada proyecto) | Una línea por método: `Clase\|Metodo\|Componente\|Objetivo`. Una prueba sin su línea aparece como *SIN DESCRIPCION*. |

Los catálogos generados y los `.trx` no se versionan, porque cambian en cada ejecución. Para no generarlos: `dotnet test -p:GenerarCatalogoPruebas=false`.

## Criterios de aceptación

- [ ] Los seis grupos mínimos tienen pruebas.
- [ ] Todas las pruebas pasan con `dotnet test`.
- [ ] Al menos un conjunto de pruebas de integración contra SQL Server real.
- [ ] Pruebas de arquitectura validan las reglas del ítem 2.
