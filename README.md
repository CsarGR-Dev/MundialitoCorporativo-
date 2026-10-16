# Mundialito Corporativo

Sistema de gestión de un torneo de fútbol corporativo: equipos, integrantes, calendario de partidos, resultados, tabla de posiciones y goleadores.

API en **.NET 10** con **Clean Architecture**, **CQRS** (EF Core para escritura, Dapper para lectura), **Unit of Work**, **Result Pattern**, REST idempotente y observabilidad; base de datos **SQL Server 2022**.

> **Estado:** en desarrollo. Ver [Estado por ítem](#estado-por-ítem) y la [bitácora de cambios](Documentacion/Bitacora.md).

---

## Estructura

```
MundialitoCorporativo/
├── src/
│   ├── MundialitoCorporativo.Domain          Modelos, reglas de negocio, Result Pattern, eventos de dominio
│   ├── MundialitoCorporativo.Application     CQRS: despachador, Commands, Queries, validaciones y contratos
│   ├── MundialitoCorporativo.Infrastructure  EF Core, repositorios, Unit of Work y consultas Dapper
│   └── MundialitoCorporativo.Api             Composition root y endpoints REST
├── tests/
│   ├── MundialitoCorporativo.UnitTests         Arquitectura, dominio, CQRS y Unit of Work
│   └── MundialitoCorporativo.IntegrationTests  Commands y Queries contra SQL Server
├── Db/              Scripts SQL (estructura, vistas y consultas de referencia)
├── Analisis/        Análisis del requerimiento, un documento por ítem
└── Documentacion/   Bitácora de cambios
```

Reglas de dependencia (verificadas con pruebas de arquitectura): Domain no depende de ninguna capa; Application depende solo de Domain; Infrastructure implementa los contratos de Application; Api solo orquesta.

## Requisitos

- .NET SDK 10
- SQL Server 2019 o superior (desarrollo: SQL Server Express con autenticación Windows)

## Base de datos

Ejecutar en SSMS, en orden:

1. `Db/001_DDL_CrearBaseDatos.sql` — base `Mundialito`: tablas, relaciones, restricciones, índices y catálogo de posiciones de juego.
2. `Db/002_VIEW_TablaPosicionesGoleadores.sql` — vistas `vw_TablaPosiciones` y `vw_Goleadores`.

La cadena de conexión de desarrollo está en `src/MundialitoCorporativo.Api/appsettings.Development.json`:

```
Server=.\SQLEXPRESS;Database=Mundialito;Trusted_Connection=True;TrustServerCertificate=True
```

Diseño y diccionario de datos: [Analisis/16_Analisis_Base_Datos.md](Analisis/16_Analisis_Base_Datos.md).

## Compilar, probar y ejecutar

```bash
dotnet build
dotnet test
dotnet run --project src/MundialitoCorporativo.Api      # http://localhost:5000
```

Las pruebas de integración crean una base temporal ejecutando `Db/001` y la eliminan al terminar. Usan la instancia local por defecto; otra instancia se indica con la variable de entorno `MUNDIALITO_TEST_SQLSERVER`. Si no hay un SQL Server disponible, se omiten.

## Convención de nombres

Los **modelos** están en español con el mismo nombre que las tablas y columnas (`Equipo.Nombre`, `Partido.EquipoLocalId`, `EstadoPartido.Jugado`). **Lo técnico** sigue la convención de .NET en inglés (`Entity`, `AggregateRoot`, `Result`, `Create`, `EquipoErrors`, `TablaPosicionesCalculator`). Detalle: [Analisis/17_Convencion_Nombres.md](Analisis/17_Convencion_Nombres.md).

## Estado por ítem

| # | Ítem | Estado |
|---|---|---|
| 2 | Clean Architecture | Implementado: 4 capas y pruebas de arquitectura |
| 3 | CQRS | Implementado: despachador, Commands (EF Core + Unit of Work) y Queries (Dapper) |
| 4 | Unit of Work | Implementado y probado |
| 5 | Result Pattern | Implementado en Domain; mapeo a HTTP pendiente |
| 6 | REST + idempotencia | Pendiente |
| 7 | Observabilidad | Eventos de dominio registrados en `EventosDominio`; logging estructurado y tracing pendientes |
| 8 | Requisitos funcionales | Torneos, equipos, jugadores, partidos, resultados, tabla de posiciones y goleadores implementados en Application; endpoints pendientes |
| 9 | Seed de datos | Pendiente |
| 10 | Filtros y paginación | Implementado en Application e Infrastructure (paginación en SQL, sortBy con lista blanca); endpoints pendientes |
| 11 | Frontend Next.js | Pendiente |
| 12 | Dockerización | Pendiente |
| 13 | Diagrama de arquitectura | Pendiente |
| 14 | Colección de Postman | Pendiente |
| 15 | Pruebas | Tabla de posiciones, puntos, diferencia de gol, Result Pattern, Unit of Work y CQRS cubiertos (unitarias e integración); idempotencia pendiente |

El análisis de cada ítem está en [Analisis/](Analisis/).
