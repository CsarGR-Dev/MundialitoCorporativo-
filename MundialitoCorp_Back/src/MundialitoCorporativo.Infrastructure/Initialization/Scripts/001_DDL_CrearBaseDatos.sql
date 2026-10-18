/* =====================================================================================
   Script      : 001_DDL_CrearBaseDatos.sql
   Proyecto    : Mundialito de Fútbol Corporativo
   Motor       : SQL Server 2022 (compatible 2019+)
   Fecha       : 2026-10-16
   Autor       : César González
   Descripción : Crea la base de datos Mundialito con todas sus tablas, llaves primarias,
                 llaves foráneas, restricciones de validación, índices y datos de catálogo.

   Catálogos              : PosicionesJuego
   Tablas de negocio      : Torneos, Equipos, TorneoEquipos, Jugadores, Partidos, Goles
   Tablas transversales   : RegistrosIdempotencia, EventosDominio

   Notas:
   - Script re-ejecutable: solo crea los objetos que no existen.
   - Todas las fechas se almacenan en UTC.
   - Catálogos extensibles (PosicionesJuego) en tabla; estados ligados a reglas de negocio
     (estado de torneo y de partido) como texto con CHECK.
   - Eliminación lógica (Eliminado) en Torneos, Equipos, Jugadores y Partidos.
   - Ejecutar completo en SSMS (F5).
   ===================================================================================== */

SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/* -------------------------------------------------------------------------------------
   1. Base de datos
   ------------------------------------------------------------------------------------- */
IF DB_ID(N'Mundialito') IS NULL
BEGIN
    CREATE DATABASE Mundialito COLLATE Modern_Spanish_CI_AS;
END
GO

USE Mundialito;
GO

