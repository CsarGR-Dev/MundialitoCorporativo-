namespace MundialitoCorporativo.Infrastructure.Seed;

/// <summary>
/// Datos iniciales del requerimiento (ítem 9): 1 torneo en curso, 4 equipos, 5 jugadores por equipo
/// y 6 partidos, 3 con resultado y goleadores.
/// </summary>
/// <remarks>
/// Las fechas son relativas al día del seed: los partidos jugados quedan en el pasado y los pendientes
/// en el futuro, para que los filtros por fecha se puedan probar en cualquier momento.
/// Tabla esperada: Operaciones City 4 pts (DG +3), Tecnología FC 3 (−2), Recursos Humanos Athletic 1 (0),
/// Finanzas United 0 (−1).
/// </remarks>
internal static class SeedData
{
    public const string TorneoNombre = "Mundialito Corporativo 2026";
    public const string TorneoDescripcion = "Torneo de fútbol entre los departamentos de la empresa.";

    /// <summary>El torneo empezó hace 14 días y termina en 30.</summary>
    public const int TorneoInicioDias = -14;
    public const int TorneoFinDias = 30;

    /// <summary>Hora de los partidos en UTC (12:00 en El Salvador, UTC−6).</summary>
    public const int HoraPartidoUtc = 18;

    // Posiciones de juego del catálogo de 001_DDL_CrearBaseDatos.sql.
    private const byte Portero = 1;
    private const byte Defensa = 2;
    private const byte Mediocampista = 3;
    private const byte Delantero = 4;

    public static readonly IReadOnlyList<EquipoSeed> Equipos =
    [
        new("Tecnología FC", "TEC", "Tecnología",
        [
            new("Carlos", "Martínez", 1, Portero),
            new("Luis", "Hernández", 4, Defensa),
            new("Jorge", "Ramírez", 5, Defensa),
            new("Andrés", "Castillo", 8, Mediocampista),
            new("Diego", "Flores", 9, Delantero)
        ]),
        new("Finanzas United", "FIN", "Finanzas",
        [
            new("Mario", "López", 1, Portero),
            new("Ricardo", "Pérez", 4, Defensa),
            new("Fernando", "Gómez", 5, Defensa),
            new("Javier", "Torres", 8, Mediocampista),
            new("Daniel", "Rivera", 9, Delantero)
        ]),
        new("Operaciones City", "OPE", "Operaciones",
        [
            new("Óscar", "Méndez", 1, Portero),
            new("Roberto", "Cruz", 4, Defensa),
            new("Manuel", "Ortiz", 5, Defensa),
            new("Eduardo", "Vásquez", 8, Mediocampista),
            new("Alejandro", "Morales", 9, Delantero)
        ]),
        new("Recursos Humanos Athletic", "RRHH", "Recursos Humanos",
        [
            new("Sofía", "Aguilar", 1, Portero),
            new("Valeria", "Romero", 4, Defensa),
            new("Gabriela", "Navarro", 5, Defensa),
            new("Daniela", "Mejía", 8, Mediocampista),
            new("Paola", "Guzmán", 9, Delantero)
        ])
    ];

    private const int Tecnologia = 0;
    private const int Finanzas = 1;
    private const int Operaciones = 2;
    private const int RecursosHumanos = 3;

    /// <summary>Todos contra todos a una vuelta; los tres primeros ya se jugaron.</summary>
    public static readonly IReadOnlyList<PartidoSeed> Partidos =
    [
        new(Tecnologia, Finanzas, -9, "Cancha 1", new ResultadoSeed(2, 1,
        [
            new(Tecnologia, 9, 12),
            new(Tecnologia, 8, 55),
            new(Finanzas, 9, 70)
        ])),
        new(Operaciones, RecursosHumanos, -6, "Cancha 2", new ResultadoSeed(1, 1,
        [
            new(Operaciones, 9, 30),
            new(RecursosHumanos, 9, 81)
        ])),
        new(Tecnologia, Operaciones, -3, "Cancha 1", new ResultadoSeed(0, 3,
        [
            new(Operaciones, 9, 15),
            new(Operaciones, 9, 48),
            new(Operaciones, 8, 77)
        ])),
        new(Finanzas, RecursosHumanos, 3, "Cancha 2", null),
        new(Tecnologia, RecursosHumanos, 6, "Cancha 1", null),
        new(Finanzas, Operaciones, 9, "Cancha 2", null)
    ];
}

internal sealed record EquipoSeed(string Nombre, string Abreviatura, string Departamento, IReadOnlyList<JugadorSeed> Jugadores);

internal sealed record JugadorSeed(string Nombres, string Apellidos, int NumeroCamiseta, byte PosicionJuegoId);

/// <param name="Local">Índice del equipo local en <see cref="SeedData.Equipos"/>.</param>
/// <param name="Visitante">Índice del equipo visitante.</param>
/// <param name="Dias">Días respecto a la fecha del seed (negativo: ya se jugó).</param>
internal sealed record PartidoSeed(int Local, int Visitante, int Dias, string Sede, ResultadoSeed? Resultado);

internal sealed record ResultadoSeed(int GolesLocal, int GolesVisitante, IReadOnlyList<GolSeed> Goles);

/// <param name="Equipo">Índice del equipo que anota.</param>
/// <param name="NumeroCamiseta">Camiseta del goleador dentro de ese equipo.</param>
internal sealed record GolSeed(int Equipo, int NumeroCamiseta, int Minuto);
