/* =====================================================================================
   Script      : 002_VIEW_TablaPosicionesGoleadores.sql
   Proyecto    : Mundialito de Fútbol Corporativo
   Motor       : SQL Server 2022 (compatible 2019+)
   Fecha       : 2026-10-16
   Autor       : César González
   Descripción : Vistas de consulta para revisar desde SSMS la tabla de posiciones y la
                 tabla de goleadores de cada torneo.

   Requiere    : 001_DDL_CrearBaseDatos.sql

   Notas:
   - Las vistas son para revisión en SSMS. La API calcula los mismos datos con
     Dapper, aplicando filtros, ordenamiento y paginación en la propia consulta.
   - Reglas: victoria 3 puntos, empate 1, derrota 0.
     Orden: Puntos, Diferencia de gol, Goles a favor (desempate final por nombre).
   - Solo cuentan partidos con Estado = 'Jugado' y no eliminados.
   - Incluye a todos los equipos inscritos en el torneo, aunque no hayan jugado.
   - Script re-ejecutable (CREATE OR ALTER).

   Uso:
       SELECT * FROM dbo.vw_TablaPosiciones WHERE TorneoId = @TorneoId ORDER BY Posicion;
       SELECT * FROM dbo.vw_Goleadores      WHERE TorneoId = @TorneoId ORDER BY Posicion;
   ===================================================================================== */

SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

USE Mundialito;
GO

/* -------------------------------------------------------------------------------------
   1. Tabla de posiciones por torneo
   ------------------------------------------------------------------------------------- */
CREATE OR ALTER VIEW dbo.vw_TablaPosiciones
AS
-- Cada partido jugado se convierte en dos filas: una por equipo, con sus goles a favor y en contra.
-- Los goles se convierten a INT: son TINYINT y la diferencia de gol puede ser negativa.
WITH Resultados AS
(
    SELECT  p.TorneoId,
            p.EquipoLocalId                 AS EquipoId,
            CAST(p.GolesLocal AS INT)       AS GolesFavor,
            CAST(p.GolesVisitante AS INT)   AS GolesContra
    FROM    dbo.Partidos p
    WHERE   p.Estado = 'Jugado' AND p.Eliminado = 0

    UNION ALL

    SELECT  p.TorneoId,
            p.EquipoVisitanteId,
            CAST(p.GolesVisitante AS INT),
            CAST(p.GolesLocal AS INT)
    FROM    dbo.Partidos p
    WHERE   p.Estado = 'Jugado' AND p.Eliminado = 0
),
Acumulado AS
(
    SELECT  te.TorneoId,
            te.EquipoId,
            COUNT(r.EquipoId)                                                               AS PartidosJugados,
            ISNULL(SUM(CASE WHEN r.GolesFavor >  r.GolesContra THEN 1 ELSE 0 END), 0)       AS Ganados,
            ISNULL(SUM(CASE WHEN r.GolesFavor =  r.GolesContra THEN 1 ELSE 0 END), 0)       AS Empatados,
            ISNULL(SUM(CASE WHEN r.GolesFavor <  r.GolesContra THEN 1 ELSE 0 END), 0)       AS Perdidos,
            ISNULL(SUM(r.GolesFavor), 0)                                                    AS GolesFavor,
            ISNULL(SUM(r.GolesContra), 0)                                                   AS GolesContra
    FROM    dbo.TorneoEquipos te
    LEFT JOIN Resultados r ON r.TorneoId = te.TorneoId AND r.EquipoId = te.EquipoId
    GROUP BY te.TorneoId, te.EquipoId
)
SELECT  ROW_NUMBER() OVER (
            PARTITION BY a.TorneoId
            ORDER BY (a.Ganados * 3 + a.Empatados) DESC,
                     (a.GolesFavor - a.GolesContra) DESC,
                     a.GolesFavor DESC,
                     e.Nombre ASC)                  AS Posicion,
        a.TorneoId,
        t.Nombre                                    AS Torneo,
        a.EquipoId,
        e.Nombre                                    AS Equipo,
        e.Abreviatura,
        a.PartidosJugados                           AS PJ,
        a.Ganados                                   AS G,
        a.Empatados                                 AS E,
        a.Perdidos                                  AS P,
        a.GolesFavor                                AS GF,
        a.GolesContra                               AS GC,
        a.GolesFavor - a.GolesContra                AS DG,
        a.Ganados * 3 + a.Empatados                 AS Pts
FROM    Acumulado a
JOIN    dbo.Torneos t ON t.Id = a.TorneoId AND t.Eliminado = 0
JOIN    dbo.Equipos e ON e.Id = a.EquipoId AND e.Eliminado = 0;
GO

/* -------------------------------------------------------------------------------------
   2. Goleadores por torneo
   ------------------------------------------------------------------------------------- */
CREATE OR ALTER VIEW dbo.vw_Goleadores
AS
WITH GolesPorJugador AS
(
    SELECT  p.TorneoId,
            g.JugadorId,
            g.EquipoId,
            COUNT(*)                    AS Goles,
            COUNT(DISTINCT g.PartidoId) AS PartidosConGol
    FROM    dbo.Goles g
    JOIN    dbo.Partidos p ON p.Id = g.PartidoId AND p.Estado = 'Jugado' AND p.Eliminado = 0
    GROUP BY p.TorneoId, g.JugadorId, g.EquipoId
)
SELECT  RANK() OVER (PARTITION BY gj.TorneoId ORDER BY gj.Goles DESC)  AS Posicion,
        gj.TorneoId,
        t.Nombre                                    AS Torneo,
        gj.JugadorId,
        j.Nombres + N' ' + j.Apellidos              AS Jugador,
        j.NumeroCamiseta,
        po.Nombre                                   AS PosicionJuego,
        gj.EquipoId,
        e.Nombre                                    AS Equipo,
        gj.Goles,
        gj.PartidosConGol
FROM    GolesPorJugador gj
JOIN    dbo.Torneos t     ON t.Id  = gj.TorneoId
JOIN    dbo.Jugadores j   ON j.Id  = gj.JugadorId
JOIN    dbo.PosicionesJuego po ON po.Id = j.PosicionJuegoId
JOIN    dbo.Equipos e     ON e.Id  = gj.EquipoId;
GO