/* -------------------------------------------------------------------------------------
   2. PosicionesJuego (Catálogo de posiciones de juego)
   ------------------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.PosicionesJuego', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PosicionesJuego
    (
        Id                  TINYINT             NOT NULL,
        Nombre              NVARCHAR(30)        NOT NULL,
        Abreviatura         NVARCHAR(5)         NOT NULL,
        Orden               TINYINT             NOT NULL,
        Activo              BIT                 NOT NULL CONSTRAINT DF_Posiciones_Activo DEFAULT (1),

        CONSTRAINT PK_PosicionesJuego PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_Posiciones_Nombre      UNIQUE (Nombre),
        CONSTRAINT UQ_Posiciones_Abreviatura UNIQUE (Abreviatura),
        CONSTRAINT CK_Posiciones_Abreviatura_Mayus CHECK (Abreviatura COLLATE Latin1_General_CS_AS = UPPER(Abreviatura))
    );
END
GO

/* -------------------------------------------------------------------------------------
   3. Torneos (Ligas / ediciones del Mundialito)
   ------------------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.Torneos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Torneos
    (
        Id                  UNIQUEIDENTIFIER    NOT NULL CONSTRAINT DF_Torneos_Id            DEFAULT NEWSEQUENTIALID(),
        Nombre              NVARCHAR(100)       NOT NULL,
        Descripcion         NVARCHAR(250)       NULL,
        FechaInicio         DATE                NOT NULL,
        FechaFin            DATE                NULL,
        Estado              VARCHAR(20)         NOT NULL CONSTRAINT DF_Torneos_Estado        DEFAULT ('Planificado'),
        Eliminado           BIT                 NOT NULL CONSTRAINT DF_Torneos_Eliminado     DEFAULT (0),
        FechaEliminacion    DATETIME2(3)        NULL,
        FechaCreacion       DATETIME2(3)        NOT NULL CONSTRAINT DF_Torneos_FechaCreacion DEFAULT SYSUTCDATETIME(),
        FechaModificacion   DATETIME2(3)        NULL,
        VersionFila         ROWVERSION          NOT NULL,

        CONSTRAINT PK_Torneos PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_Torneos_Nombre_Longitud CHECK (LEN(Nombre) >= 3),
        CONSTRAINT CK_Torneos_Fechas          CHECK (FechaFin IS NULL OR FechaFin >= FechaInicio),
        CONSTRAINT CK_Torneos_Estado          CHECK (Estado IN ('Planificado', 'EnCurso', 'Finalizado')),
        CONSTRAINT CK_Torneos_Eliminado       CHECK ((Eliminado = 0 AND FechaEliminacion IS NULL) OR (Eliminado = 1 AND FechaEliminacion IS NOT NULL))
    );
END
GO

/* -------------------------------------------------------------------------------------
   4. Equipos
   ------------------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.Equipos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Equipos
    (
        Id                  UNIQUEIDENTIFIER    NOT NULL CONSTRAINT DF_Equipos_Id            DEFAULT NEWSEQUENTIALID(),
        Nombre              NVARCHAR(100)       NOT NULL,
        Abreviatura         NVARCHAR(5)         NOT NULL,
        Departamento        NVARCHAR(100)       NULL,
        Eliminado           BIT                 NOT NULL CONSTRAINT DF_Equipos_Eliminado     DEFAULT (0),
        FechaEliminacion    DATETIME2(3)        NULL,
        FechaCreacion       DATETIME2(3)        NOT NULL CONSTRAINT DF_Equipos_FechaCreacion DEFAULT SYSUTCDATETIME(),
        FechaModificacion   DATETIME2(3)        NULL,
        VersionFila         ROWVERSION          NOT NULL,

        CONSTRAINT PK_Equipos PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_Equipos_Nombre_Longitud      CHECK (LEN(Nombre) >= 3),
        CONSTRAINT CK_Equipos_Abreviatura_Longitud CHECK (LEN(Abreviatura) BETWEEN 2 AND 5),
        CONSTRAINT CK_Equipos_Abreviatura_Mayus    CHECK (Abreviatura COLLATE Latin1_General_CS_AS = UPPER(Abreviatura)),
        CONSTRAINT CK_Equipos_Eliminado            CHECK ((Eliminado = 0 AND FechaEliminacion IS NULL) OR (Eliminado = 1 AND FechaEliminacion IS NOT NULL))
    );
END
GO

/* -------------------------------------------------------------------------------------
   5. TorneoEquipos (Inscripción de equipos en un torneo)
   ------------------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.TorneoEquipos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TorneoEquipos
    (
        TorneoId            UNIQUEIDENTIFIER    NOT NULL,
        EquipoId            UNIQUEIDENTIFIER    NOT NULL,
        FechaInscripcion    DATETIME2(3)        NOT NULL CONSTRAINT DF_TorneoEquipos_FechaInscripcion DEFAULT SYSUTCDATETIME(),

        -- Un equipo se inscribe una sola vez por torneo.
        CONSTRAINT PK_TorneoEquipos PRIMARY KEY CLUSTERED (TorneoId, EquipoId),
        CONSTRAINT FK_TorneoEquipos_Torneos FOREIGN KEY (TorneoId) REFERENCES dbo.Torneos (Id),
        CONSTRAINT FK_TorneoEquipos_Equipos FOREIGN KEY (EquipoId) REFERENCES dbo.Equipos (Id)
    );
END
GO

/* -------------------------------------------------------------------------------------
   6. Jugadores (Integrantes de equipos)
   ------------------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.Jugadores', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Jugadores
    (
        Id                  UNIQUEIDENTIFIER    NOT NULL CONSTRAINT DF_Jugadores_Id            DEFAULT NEWSEQUENTIALID(),
        EquipoId            UNIQUEIDENTIFIER    NOT NULL,
        PosicionJuegoId     TINYINT             NOT NULL,
        Nombres             NVARCHAR(60)        NOT NULL,
        Apellidos           NVARCHAR(60)        NOT NULL,
        NumeroCamiseta      TINYINT             NOT NULL,
        Activo              BIT                 NOT NULL CONSTRAINT DF_Jugadores_Activo        DEFAULT (1),
        Eliminado           BIT                 NOT NULL CONSTRAINT DF_Jugadores_Eliminado     DEFAULT (0),
        FechaEliminacion    DATETIME2(3)        NULL,
        FechaCreacion       DATETIME2(3)        NOT NULL CONSTRAINT DF_Jugadores_FechaCreacion DEFAULT SYSUTCDATETIME(),
        FechaModificacion   DATETIME2(3)        NULL,
        VersionFila         ROWVERSION          NOT NULL,

        CONSTRAINT PK_Jugadores PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Jugadores_Equipos    FOREIGN KEY (EquipoId)   REFERENCES dbo.Equipos (Id),
        CONSTRAINT FK_Jugadores_PosicionesJuego FOREIGN KEY (PosicionJuegoId) REFERENCES dbo.PosicionesJuego (Id),
        -- Permite que Goles valide con una FK compuesta que el goleador pertenece al equipo.
        CONSTRAINT UQ_Jugadores_Id_EquipoId UNIQUE (Id, EquipoId),
        CONSTRAINT CK_Jugadores_Nombres_Longitud   CHECK (LEN(Nombres) >= 2),
        CONSTRAINT CK_Jugadores_Apellidos_Longitud CHECK (LEN(Apellidos) >= 2),
        CONSTRAINT CK_Jugadores_NumeroCamiseta     CHECK (NumeroCamiseta BETWEEN 1 AND 99),
        CONSTRAINT CK_Jugadores_Eliminado          CHECK ((Eliminado = 0 AND FechaEliminacion IS NULL) OR (Eliminado = 1 AND FechaEliminacion IS NOT NULL))
    );
END
GO

/* -------------------------------------------------------------------------------------
   7. Partidos (Calendario y resultado)
   ------------------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.Partidos', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Partidos
    (
        Id                  UNIQUEIDENTIFIER    NOT NULL CONSTRAINT DF_Partidos_Id            DEFAULT NEWSEQUENTIALID(),
        TorneoId            UNIQUEIDENTIFIER    NOT NULL,
        EquipoLocalId       UNIQUEIDENTIFIER    NOT NULL,
        EquipoVisitanteId   UNIQUEIDENTIFIER    NOT NULL,
        FechaProgramada     DATETIME2(0)        NOT NULL,
        Sede                NVARCHAR(100)       NULL,
        Estado              VARCHAR(20)         NOT NULL CONSTRAINT DF_Partidos_Estado        DEFAULT ('Programado'),
        GolesLocal          TINYINT             NULL,
        GolesVisitante      TINYINT             NULL,
        FechaJugado         DATETIME2(3)        NULL,
        Eliminado           BIT                 NOT NULL CONSTRAINT DF_Partidos_Eliminado     DEFAULT (0),
        FechaEliminacion    DATETIME2(3)        NULL,
        FechaCreacion       DATETIME2(3)        NOT NULL CONSTRAINT DF_Partidos_FechaCreacion DEFAULT SYSUTCDATETIME(),
        FechaModificacion   DATETIME2(3)        NULL,
        VersionFila         ROWVERSION          NOT NULL,

        CONSTRAINT PK_Partidos PRIMARY KEY CLUSTERED (Id),
        -- FK compuestas: ambos equipos deben estar inscritos en el torneo del partido.
        CONSTRAINT FK_Partidos_TorneoEquipoLocal     FOREIGN KEY (TorneoId, EquipoLocalId)     REFERENCES dbo.TorneoEquipos (TorneoId, EquipoId),
        CONSTRAINT FK_Partidos_TorneoEquipoVisitante FOREIGN KEY (TorneoId, EquipoVisitanteId) REFERENCES dbo.TorneoEquipos (TorneoId, EquipoId),
        CONSTRAINT CK_Partidos_EquiposDistintos CHECK (EquipoLocalId <> EquipoVisitanteId),
        CONSTRAINT CK_Partidos_Estado           CHECK (Estado IN ('Programado', 'Jugado', 'Cancelado')),
        -- Un partido jugado tiene marcador y fecha de juego; uno no jugado no tiene marcador.
        CONSTRAINT CK_Partidos_Resultado CHECK (
               (Estado = 'Jugado'  AND GolesLocal IS NOT NULL AND GolesVisitante IS NOT NULL AND FechaJugado IS NOT NULL)
            OR (Estado <> 'Jugado' AND GolesLocal IS NULL     AND GolesVisitante IS NULL     AND FechaJugado IS NULL)
        ),
        -- Un partido jugado no puede eliminarse.
        CONSTRAINT CK_Partidos_Eliminado CHECK (
               (Eliminado = 0 AND FechaEliminacion IS NULL)
            OR (Eliminado = 1 AND FechaEliminacion IS NOT NULL AND Estado <> 'Jugado')
        )
    );
END
GO

/* -------------------------------------------------------------------------------------
   8. Goles (Goleadores)
   ------------------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.Goles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Goles
    (
        Id                  UNIQUEIDENTIFIER    NOT NULL CONSTRAINT DF_Goles_Id            DEFAULT NEWSEQUENTIALID(),
        PartidoId           UNIQUEIDENTIFIER    NOT NULL,
        JugadorId           UNIQUEIDENTIFIER    NOT NULL,
        EquipoId            UNIQUEIDENTIFIER    NOT NULL,
        Minuto              TINYINT             NOT NULL,
        FechaCreacion       DATETIME2(3)        NOT NULL CONSTRAINT DF_Goles_FechaCreacion DEFAULT SYSUTCDATETIME(),

        CONSTRAINT PK_Goles PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Goles_Partidos FOREIGN KEY (PartidoId) REFERENCES dbo.Partidos (Id),
        CONSTRAINT FK_Goles_Equipos  FOREIGN KEY (EquipoId)  REFERENCES dbo.Equipos (Id),
        -- FK compuesta: el jugador debe pertenecer al equipo al que se acredita el gol.
        CONSTRAINT FK_Goles_Jugadores_Equipo FOREIGN KEY (JugadorId, EquipoId) REFERENCES dbo.Jugadores (Id, EquipoId),
        CONSTRAINT CK_Goles_Minuto CHECK (Minuto BETWEEN 1 AND 130)
    );
END
GO

/* -------------------------------------------------------------------------------------
   9. RegistrosIdempotencia (Almacenamiento de Idempotency-Key)
   ------------------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.RegistrosIdempotencia', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RegistrosIdempotencia
    (
        ClaveIdempotencia       NVARCHAR(100)   NOT NULL,
        RutaSolicitud           NVARCHAR(300)   NOT NULL,
        MetodoSolicitud         VARCHAR(10)     NOT NULL,
        HashSolicitud           CHAR(64)        NOT NULL,
        Estado                  VARCHAR(20)     NOT NULL CONSTRAINT DF_RegistrosIdempotencia_Estado        DEFAULT ('EnProceso'),
        CodigoEstadoRespuesta   SMALLINT        NULL,
        CuerpoRespuesta         NVARCHAR(MAX)   NULL,
        EncabezadosRespuesta    NVARCHAR(MAX)   NULL,
        FechaCreacion           DATETIME2(3)    NOT NULL CONSTRAINT DF_RegistrosIdempotencia_FechaCreacion DEFAULT SYSUTCDATETIME(),
        FechaCompletado         DATETIME2(3)    NULL,
        FechaExpiracion         DATETIME2(3)    NOT NULL,

        -- La PK única impide que dos peticiones concurrentes registren la misma clave.
        CONSTRAINT PK_RegistrosIdempotencia PRIMARY KEY CLUSTERED (ClaveIdempotencia, RutaSolicitud),
        CONSTRAINT CK_RegistrosIdempotencia_Estado CHECK (Estado IN ('EnProceso', 'Completado')),
        CONSTRAINT CK_RegistrosIdempotencia_Metodo CHECK (MetodoSolicitud IN ('POST', 'PUT', 'PATCH', 'DELETE')),
        CONSTRAINT CK_RegistrosIdempotencia_Completado CHECK (
               (Estado = 'EnProceso'  AND CodigoEstadoRespuesta IS NULL     AND FechaCompletado IS NULL)
            OR (Estado = 'Completado' AND CodigoEstadoRespuesta IS NOT NULL AND FechaCompletado IS NOT NULL)
        ),
        CONSTRAINT CK_RegistrosIdempotencia_Encabezados CHECK (EncabezadosRespuesta IS NULL OR ISJSON(EncabezadosRespuesta) = 1),
        CONSTRAINT CK_RegistrosIdempotencia_Expiracion  CHECK (FechaExpiracion > FechaCreacion)
    );
END
GO

/* -------------------------------------------------------------------------------------
   10. EventosDominio (Registro de eventos de dominio)
   ------------------------------------------------------------------------------------- */
