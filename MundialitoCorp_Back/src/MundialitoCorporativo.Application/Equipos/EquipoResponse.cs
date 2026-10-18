namespace MundialitoCorporativo.Application.Equipos;

public sealed record EquipoResponse
{
    public Guid Id { get; init; }

    public string Nombre { get; init; } = string.Empty;

    public string Abreviatura { get; init; } = string.Empty;

    public string? Departamento { get; init; }

    public int CantidadJugadores { get; init; }

    public DateTimeOffset FechaCreacion { get; init; }
}
