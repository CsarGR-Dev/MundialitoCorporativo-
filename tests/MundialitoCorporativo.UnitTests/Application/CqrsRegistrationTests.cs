using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Common.Paging;
using MundialitoCorporativo.Application.Equipos.Queries;
using MundialitoCorporativo.Application.Jugadores.Queries;
using MundialitoCorporativo.Application.Partidos.Queries;
using MundialitoCorporativo.Application.PosicionesJuego.Queries;
using MundialitoCorporativo.Application.Torneos.Queries;
using MundialitoCorporativo.Infrastructure.Queries;

namespace MundialitoCorporativo.UnitTests.Application;

public class CqrsRegistrationTests
{
    private static readonly Type[] HandlerDefinitions = [typeof(ICommandHandler<>), typeof(ICommandHandler<,>), typeof(IQueryHandler<,>)];

    public static TheoryData<string, IReadOnlyList<string>, IReadOnlyDictionary<string, string>> SortMappings => new()
    {
        { nameof(GetEquiposQuery), GetEquiposQuery.SortFields, EquipoQueries.Sort.Columns },
        { nameof(GetJugadoresQuery), GetJugadoresQuery.SortFields, JugadorQueries.Sort.Columns },
        { nameof(GetPartidosQuery), GetPartidosQuery.SortFields, PartidoQueries.Sort.Columns },
        { nameof(GetPosicionesJuegoQuery), GetPosicionesJuegoQuery.SortFields, PosicionJuegoQueries.Sort.Columns },
        { nameof(GetTorneosQuery), GetTorneosQuery.SortFields, TorneoQueries.TorneoSort.Columns },
        { nameof(GetTorneoEquiposQuery), GetTorneoEquiposQuery.SortFields, TorneoQueries.EquiposSort.Columns },
        { nameof(GetTablaPosicionesQuery), GetTablaPosicionesQuery.SortFields, TorneoQueries.TablaSort.Columns },
        { nameof(GetGoleadoresQuery), GetGoleadoresQuery.SortFields, TorneoQueries.GoleadoresSort.Columns },
    };

    [Fact]
    public void EveryCommandAndQuery_ShouldHaveExactlyOneHandler()
    {
        var types = MundialitoCorporativo.Application.AssemblyReference.Assembly.GetTypes();

        var requests = types.Where(type => type is { IsAbstract: false, IsInterface: false }
                                           && type.GetInterfaces().Any(IsRequestInterface));

        var handledRequests = types
            .SelectMany(type => type.GetInterfaces())
            .Where(contract => contract.IsGenericType && HandlerDefinitions.Contains(contract.GetGenericTypeDefinition()))
            .Select(contract => contract.GetGenericArguments()[0])
            .ToList();

        Assert.All(requests, request => Assert.Equal(1, handledRequests.Count(handled => handled == request)));
    }

    [Theory]
    [MemberData(nameof(SortMappings))]
    public void EveryAllowedSortField_ShouldMapToASqlColumn(string queryName, IReadOnlyList<string> sortFields, IReadOnlyDictionary<string, string> columns)
    {
        Assert.All(sortFields, field => Assert.True(columns.ContainsKey(field), $"{queryName}: falta la columna para '{field}'."));
        Assert.Equal(sortFields.Count, columns.Count);
    }

    [Fact]
    public void PagedQueries_ShouldUseFirstAllowedFieldAsDefaultSort()
    {
        var query = new GetEquiposQuery();

        Assert.Equal("nombre", query.EffectiveSortBy);
        Assert.Equal(SortDirection.Asc, query.SortDirection);
        Assert.Equal(PagedQuery.DefaultPageSize, query.PageSize);
    }

    private static bool IsRequestInterface(Type contract) =>
        contract == typeof(ICommand)
        || (contract.IsGenericType && (contract.GetGenericTypeDefinition() == typeof(ICommand<>) || contract.GetGenericTypeDefinition() == typeof(IQuery<>)));
}
