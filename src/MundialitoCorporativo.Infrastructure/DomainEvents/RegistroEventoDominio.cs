namespace MundialitoCorporativo.Infrastructure.DomainEvents;

/// <summary>
/// Registro persistido de un evento de dominio (tabla EventosDominio).
/// </summary>
public sealed class RegistroEventoDominio
{
    public RegistroEventoDominio(
        Guid id,
        string tipoEvento,
        string tipoAgregado,
        Guid agregadoId,
        string datosEvento,
        DateTime fechaOcurrencia,
        string? traceId,
        string? correlationId)
    {
        Id = id;
        TipoEvento = tipoEvento;
        TipoAgregado = tipoAgregado;
        AgregadoId = agregadoId;
        DatosEvento = datosEvento;
        FechaOcurrencia = fechaOcurrencia;
        TraceId = traceId;
        CorrelationId = correlationId;
    }

    private RegistroEventoDominio()
    {
    }

    public Guid Id { get; private init; }

    public string TipoEvento { get; private init; } = string.Empty;

    public string TipoAgregado { get; private init; } = string.Empty;

    public Guid AgregadoId { get; private init; }

    public string DatosEvento { get; private init; } = string.Empty;

    public DateTime FechaOcurrencia { get; private init; }

    public string? TraceId { get; private init; }

    public string? CorrelationId { get; private init; }

    public DateTime? FechaProcesado { get; private set; }
}
