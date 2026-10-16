using Dapper;
using MundialitoCorporativo.Application.Abstractions.Data;
using MundialitoCorporativo.Application.Abstractions.Queries;
using MundialitoCorporativo.Application.Common.Paging;
using MundialitoCorporativo.Application.Equipos;
using MundialitoCorporativo.Application.Equipos.Queries;
using MundialitoCorporativo.Infrastructure.Queries.Sql;

namespace MundialitoCorporativo.Infrastructure.Queries;

/// <summary>
/// Lectura de equipos con Dapper (requerimiento ítem 3: Queries sin EF Core).
/// </summary>
internal sealed class EquipoQueries(IDbConnectionFactory connectionFactory) : IEquipoQueries
{
    private const string Select = """
        e.Id, e.Nombre, e.Abreviatura, e.Departamento,
        (SELECT COUNT(*) FROM dbo.Jugadores j WHERE j.EquipoId = e.Id AND j.Eliminado = 0) AS CantidadJugadores,
        TODATETIMEOFFSET(e.FechaCreacion, 0) AS FechaCreacion
        """;

    private const string From = "FROM dbo.Equipos e";

    internal static readonly SqlSort Sort = SqlSort.For(
        "e.Id",
        ("nombre", "e.Nombre"),
        ("abreviatura", "e.Abreviatura"),
        ("departamento", "e.Departamento"),
        ("fechaCreacion", "e.FechaCreacion"));

    public Task<PagedResponse<EquipoResponse>> GetPagedAsync(GetEquiposQuery query, CancellationToken cancellationToken)
    {
        var filter = new SqlFilter("e.Eliminado = 0")
            .AddContains(query.Nombre, "(e.Nombre LIKE @Nombre OR e.Abreviatura LIKE @Nombre)", "Nombre")
            .AddContains(query.Departamento, "e.Departamento LIKE @Departamento", "Departamento");

        return PagedSql.QueryAsync<EquipoResponse>(connectionFactory, query, Select, From, filter, Sort, cancellationToken);
    }

    public async Task<EquipoResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<EquipoResponse>(new CommandDefinition(
            $"SELECT {Select} {From} WHERE e.Id = @Id AND e.Eliminado = 0",
            new { Id = id },
            cancellationToken: cancellationToken));
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) =>
        PagedSql.ExistsAsync(connectionFactory, "dbo.Equipos", id, cancellationToken);
}
