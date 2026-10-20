using System.Collections.Concurrent;
using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using MundialitoCorporativo.Application;
using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Abstractions.Persistence;
using MundialitoCorporativo.Application.Abstractions.Queries;
using MundialitoCorporativo.Application.Common.Observability;
using MundialitoCorporativo.Application.Common.Paging;
using MundialitoCorporativo.Application.Equipos;
using MundialitoCorporativo.Application.Equipos.Commands;
using MundialitoCorporativo.Application.Equipos.Queries;
using MundialitoCorporativo.Domain.Common;
using NSubstitute;

namespace MundialitoCorporativo.UnitTests.Application;

/// <summary>
/// Pipeline de MediatR: validación antes del handler, span por request y log con nombre, resultado y duración.
/// </summary>
public class MediatorPipelineTests
{
    private readonly IEquipoRepository _equipoRepository = Substitute.For<IEquipoRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IEquipoQueries _equipoQueries = Substitute.For<IEquipoQueries>();
    private readonly ISender _sender;
    private readonly FakeLogCollector _logs;

    public MediatorPipelineTests()
    {
        var services = new ServiceCollection();
        services.AddFakeLogging();
        services.AddApplication();
        services.AddSingleton(_equipoRepository);
        services.AddSingleton(_unitOfWork);
        services.AddSingleton(_equipoQueries);
        var provider = services.BuildServiceProvider();
        _sender = provider.CreateScope().ServiceProvider.GetRequiredService<ISender>();
        _logs = provider.GetFakeLogCollector();
    }

    [Fact]
    public async Task Send_WithInvalidCommand_ShouldReturnValidationErrorWithoutCallingHandler()
    {
        var result = await _sender.Send(new CreateEquipoCommand("", "T", null));

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

        var result = await _sender.Send(new CreateEquipoCommand("Tecnología FC", "TEC", null));

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
        var result = await _sender.Send(new GetEquiposQuery { PageNumber = pageNumber, PageSize = pageSize });

        Assert.IsType<ValidationError>(result.Error);
        await _equipoQueries.DidNotReceive().GetPagedAsync(Arg.Any<GetEquiposQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Query_WithSortByOutsideWhitelist_ShouldReturnValidationError()
    {
        var result = await _sender.Send(new GetEquiposQuery { SortBy = "Nombre; DROP TABLE dbo.Equipos" });

        var error = Assert.IsType<ValidationError>(result.Error);
        Assert.Contains(error.Errors, field => field.Field == "sortBy");
    }

    [Fact]
    public async Task Query_WithValidPaging_ShouldReturnPagedResponseFromReadSide()
    {
        var expected = new PagedResponse<EquipoResponse>([], 2, 5, 12);
        _equipoQueries.GetPagedAsync(Arg.Any<GetEquiposQuery>(), Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _sender.Send(new GetEquiposQuery { PageNumber = 2, PageSize = 5, SortBy = "NOMBRE", SortDirection = SortDirection.Desc });

        Assert.True(result.IsSuccess);
        Assert.Same(expected, result.Value);
        Assert.Equal(3, result.Value.TotalPages);
    }

    [Fact]
    public async Task Send_WithValidCommand_ShouldLogInformationWithNameAndDurationOnly()
    {
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());

        await _sender.Send(new CreateEquipoCommand("Tecnología FC", "TEC", null));

        var record = Assert.Single(_logs.GetSnapshot(), log => log.Category!.EndsWith(".ObservabilityBehavior", StringComparison.Ordinal));
        Assert.Equal(LogLevel.Information, record.Level);
        Assert.Equal(nameof(CreateEquipoCommand), record.GetStructuredStateValue("RequestName"));
        Assert.NotNull(record.GetStructuredStateValue("ElapsedMs"));
        Assert.DoesNotContain("Tecnología FC", record.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Send_WhenResultFails_ShouldLogWarningWithErrorCode()
    {
        await _sender.Send(new CreateEquipoCommand("", "T", null));

        var record = Assert.Single(_logs.GetSnapshot(), log => log.Category!.EndsWith(".ObservabilityBehavior", StringComparison.Ordinal));
        Assert.Equal(LogLevel.Warning, record.Level);
        Assert.Equal(ValidationError.DefaultCode, record.GetStructuredStateValue("ErrorCode"));
        Assert.Equal(nameof(ErrorType.Validation), record.GetStructuredStateValue("ErrorType"));
    }

    [Fact]
    public async Task SendAndQuery_ShouldOpenOneSpanPerRequestInTheCurrentTrace()
    {
        var stopped = new ConcurrentBag<Activity>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == ApplicationDiagnostics.ActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = stopped.Add
        };
        ActivitySource.AddActivityListener(listener);
        _equipoQueries.GetPagedAsync(Arg.Any<GetEquiposQuery>(), Arg.Any<CancellationToken>()).Returns(new PagedResponse<EquipoResponse>([], 1, 10, 0));

        using var request = new Activity("GET /api/equipos").SetIdFormat(ActivityIdFormat.W3C).Start();
        await _sender.Send(new GetEquiposQuery());
        await _sender.Send(new CreateEquipoCommand("", "T", null));

        var spans = stopped.Where(span => span.TraceId == request.TraceId).ToList();
        var query = Assert.Single(spans, span => span.DisplayName == $"Query {nameof(GetEquiposQuery)}");
        var command = Assert.Single(spans, span => span.DisplayName == $"Command {nameof(CreateEquipoCommand)}");
        Assert.Equal(request.SpanId, query.ParentSpanId);
        Assert.Null(query.GetTagItem("mundialito.error.code"));
        Assert.Equal(ValidationError.DefaultCode, command.GetTagItem("mundialito.error.code"));
    }
}
