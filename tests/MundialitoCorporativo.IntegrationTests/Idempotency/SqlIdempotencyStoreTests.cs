using Microsoft.Extensions.Options;
using MundialitoCorporativo.Application.Abstractions.Idempotency;
using MundialitoCorporativo.Application.Abstractions.Time;
using MundialitoCorporativo.Infrastructure.Data;
using MundialitoCorporativo.Infrastructure.Idempotency;

namespace MundialitoCorporativo.IntegrationTests.Idempotency;

/// <summary>
/// Persistencia de Idempotency-Key en dbo.RegistrosIdempotencia: reserva atómica, respuesta guardada,
/// liberación y vencimiento.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class SqlIdempotencyStoreTests(SqlServerDatabaseFixture fixture)
{
    private const string Path = "/api/equipos";
    private static readonly DateTime Now = new(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc);

    // SHA-256 en hexadecimal: 64 caracteres, igual que la columna CHAR(64).
    private static readonly string Hash1 = new('A', 64);
    private static readonly string Hash2 = new('B', 64);

    private readonly AdjustableDateTimeProvider _clock = new(Now);

    [SkippableFact]
    public async Task TryBegin_WithNewKey_ShouldReserveIt()
    {
        var store = CreateStore();
        var key = NewKey();

        var existing = await store.TryBeginAsync(Request(key), CancellationToken.None);
        var second = await store.TryBeginAsync(Request(key), CancellationToken.None);

        Assert.Null(existing);
        Assert.NotNull(second);
        Assert.Equal(EstadoRegistroIdempotencia.EnProceso, second.Estado);
        Assert.Equal(Hash1, second.HashSolicitud);
        Assert.Equal(Now.AddHours(24), second.FechaExpiracion);
    }

    [SkippableFact]
    public async Task TryBegin_Concurrently_ShouldGrantASingleReservation()
    {
        var key = NewKey();

        var results = await Task.WhenAll(Enumerable.Range(0, 20)
            .Select(_ => Task.Run(() => CreateStore().TryBeginAsync(Request(key), CancellationToken.None))));

        Assert.Single(results, result => result is null);
    }

    [SkippableFact]
    public async Task Complete_ShouldPersistTheOriginalResponse()
    {
        var store = CreateStore();
        var key = NewKey();
        await store.TryBeginAsync(Request(key), CancellationToken.None);

        await store.CompleteAsync(key, Path, new IdempotencyResponse(201, """{"id":"1","nombre":"Tecnología"}""", """{"Location":"/api/equipos/1"}"""), CancellationToken.None);
        var stored = await store.TryBeginAsync(Request(key), CancellationToken.None);

        Assert.NotNull(stored);
        Assert.Equal(EstadoRegistroIdempotencia.Completado, stored.Estado);
        Assert.Equal(201, stored.CodigoEstadoRespuesta);
        Assert.Equal("""{"id":"1","nombre":"Tecnología"}""", stored.CuerpoRespuesta);
        Assert.Equal("""{"Location":"/api/equipos/1"}""", stored.EncabezadosRespuesta);
        Assert.Equal(Now, stored.FechaCompletado);
    }

    [SkippableFact]
    public async Task Release_ShouldAllowTheKeyToBeReserved_Again()
    {
        var store = CreateStore();
        var key = NewKey();
        await store.TryBeginAsync(Request(key), CancellationToken.None);

        await store.ReleaseAsync(key, Path, CancellationToken.None);

        Assert.Null(await store.TryBeginAsync(Request(key), CancellationToken.None));
    }

    [SkippableFact]
    public async Task SameKey_OnAnotherRoute_ShouldBeIndependent()
    {
        var store = CreateStore();
        var key = NewKey();
        await store.TryBeginAsync(Request(key), CancellationToken.None);

        Assert.Null(await store.TryBeginAsync(Request(key) with { Path = "/api/torneos" }, CancellationToken.None));
    }

    [SkippableFact]
    public async Task ExpiredKey_ShouldBeReplacedByANewReservation()
    {
        var store = CreateStore();
        var key = NewKey();
        await store.TryBeginAsync(Request(key), CancellationToken.None);
        await store.CompleteAsync(key, Path, new IdempotencyResponse(201, "{}", null), CancellationToken.None);

        _clock.UtcNow = Now.AddHours(25);

        Assert.Null(await store.TryBeginAsync(Request(key) with { RequestHash = Hash2 }, CancellationToken.None));
    }

    [SkippableFact]
    public async Task AbandonedReservation_ShouldBeReplacedAfterTheTimeout()
    {
        var store = CreateStore();
        var key = NewKey();
        await store.TryBeginAsync(Request(key), CancellationToken.None);

        _clock.UtcNow = Now.AddMinutes(1);
        Assert.NotNull(await store.TryBeginAsync(Request(key), CancellationToken.None));

        _clock.UtcNow = Now.AddMinutes(3);
        Assert.Null(await store.TryBeginAsync(Request(key), CancellationToken.None));
    }

    [SkippableFact]
    public async Task DeleteExpired_ShouldRemoveOnlyExpiredKeys()
    {
        var store = CreateStore();
        var expiredKey = NewKey();
        await store.TryBeginAsync(Request(expiredKey), CancellationToken.None);

        _clock.UtcNow = Now.AddHours(23);
        var activeKey = NewKey();
        await store.TryBeginAsync(Request(activeKey), CancellationToken.None);
        await store.CompleteAsync(activeKey, Path, new IdempotencyResponse(201, "{}", null), CancellationToken.None);

        _clock.UtcNow = Now.AddHours(24);
        var deleted = await store.DeleteExpiredAsync(CancellationToken.None);

        Assert.True(deleted >= 1);
        Assert.Null(await store.TryBeginAsync(Request(expiredKey), CancellationToken.None));
        Assert.NotNull(await store.TryBeginAsync(Request(activeKey), CancellationToken.None));
    }

    private SqlIdempotencyStore CreateStore()
    {
        Skip.IfNot(fixture.IsAvailable, fixture.UnavailableReason);

        return new SqlIdempotencyStore(
            new SqlConnectionFactory(fixture.ConnectionString),
            _clock,
            Options.Create(new IdempotencyOptions()));
    }

    private static IdempotencyRequest Request(string key) => new(key, Path, "POST", Hash1);

    private static string NewKey() => Guid.NewGuid().ToString();

    private sealed class AdjustableDateTimeProvider(DateTime utcNow) : IDateTimeProvider
    {
        public DateTime UtcNow { get; set; } = utcNow;
    }
}
