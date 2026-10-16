namespace MundialitoCorporativo.Domain.Torneos;

/// <summary>
/// Estado del torneo (columna Torneos.Estado). Solo avanza: Planificado → EnCurso → Finalizado.
/// </summary>
public enum EstadoTorneo
{
    Planificado = 1,
    EnCurso = 2,
    Finalizado = 3
}
