using Microsoft.Extensions.DependencyInjection;
using MundialitoCorporativo.Application;
using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Abstractions.Persistence;
using MundialitoCorporativo.Application.Abstractions.Queries;
using MundialitoCorporativo.Application.Common.Paging;
using MundialitoCorporativo.Application.Equipos;
using MundialitoCorporativo.Application.Equipos.Commands;
using MundialitoCorporativo.Application.Equipos.Queries;
using MundialitoCorporativo.Domain.Common;
using NSubstitute;

namespace MundialitoCorporativo.UnitTests.Application;

public class DispatcherTests
{
    private readonly IEquipoRepository _equipoRepository = Substitute.For<IEquipoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IEquipoQueries _equipoQueries = Substitute.For<IEquipoQueries>();
    private readonly IDispatcher _dispatcher;

    public DispatcherTests()
    {
        var services = new ServiceCollection();
        services.AddApplication();
        services.AddSingleton(_equipoRepository);
        services.AddSingleton(_unitOfWork);
        services.AddSingleton(_equipoQueries);
        _dispatcher = services.BuildServiceProvider().CreateScope().ServiceProvider.GetRequiredService<IDispatcher>();
    }

    [Fact]
    public async Task Send_WithInvalidCommand_ShouldReturnValidationErrorWithoutCallingHandler()
    {
        var result = await _dispatcher.Send(new CreateEquipoCommand("", "T", null));

        Assert.True(result.IsFailure);
        var error = Assert.IsType<ValidationError>(result.Error);
        Assert.Contains(error.Errors, field => field.Field == "nombre");
        Assert.Contains(error.Errors, field => field.Field == "abreviatura");
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Send_WithValidCommand_ShouldExecuteHandler()
    {
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());

        var result = await _dispatcher.Send(new CreateEquipoCommand("Tecnología FC", "TEC", null));

        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task Query_WithInvalidPaging_ShouldReturnValidationError(int pageNumber, int pageSize)
    {
        var result = await _dispatcher.Query(new GetEquiposQuery { PageNumber = pageNumber, PageSize = pageSize });

        Assert.IsType<ValidationError>(result.Error);
        await _equipoQueries.DidNotReceive().GetPagedAsync(Arg.Any<GetEquiposQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Query_WithSortByOutsideWhitelist_ShouldReturnValidationError()
    {
        var result = await _dispatcher.Query(new GetEquiposQuery { SortBy = "Nombre; DROP TABLE dbo.Equipos" });

        var error = Assert.IsType<ValidationError>(result.Error);
        Assert.Contains(error.Errors, field => field.Field == "sortBy");
    }

    [Fact]
    public async Task Query_WithValidPaging_ShouldReturnPagedResponseFromReadSide()
    {
        var expected = new PagedResponse<EquipoResponse>([], 2, 5, 12);
        _equipoQueries.GetPagedAsync(Arg.Any<GetEquiposQuery>(), Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _dispatcher.Query(new GetEquiposQuery { PageNumber = 2, PageSize = 5, SortBy = "NOMBRE", SortDirection = SortDirection.Desc });

        Assert.True(result.IsSuccess);
        Assert.Same(expected, result.Value);
        Assert.Equal(3, result.Value.TotalPages);
    }
}