IF OBJECT_ID(N'dbo.EventosDominio', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EventosDominio
    (
        Id                  UNIQUEIDENTIFIER    NOT NULL,
        TipoEvento          VARCHAR(150)        NOT NULL,
        TipoAgregado        VARCHAR(100)        NOT NULL,
        AgregadoId          UNIQUEIDENTIFIER    NOT NULL,
        DatosEvento         NVARCHAR(MAX)       NOT NULL,
        FechaOcurrencia     DATETIME2(3)        NOT NULL,
        TraceId             VARCHAR(32)         NULL,
        CorrelationId       VARCHAR(64)         NULL,
        FechaProcesado      DATETIME2(3)        NULL,

        CONSTRAINT PK_EventosDominio PRIMARY KEY NONCLUSTERED (Id),
        CONSTRAINT CK_EventosDominio_DatosEvento CHECK (ISJSON(DatosEvento) = 1)
    );

    -- Los eventos se consultan por orden cronológico: índice agrupado por fecha.
    CREATE CLUSTERED INDEX CIX_EventosDominio_FechaOcurrencia ON dbo.EventosDominio (FechaOcurrencia);
END
GO

/* -------------------------------------------------------------------------------------
   11. Índices
   ------------------------------------------------------------------------------------- */

-- Torneos: nombre único entre torneos no eliminados.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Torneos_Nombre' AND object_id = OBJECT_ID(N'dbo.Torneos'))
    CREATE UNIQUE NONCLUSTERED INDEX UX_Torneos_Nombre ON dbo.Torneos (Nombre) WHERE Eliminado = 0;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Torneos_Estado' AND object_id = OBJECT_ID(N'dbo.Torneos'))
    CREATE NONCLUSTERED INDEX IX_Torneos_Estado ON dbo.Torneos (Estado, FechaInicio) INCLUDE (Nombre) WHERE Eliminado = 0;
GO

-- Equipos: nombre y abreviatura únicos entre equipos no eliminados (intercalación CI → sin distinguir mayúsculas).
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Equipos_Nombre' AND object_id = OBJECT_ID(N'dbo.Equipos'))
    CREATE UNIQUE NONCLUSTERED INDEX UX_Equipos_Nombre ON dbo.Equipos (Nombre) WHERE Eliminado = 0;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Equipos_Abreviatura' AND object_id = OBJECT_ID(N'dbo.Equipos'))
    CREATE UNIQUE NONCLUSTERED INDEX UX_Equipos_Abreviatura ON dbo.Equipos (Abreviatura) WHERE Eliminado = 0;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Equipos_Departamento' AND object_id = OBJECT_ID(N'dbo.Equipos'))
    CREATE NONCLUSTERED INDEX IX_Equipos_Departamento ON dbo.Equipos (Departamento) INCLUDE (Nombre, Abreviatura) WHERE Eliminado = 0;
GO

-- TorneoEquipos: torneos en los que participa un equipo.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_TorneoEquipos_EquipoId' AND object_id = OBJECT_ID(N'dbo.TorneoEquipos'))
    CREATE NONCLUSTERED INDEX IX_TorneoEquipos_EquipoId ON dbo.TorneoEquipos (EquipoId);
GO

-- Jugadores: número de camiseta único por equipo; búsqueda por equipo, apellido y posición.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Jugadores_Equipo_NumeroCamiseta' AND object_id = OBJECT_ID(N'dbo.Jugadores'))
    CREATE UNIQUE NONCLUSTERED INDEX UX_Jugadores_Equipo_NumeroCamiseta ON dbo.Jugadores (EquipoId, NumeroCamiseta) WHERE Eliminado = 0;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Jugadores_Equipo_Apellidos' AND object_id = OBJECT_ID(N'dbo.Jugadores'))
    CREATE NONCLUSTERED INDEX IX_Jugadores_Equipo_Apellidos ON dbo.Jugadores (EquipoId, Apellidos, Nombres)
        INCLUDE (NumeroCamiseta, PosicionJuegoId, Activo) WHERE Eliminado = 0;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Jugadores_Apellidos' AND object_id = OBJECT_ID(N'dbo.Jugadores'))
    CREATE NONCLUSTERED INDEX IX_Jugadores_Apellidos ON dbo.Jugadores (Apellidos, Nombres) WHERE Eliminado = 0;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Jugadores_PosicionJuegoId' AND object_id = OBJECT_ID(N'dbo.Jugadores'))
    CREATE NONCLUSTERED INDEX IX_Jugadores_PosicionJuegoId ON dbo.Jugadores (PosicionJuegoId) WHERE Eliminado = 0;
GO

-- Partidos: un equipo no puede tener dos partidos activos a la misma hora (como local o como visitante).
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Partidos_EquipoLocal_FechaProgramada' AND object_id = OBJECT_ID(N'dbo.Partidos'))
    CREATE UNIQUE NONCLUSTERED INDEX UX_Partidos_EquipoLocal_FechaProgramada ON dbo.Partidos (EquipoLocalId, FechaProgramada)
        WHERE Eliminado = 0 AND Estado IN ('Programado', 'Jugado');
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_Partidos_EquipoVisitante_FechaProgramada' AND object_id = OBJECT_ID(N'dbo.Partidos'))
    CREATE UNIQUE NONCLUSTERED INDEX UX_Partidos_EquipoVisitante_FechaProgramada ON dbo.Partidos (EquipoVisitanteId, FechaProgramada)
        WHERE Eliminado = 0 AND Estado IN ('Programado', 'Jugado');
GO
-- Tabla de posiciones y calendario por torneo.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Partidos_Torneo_Estado' AND object_id = OBJECT_ID(N'dbo.Partidos'))
    CREATE NONCLUSTERED INDEX IX_Partidos_Torneo_Estado ON dbo.Partidos (TorneoId, Estado)
        INCLUDE (EquipoLocalId, EquipoVisitanteId, GolesLocal, GolesVisitante) WHERE Eliminado = 0;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Partidos_Torneo_FechaProgramada' AND object_id = OBJECT_ID(N'dbo.Partidos'))
    CREATE NONCLUSTERED INDEX IX_Partidos_Torneo_FechaProgramada ON dbo.Partidos (TorneoId, FechaProgramada)
        INCLUDE (EquipoLocalId, EquipoVisitanteId, Estado, GolesLocal, GolesVisitante, Sede) WHERE Eliminado = 0;
GO
-- Filtro por fecha y estado sin torneo.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Partidos_FechaProgramada' AND object_id = OBJECT_ID(N'dbo.Partidos'))
    CREATE NONCLUSTERED INDEX IX_Partidos_FechaProgramada ON dbo.Partidos (FechaProgramada)
        INCLUDE (TorneoId, EquipoLocalId, EquipoVisitanteId, Estado, GolesLocal, GolesVisitante, Sede) WHERE Eliminado = 0;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Partidos_Estado' AND object_id = OBJECT_ID(N'dbo.Partidos'))
    CREATE NONCLUSTERED INDEX IX_Partidos_Estado ON dbo.Partidos (Estado, FechaProgramada)
        INCLUDE (TorneoId, EquipoLocalId, EquipoVisitanteId, GolesLocal, GolesVisitante) WHERE Eliminado = 0;
GO

-- Goles: goleadores (agrupación por jugador) y detalle de goles por partido.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Goles_JugadorId' AND object_id = OBJECT_ID(N'dbo.Goles'))
    CREATE NONCLUSTERED INDEX IX_Goles_JugadorId ON dbo.Goles (JugadorId) INCLUDE (EquipoId, PartidoId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Goles_PartidoId' AND object_id = OBJECT_ID(N'dbo.Goles'))
    CREATE NONCLUSTERED INDEX IX_Goles_PartidoId ON dbo.Goles (PartidoId) INCLUDE (JugadorId, EquipoId, Minuto);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Goles_EquipoId' AND object_id = OBJECT_ID(N'dbo.Goles'))
    CREATE NONCLUSTERED INDEX IX_Goles_EquipoId ON dbo.Goles (EquipoId);
GO

-- RegistrosIdempotencia: limpieza periódica de registros expirados.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RegistrosIdempotencia_FechaExpiracion' AND object_id = OBJECT_ID(N'dbo.RegistrosIdempotencia'))
    CREATE NONCLUSTERED INDEX IX_RegistrosIdempotencia_FechaExpiracion ON dbo.RegistrosIdempotencia (FechaExpiracion);
GO

-- EventosDominio: trazabilidad por agregado y por TraceId.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_EventosDominio_Agregado' AND object_id = OBJECT_ID(N'dbo.EventosDominio'))
    CREATE NONCLUSTERED INDEX IX_EventosDominio_Agregado ON dbo.EventosDominio (TipoAgregado, AgregadoId) INCLUDE (TipoEvento);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_EventosDominio_TraceId' AND object_id = OBJECT_ID(N'dbo.EventosDominio'))
    CREATE NONCLUSTERED INDEX IX_EventosDominio_TraceId ON dbo.EventosDominio (TraceId) WHERE TraceId IS NOT NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_EventosDominio_Pendientes' AND object_id = OBJECT_ID(N'dbo.EventosDominio'))
    CREATE NONCLUSTERED INDEX IX_EventosDominio_Pendientes ON dbo.EventosDominio (FechaOcurrencia) WHERE FechaProcesado IS NULL;
GO

/* -------------------------------------------------------------------------------------
   12. Datos de catálogo (forman parte de la estructura, no del seed de prueba)
   ------------------------------------------------------------------------------------- */
MERGE dbo.PosicionesJuego AS destino
USING (VALUES
    (CAST(1 AS TINYINT), N'Portero',       N'POR', CAST(1 AS TINYINT)),
    (CAST(2 AS TINYINT), N'Defensa',       N'DEF', CAST(2 AS TINYINT)),
    (CAST(3 AS TINYINT), N'Mediocampista', N'MED', CAST(3 AS TINYINT)),
    (CAST(4 AS TINYINT), N'Delantero',     N'DEL', CAST(4 AS TINYINT))
) AS origen (Id, Nombre, Abreviatura, Orden)
ON destino.Id = origen.Id
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, Nombre, Abreviatura, Orden) VALUES (origen.Id, origen.Nombre, origen.Abreviatura, origen.Orden);
GO

/* -------------------------------------------------------------------------------------
   13. Documentación de tablas (visible en SSMS → Propiedades → Propiedades extendidas)
   ------------------------------------------------------------------------------------- */
DECLARE @descripciones TABLE (Tabla SYSNAME, Descripcion NVARCHAR(200));
INSERT @descripciones VALUES
    (N'PosicionesJuego',       N'Catálogo de posiciones de juego.'),
    (N'Torneos',               N'Torneos o ediciones del Mundialito.'),
    (N'Equipos',               N'Equipos de la empresa que pueden participar en torneos.'),
    (N'TorneoEquipos',         N'Inscripción de equipos en cada torneo.'),
    (N'Jugadores',             N'Integrantes de cada equipo.'),
    (N'Partidos',              N'Calendario de partidos de un torneo y su resultado.'),
    (N'Goles',                 N'Goles anotados por partido; base de la tabla de goleadores.'),
    (N'RegistrosIdempotencia', N'Respuestas guardadas por Idempotency-Key para POST idempotentes.'),
    (N'EventosDominio',        N'Eventos de dominio registrados con su TraceId y CorrelationId.');

DECLARE @tabla SYSNAME, @descripcion NVARCHAR(200);
DECLARE cur CURSOR LOCAL FAST_FORWARD FOR SELECT Tabla, Descripcion FROM @descripciones;
OPEN cur;
FETCH NEXT FROM cur INTO @tabla, @descripcion;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.extended_properties
                   WHERE major_id = OBJECT_ID(N'dbo.' + @tabla) AND minor_id = 0 AND name = N'MS_Description')
        EXEC sp_addextendedproperty N'MS_Description', @descripcion, N'SCHEMA', N'dbo', N'TABLE', @tabla;
    FETCH NEXT FROM cur INTO @tabla, @descripcion;
END
CLOSE cur;
DEALLOCATE cur;
GO

/* -------------------------------------------------------------------------------------
   14. Verificación
   ------------------------------------------------------------------------------------- */
SELECT  t.name                                   AS Tabla,
        COUNT(DISTINCT c.column_id)              AS Columnas,
        (SELECT COUNT(*) FROM sys.foreign_keys f WHERE f.parent_object_id = t.object_id) AS LlavesForaneas,
        (SELECT COUNT(*) FROM sys.indexes i WHERE i.object_id = t.object_id AND i.index_id > 0) AS Indices
FROM    sys.tables t
JOIN    sys.columns c ON c.object_id = t.object_id
WHERE   t.name IN (N'PosicionesJuego', N'Torneos', N'Equipos', N'TorneoEquipos', N'Jugadores', N'Partidos',
                   N'Goles', N'RegistrosIdempotencia', N'EventosDominio')
GROUP BY t.name, t.object_id
ORDER BY t.name;
GO
