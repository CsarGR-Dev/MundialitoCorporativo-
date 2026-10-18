using System.Collections.Concurrent;
using MundialitoCorporativo.Application.Abstractions.Idempotency;

namespace MundialitoCorporativo.UnitTests.Api;

/// <summary>
/// Almacén en memoria con la misma semántica que SqlIdempotencyStore, para probar el middleware sin base de datos.
/// </summary>
internal sealed class InMemoryIdempotencyStore : IIdempotencyStore
{
    private readonly ConcurrentDictionary<(string Key, string Path), RegistroIdempotencia> _records = new();

    public IReadOnlyCollection<RegistroIdempotencia> Records => _records.Values.ToList();

    public void Add(RegistroIdempotencia record) => _records[(record.ClaveIdempotencia, record.RutaSolicitud)] = record;

    public Task<RegistroIdempotencia?> TryBeginAsync(IdempotencyRequest request, CancellationToken cancellationToken)
    {
        var reservation = new RegistroIdempotencia
        {
            ClaveIdempotencia = request.Key,
            RutaSolicitud = request.Path,
            MetodoSolicitud = request.Method,
            HashSolicitud = request.RequestHash,
            Estado = EstadoRegistroIdempotencia.EnProceso,
            FechaCreacion = DateTime.UtcNow,
            FechaExpiracion = DateTime.UtcNow.AddHours(24)
        };

        var stored = _records.GetOrAdd((request.Key, request.Path), reservation);
        return Task.FromResult(ReferenceEquals(stored, reservation) ? null : stored);
    }

    public Task CompleteAsync(string key, string path, IdempotencyResponse response, CancellationToken cancellationToken)
    {
        _records[(key, path)] = _records[(key, path)] with
        {
            Estado = EstadoRegistroIdempotencia.Completado,
            CodigoEstadoRespuesta = response.StatusCode,
            CuerpoRespuesta = response.Body,
            EncabezadosRespuesta = response.Headers,
            FechaCompletado = DateTime.UtcNow
        };

        return Task.CompletedTask;
    }

    public Task ReleaseAsync(string key, string path, CancellationToken cancellationToken)
    {
        _records.TryRemove((key, path), out _);
        return Task.CompletedTask;
    }

    public Task<int> DeleteExpiredAsync(CancellationToken cancellationToken) => Task.FromResult(0);
}
