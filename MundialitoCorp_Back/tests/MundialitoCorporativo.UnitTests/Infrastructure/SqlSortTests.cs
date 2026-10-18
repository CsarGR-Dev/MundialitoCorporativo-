using MundialitoCorporativo.Application.Common.Paging;
using MundialitoCorporativo.Application.Equipos.Queries;
using MundialitoCorporativo.Application.Torneos.Queries;
using MundialitoCorporativo.Infrastructure.Queries;
using MundialitoCorporativo.Infrastructure.Queries.Sql;

namespace MundialitoCorporativo.UnitTests.Infrastructure;

public class SqlSortTests
{
    [Fact]
    public void ToOrderBy_ShouldTranslateFieldAndAppendTieBreaker()
    {
        var sort = SqlSort.For("e.Id", ("nombre", "e.Nombre"));

        var orderBy = sort.ToOrderBy(new GetEquiposQuery { SortBy = "NOMBRE", SortDirection = SortDirection.Desc });

        Assert.Equal("e.Nombre DESC, e.Id", orderBy);
    }

    [Fact]
    public void ToOrderBy_WhenSortColumnIsTieBreaker_ShouldNotRepeatColumn()
    {
        var orderBy = TorneoQueries.TablaSort.ToOrderBy(new GetTablaPosicionesQuery { TorneoId = Guid.NewGuid() });

        Assert.Equal("Posicion ASC", orderBy);
    }

    [Fact]
    public void ToOrderBy_WhenSortColumnIsPartOfTieBreaker_ShouldKeepOnlyRemainingColumns()
    {
        var orderBy = TorneoQueries.GoleadoresSort.ToOrderBy(new GetGoleadoresQuery { TorneoId = Guid.NewGuid(), SortBy = "jugador" });

        Assert.Equal("Apellidos ASC, JugadorId", orderBy);
    }
}
