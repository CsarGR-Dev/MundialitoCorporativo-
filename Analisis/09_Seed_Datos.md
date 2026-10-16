# Ítem 9 – Seed de Datos (Obligatorio)

## Requerimiento

Automático al iniciar si la base de datos está vacía:
- 4 equipos
- 5 jugadores por equipo
- 6 partidos
- 3 con resultados

Debe estar en Infrastructure, funcionar con Docker y permitir probar la tabla inmediatamente.

## Diseño

### Ubicación

`Infrastructure/Seed/DatabaseSeeder.cs` + `Infrastructure/Seed/SeedData.cs` (datos estáticos).

### Ejecución

1. En el arranque de la API (`Program.cs`) se invoca una extensión `await app.InitializeDatabaseAsync();` definida en Infrastructure.
2. Esta extensión:
   - Aplica migraciones pendientes (`Database.MigrateAsync()`) con política de reintentos (SQL Server puede tardar en estar listo dentro de Docker).
   - Verifica si existen torneos o equipos (`AnyAsync`). Si hay datos, no hace nada (idempotente).
   - Si está vacía, inserta los datos usando las entidades de dominio y sus métodos de fábrica (respetando invariantes) y confirma con el **Unit of Work** en una sola transacción.
3. Registra con log Information: `Seed ejecutado: {Tournaments} torneo, {Teams} equipos, {Players} jugadores, {Matches} partidos`.
4. Se controla con configuración `Database:SeedOnStartup` (true por defecto en Docker y Development).

Se descarta `HasData` de EF Core porque no permite usar la lógica de dominio ni condicionar a “base vacía”.

### Datos propuestos

**Torneo (1)**: *Mundialito Corporativo 2026*, estado `EnCurso`, con inicio en el pasado. Los 4 equipos quedan inscritos en él. El catálogo de posiciones no forma parte del seed: lo crea el script de estructura (`Db/001`) o la migración.

**Equipos (4)**

| Equipo | Abreviatura | Departamento |
|---|---|---|
| Tecnología FC | TEC | Tecnología |
| Finanzas United | FIN | Finanzas |
| Operaciones City | OPE | Operaciones |
| Recursos Humanos Athletic | RRHH | Recursos Humanos |

**Jugadores (5 por equipo = 20)**: un portero, dos defensas, un mediocampista, un delantero; números de camiseta 1, 4, 5, 8, 9.

**Partidos (6)** – todos contra todos a una vuelta:

| # | Local | Visitante | Estado | Marcador |
|---|---|---|---|---|
| 1 | Tecnología FC | Finanzas United | Played | 2 – 1 |
| 2 | Operaciones City | Recursos Humanos Athletic | Played | 1 – 1 |
| 3 | Tecnología FC | Operaciones City | Played | 0 – 3 |
| 4 | Finanzas United | Recursos Humanos Athletic | Scheduled | — |
| 5 | Tecnología FC | Recursos Humanos Athletic | Scheduled | — |
| 6 | Finanzas United | Operaciones City | Scheduled | — |

Los 3 partidos jugados incluyen sus goles con goleador y minuto (total 8 goles), para que la tabla de goleadores tenga datos.

**Tabla esperada después del seed**

| Pos | Equipo | PJ | G | E | P | GF | GC | DG | Pts |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Operaciones City | 2 | 1 | 1 | 0 | 4 | 1 | +3 | 4 |
| 2 | Tecnología FC | 2 | 1 | 0 | 1 | 2 | 4 | −2 | 3 |
| 3 | Recursos Humanos Athletic | 1 | 0 | 1 | 0 | 1 | 1 | 0 | 1 |
| 4 | Finanzas United | 1 | 0 | 0 | 1 | 1 | 2 | −1 | 0 |

Esta tabla queda documentada en el README y sirve como caso de prueba de integración.

Fechas: los partidos jugados con fechas pasadas relativas a la fecha de ejecución; los pendientes con fechas futuras, para que los filtros por fecha se puedan comprobar.

## Docker

- El servicio `api` depende de `db` con `condition: service_healthy`.
- Al ejecutar `docker-compose up` en un volumen nuevo, la API migra y siembra sola.
- Para reiniciar datos: `docker-compose down -v`.

## Criterios de aceptación

- [ ] Con base vacía, al iniciar quedan 1 torneo en curso con 4 equipos inscritos, 20 jugadores, 6 partidos, 3 jugados.
- [ ] Script equivalente en `Db/003_DML_SeedInicial.sql` para cargar los mismos datos desde SSMS.
- [ ] Al reiniciar la API no se duplican datos.
- [ ] El código vive en Infrastructure.
- [ ] Funciona con `docker-compose up` sin pasos manuales.
- [ ] `GET /api/torneos/{id}/tabla-posiciones` devuelve la tabla esperada inmediatamente.
