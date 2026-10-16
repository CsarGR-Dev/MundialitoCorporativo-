using FluentValidation;
using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Abstractions.Persistence;
using MundialitoCorporativo.Application.Abstractions.Time;
using MundialitoCorporativo.Domain.Common;
using MundialitoCorporativo.Domain.Partidos;
using MundialitoCorporativo.Domain.Torneos;

namespace MundialitoCorporativo.Application.Partidos.Commands;

/// <summary>
/// Registro del resultado de un partido y sus goleadores (requerimiento ítem 8: "Registro de resultado").
/// Emite el evento obligatorio PartidoResultRegisteredDomainEvent (ítem 7).
/// </summary>
public sealed record RegisterPartidoResultCommand(
    Guid PartidoId,
    int GolesLocal,
    int GolesVisitante,
    IReadOnlyList<DetalleGol> Goles) : ICommand;

internal sealed class RegisterPartidoResultCommandValidator : AbstractValidator<RegisterPartidoResultCommand>
{
    public RegisterPartidoResultCommandValidator()
    {
        RuleFor(command => command.GolesLocal).InclusiveBetween(0, Partido.GolesMax);
        RuleFor(command => command.GolesVisitante).InclusiveBetween(0, Partido.GolesMax);
        RuleFor(command => command.Goles).NotNull();
        RuleFor(command => command.Goles.Count)
            .Equal(command => command.GolesLocal + command.GolesVisitante)
            .When(command => command.Goles is not null)
            .OverridePropertyName("goles")
            .WithMessage("Debe registrar un goleador por cada gol del marcador.");

        RuleForEach(command => command.Goles).ChildRules(gol =>
        {
            gol.RuleFor(detalle => detalle.JugadorId).NotEmpty();
            gol.RuleFor(detalle => detalle.EquipoId).NotEmpty();
            gol.RuleFor(detalle => detalle.Minuto).InclusiveBetween(Partido.MinutoMin, Partido.MinutoMax);
        });
    }
}

internal sealed class RegisterPartidoResultCommandHandler(
    IPartidoRepository partidoRepository,
    ITorneoRepository torneoRepository,
    IJugadorRepository jugadorRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<RegisterPartidoResultCommand>
{
    public async Task<Result> Handle(RegisterPartidoResultCommand command, CancellationToken cancellationToken)
    {
        var partido = await partidoRepository.GetByIdAsync(command.PartidoId, cancellationToken);
        if (partido is null)
        {
            return PartidoErrors.NotFound(command.PartidoId);
        }

        var torneo = await torneoRepository.GetByIdAsync(partido.TorneoId, cancellationToken);
        if (torneo is null)
        {
            return TorneoErrors.NotFound(partido.TorneoId);
        }

        var scorers = await jugadorRepository.GetByIdsAsync(command.Goles.Select(gol => gol.JugadorId), cancellationToken);

        var registration = partido.RegisterResult(
            torneo,
            command.GolesLocal,
            command.GolesVisitante,
            command.Goles,
            scorers,
            dateTimeProvider.UtcNow);

        return registration.IsFailure ? registration : await unitOfWork.CommitAsync(cancellationToken);
    }
}
