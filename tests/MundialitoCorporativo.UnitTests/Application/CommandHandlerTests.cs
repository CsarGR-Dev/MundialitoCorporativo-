using MundialitoCorporativo.Application.Abstractions.Persistence;
using MundialitoCorporativo.Application.Equipos.Commands;
using MundialitoCorporativo.Application.Partidos.Commands;
using MundialitoCorporativo.Domain.Common;
using MundialitoCorporativo.Domain.Equipos;
using MundialitoCorporativo.Domain.Partidos;
using MundialitoCorporativo.UnitTests.Domain;
using MundialitoCorporativo.UnitTests.Infrastructure;
using NSubstitute;

namespace MundialitoCorporativo.UnitTests.Application;

/// <summary>
/// Reglas de los Command handlers que requieren consultar la base: se prueban con dobles de los repositorios.
/// Todo Command confirma con el Unit of Work solo cuando la operación es válida.
/// </summary>
public class CommandHandlerTests
{
    private readonly IEquipoRepository _equipoRepository = Substitute.For<IEquipoRepository>();
    private readonly ITorneoRepository _torneoRepository = Substitute.For<ITorneoRepository>();
    private readonly IPartidoRepository _partidoRepository = Substitute.For<IPartidoRepository>();
    private readonly IJugadorRepository _jugadorRepository = Substitute.For<IJugadorRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly FixedDateTimeProvider _clock = new(TestData.UtcNow);

    public CommandHandlerTests()
    {
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
    }

    [Fact]
    public async Task CreateEquipo_WithDuplicateNombre_ShouldReturnConflictWithoutCommit()
    {
        _equipoRepository.ExistsByNombreAsync("Tecnología FC", null, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new CreateEquipoCommandHandler(_equipoRepository, _unitOfWork);

        var result = await handler.Handle(new CreateEquipoCommand("Tecnología FC", "TEC", null), CancellationToken.None);

        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal("Equipo.DuplicateNombre", result.ErrorCode);
        _equipoRepository.DidNotReceive().Add(Arg.Any<Equipo>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateEquipo_WithValidData_ShouldAddAndCommitOnce()
    {
        var handler = new CreateEquipoCommandHandler(_equipoRepository, _unitOfWork);

        var result = await handler.Handle(new CreateEquipoCommand("Tecnología FC", "TEC", null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _equipoRepository.Received(1).Add(Arg.Is<Equipo>(equipo => equipo.Id == result.Value));
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateEquipo_WhenCommitFails_ShouldReturnCommitError()
    {
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Failure(Error.Conflict("Conflicto", "Database.ConstraintViolation")));
        var handler = new CreateEquipoCommandHandler(_equipoRepository, _unitOfWork);

        var result = await handler.Handle(new CreateEquipoCommand("Tecnología FC", "TEC", null), CancellationToken.None);

        Assert.Equal("Database.ConstraintViolation", result.ErrorCode);
    }

    [Fact]
    public async Task DeleteEquipo_ThatNeverExisted_ShouldReturnNotFound()
    {
        var handler = new DeleteEquipoCommandHandler(_equipoRepository, _partidoRepository, _unitOfWork, _clock);

        var result = await handler.Handle(new DeleteEquipoCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task DeleteEquipo_AlreadyDeleted_ShouldSucceedWithoutCommit()
    {
        var equipo = TestData.NewEquipo("Tecnología FC", "TEC");
        equipo.Delete(TestData.UtcNow);
        _equipoRepository.GetByIdIncludingDeletedAsync(equipo.Id, Arg.Any<CancellationToken>()).Returns(equipo);
        var handler = new DeleteEquipoCommandHandler(_equipoRepository, _partidoRepository, _unitOfWork, _clock);

        var result = await handler.Handle(new DeleteEquipoCommand(equipo.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteEquipo_WithPartidos_ShouldReturnConflict()
    {
        var equipo = TestData.NewEquipo("Tecnología FC", "TEC");
        _equipoRepository.GetByIdIncludingDeletedAsync(equipo.Id, Arg.Any<CancellationToken>()).Returns(equipo);
        _partidoRepository.ExistsForEquipoAsync(equipo.Id, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new DeleteEquipoCommandHandler(_equipoRepository, _partidoRepository, _unitOfWork, _clock);

        var result = await handler.Handle(new DeleteEquipoCommand(equipo.Id), CancellationToken.None);

        Assert.Equal(EquipoErrors.HasPartidos, result.Error);
        Assert.False(equipo.Eliminado);
    }

    [Fact]
    public async Task SchedulePartido_WhenEquipoAlreadyPlaysAtThatTime_ShouldReturnConflict()
    {
        var torneo = TestData.NewTorneo();
        var local = Guid.NewGuid();
        var visitante = Guid.NewGuid();
        torneo.RegisterEquipo(local, TestData.UtcNow);
        torneo.RegisterEquipo(visitante, TestData.UtcNow);
        _torneoRepository.GetByIdAsync(torneo.Id, Arg.Any<CancellationToken>()).Returns(torneo);
        _partidoRepository.ExistsAtSameTimeAsync(local, visitante, Arg.Any<DateTime>(), null, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new SchedulePartidoCommandHandler(_torneoRepository, _partidoRepository, _unitOfWork);

        var result = await handler.Handle(
            new SchedulePartidoCommand(torneo.Id, local, visitante, new DateTime(2026, 10, 20, 15, 0, 0, DateTimeKind.Utc), null),
            CancellationToken.None);

        Assert.Equal(PartidoErrors.EquipoAlreadyHasPartidoAtTime, result.Error);
        _partidoRepository.DidNotReceive().Add(Arg.Any<Partido>());
    }

    [Fact]
    public async Task RegisterPartidoResult_ForUnknownPartido_ShouldReturnNotFound()
    {
        var handler = new RegisterPartidoResultCommandHandler(_partidoRepository, _torneoRepository, _jugadorRepository, _unitOfWork, _clock);

        var result = await handler.Handle(new RegisterPartidoResultCommand(Guid.NewGuid(), 0, 0, []), CancellationToken.None);

        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }
}
