# Ítem 1 – Objetivo

## Requerimiento

Desarrollar una solución completa con enfoque arquitectónico profesional aplicando: Clean Architecture, CQRS, EF Core para escritura, Dapper para lectura, Unit of Work, Result Pattern, REST completo e idempotente, trazabilidad y observabilidad, frontend desacoplado y dockerización.

## Interpretación

El objetivo no es solo que el sistema funcione, sino que cada patrón sea **visible y verificable** en el código. Cada concepto listado debe poder señalarse en un archivo o carpeta concreta durante la presentación.

## Qué debe llevar

| Concepto | Dónde se evidencia |
|---|---|
| Clean Architecture | Cuatro proyectos separados con referencias en una sola dirección (ver ítem 2). |
| CQRS | Carpetas `Commands/` y `Queries/` por módulo en Application; interfaces distintas para cada lado (ver ítem 3). |
| EF Core para escritura | `MundialitoDbContext` y repositorios en Infrastructure, usados solo por Command handlers. |
| Dapper para lectura | `IDbConnectionFactory` + consultas SQL en Infrastructure/ReadModels, usadas solo por Query handlers. |
| Unit of Work | `IUnitOfWork` en Application, `UnitOfWork` en Infrastructure; único punto que llama a `SaveChangesAsync` (ver ítem 4). |
| Result Pattern | `Result` y `Result<T>` en Domain/Common; ningún handler lanza excepciones para flujo de negocio (ver ítem 5). |
| REST completo e idempotente | GET, POST, PUT, PATCH, DELETE correctamente aplicados; header `Idempotency-Key` en POST (ver ítem 6). |
| Trazabilidad y observabilidad | Serilog estructurado, middleware de CorrelationId/TraceId, duración de requests, eventos de dominio (ver ítem 7). |
| Frontend desacoplado | Aplicación Next.js independiente que consume la API solo por HTTP (ver ítem 11). |
| Dockerización | Dockerfile para API y frontend, `docker-compose.yml` con API + DB + Frontend (ver ítem 12). |

## Entregables

- Repositorio público en GitHub con historial de commits progresivo y mensajes descriptivos.
- `README.md` con: descripción, arquitectura, cómo ejecutar (`docker-compose up`), cómo ejecutar pruebas, endpoints, decisiones de diseño y supuestos.
- Diagrama de arquitectura en `docs/arquitectura/`.
- Colección y environment de Postman en `postman/`.
- Proyectos de prueba en `MundialitoCorp_Back/tests/`.

## Criterios de aceptación

- [ ] Cada uno de los 10 conceptos tiene una ubicación clara en el código.
- [ ] El README explica cada decisión en lenguaje propio.
- [ ] El sistema levanta completo con un solo comando.
- [ ] El repositorio es público y accesible.
