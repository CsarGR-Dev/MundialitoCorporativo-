using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using MundialitoCorporativo.Application.Common.Paging;
using MundialitoCorporativo.Application.Equipos;
using MundialitoCorporativo.Application.Jugadores;
using MundialitoCorporativo.Application.Partidos;
using MundialitoCorporativo.Application.Torneos;

namespace MundialitoCorporativo.IntegrationTests.Api;

/// <summary>
/// API REST de punta a punta contra SQL Server: verbos y códigos HTTP (ítems 5 y 6), idempotencia persistida
/// en base de datos y la lógica del torneo expuesta por HTTP.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class ApiSqlServerTests(SqlServerDatabaseFixture fixture) : IDisposable
{
    private const string ReplayedHeader = "Idempotent-Replayed";

    private readonly MundialitoApiFactory? _factory = fixture.IsAvailable ? new MundialitoApiFactory(fixture.ConnectionString) : null;
    private HttpClient? _client;

    private HttpClient Client => _client ??= _factory!.CreateClient();

    public void Dispose()
    {
        _client?.Dispose();
        _factory?.Dispose();
    }

    [SkippableFact]
    public async Task PostEquipo_WithoutIdempotencyKey_ShouldReturn400()
    {
        SkipIfUnavailable();

        var response = await Client.PostAsJsonAsync("/api/equipos", NewEquipo());

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Idempotency.KeyRequired");
    }

    [SkippableFact]
    public async Task PostEquipo_RepeatedWithSameKey_ShouldReturnOriginalResponseWithoutDuplicating()
    {
        SkipIfUnavailable();
        var equipo = NewEquipo();
        var key = Guid.NewGuid().ToString();

        var first = await Client.PostWithKeyAsync("/api/equipos", equipo, key);
        var second = await Client.PostWithKeyAsync("/api/equipos", equipo, key);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.Equal(await first.Content.ReadAsStringAsync(), await second.Content.ReadAsStringAsync());
        Assert.Equal(first.Headers.Location, second.Headers.Location);
        Assert.False(first.Headers.Contains(ReplayedHeader));
        Assert.Equal("true", Assert.Single(second.Headers.GetValues(ReplayedHeader)));

        var equipos = await Client.GetFromJsonAsync<PagedResponse<EquipoResponse>>($"/api/equipos?nombre={Uri.EscapeDataString(equipo.Nombre)}");
        Assert.Equal(1, equipos!.TotalRecords);
    }

    [SkippableFact]
    public async Task PostEquipo_WithSameKeyAndDifferentBody_ShouldReturn409()
    {
        SkipIfUnavailable();
        var key = Guid.NewGuid().ToString();

        await Client.PostWithKeyAsync("/api/equipos", NewEquipo(), key);
        var response = await Client.PostWithKeyAsync("/api/equipos", NewEquipo(), key);

        await AssertProblemAsync(response, HttpStatusCode.Conflict, "Idempotency.KeyReused");
    }

    [SkippableFact]
    public async Task ConcurrentPosts_WithSameKey_ShouldCreateASingleEquipo()
    {
        SkipIfUnavailable();
        var equipo = NewEquipo();
        var key = Guid.NewGuid().ToString();

        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Client.PostWithKeyAsync("/api/equipos", equipo, key)));

        // Cada respuesta es la creación, su repetición o el aviso de que la clave sigue en proceso; nunca un duplicado.
        Assert.All(responses, response => Assert.Contains(response.StatusCode, new[] { HttpStatusCode.Created, HttpStatusCode.Conflict }));
        Assert.Contains(responses, response => response.StatusCode == HttpStatusCode.Created);
        foreach (var conflict in responses.Where(response => response.StatusCode == HttpStatusCode.Conflict))
        {
            await AssertProblemAsync(conflict, HttpStatusCode.Conflict, "Idempotency.InProgress");
        }

        var equipos = await Client.GetFromJsonAsync<PagedResponse<EquipoResponse>>($"/api/equipos?nombre={Uri.EscapeDataString(equipo.Nombre)}");
        Assert.Equal(1, equipos!.TotalRecords);
    }

    [SkippableFact]
    public async Task IdempotencyKey_ShouldSurviveApiRestart()
    {
        SkipIfUnavailable();
        var equipo = NewEquipo();
        var key = Guid.NewGuid().ToString();

        string originalBody;
        using (var firstInstance = new MundialitoApiFactory(fixture.ConnectionString))
        using (var firstClient = firstInstance.CreateClient())
        {
            var created = await firstClient.PostWithKeyAsync("/api/equipos", equipo, key);
            Assert.Equal(HttpStatusCode.Created, created.StatusCode);
            originalBody = await created.Content.ReadAsStringAsync();
        }

        var replayed = await Client.PostWithKeyAsync("/api/equipos", equipo, key);

        Assert.Equal(HttpStatusCode.Created, replayed.StatusCode);
        Assert.Equal(originalBody, await replayed.Content.ReadAsStringAsync());
        Assert.True(replayed.Headers.Contains(ReplayedHeader));
    }

    [SkippableFact]
    public async Task PostEquipo_WithDuplicateNombreAndNewKey_ShouldReturn409WithErrorCodeAndTraceId()
    {
        SkipIfUnavailable();
        var equipo = NewEquipo();
        await Client.PostWithKeyAsync("/api/equipos", equipo);

        var response = await Client.PostWithKeyAsync("/api/equipos", equipo with { Abreviatura = NewAbreviatura() });

        var problem = await AssertProblemAsync(response, HttpStatusCode.Conflict, "Equipo.DuplicateNombre");
        Assert.True(problem.TryGetProperty("traceId", out _));
        Assert.Contains(equipo.Nombre, problem.GetProperty("detail").GetString());
    }

    [SkippableFact]
    public async Task PostEquipo_WithInvalidData_ShouldReturn400WithFieldErrors()
    {
        SkipIfUnavailable();

        var response = await Client.PostWithKeyAsync("/api/equipos", new { nombre = "AB", abreviatura = "" });

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Validation.Failed");
        var errors = problem.GetProperty("errors");
        Assert.True(errors.TryGetProperty("nombre", out _));
        Assert.True(errors.TryGetProperty("abreviatura", out _));
    }

    [SkippableFact]
    public async Task PostEquipo_WithMalformedJson_ShouldReturn400()
    {
        SkipIfUnavailable();

        var response = await Client.PostRawWithKeyAsync("/api/equipos", """{"nombre": """, Guid.NewGuid().ToString());

        await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Validation.Failed");
    }

    [SkippableFact]
    public async Task GetEquipo_ThatDoesNotExist_ShouldReturn404()
    {
        SkipIfUnavailable();

        var response = await Client.GetAsync($"/api/equipos/{Guid.NewGuid()}");

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "Equipo.NotFound");
    }

    [SkippableFact]
    public async Task GetEquipos_WithInvalidPagingAndSort_ShouldReturn400()
    {
        SkipIfUnavailable();

        var response = await Client.GetAsync("/api/equipos?pageNumber=0&pageSize=500&sortBy=Nombre;DROP TABLE dbo.Equipos");

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Validation.Failed");
        var errors = problem.GetProperty("errors");
        Assert.True(errors.TryGetProperty("pageNumber", out _));
        Assert.True(errors.TryGetProperty("pageSize", out _));
        Assert.True(errors.TryGetProperty("sortBy", out _));
    }

    [SkippableFact]
    public async Task GetEquipos_ShouldReturnRequiredPagedFormat()
    {
        SkipIfUnavailable();
        await Client.PostWithKeyAsync("/api/equipos", NewEquipo());

        using var document = JsonDocument.Parse(await Client.GetStringAsync("/api/equipos?pageNumber=1&pageSize=1&sortBy=nombre&sortDirection=desc"));
        var root = document.RootElement;

        Assert.Equal(JsonValueKind.Array, root.GetProperty("data").ValueKind);
        Assert.Equal(1, root.GetProperty("data").GetArrayLength());
        Assert.Equal(1, root.GetProperty("pageNumber").GetInt32());
        Assert.Equal(1, root.GetProperty("pageSize").GetInt32());
        Assert.True(root.GetProperty("totalRecords").GetInt32() >= 1);
        Assert.Equal(root.GetProperty("totalRecords").GetInt32(), root.GetProperty("totalPages").GetInt32());
    }

    [SkippableFact]
    public async Task PutEquipo_Twice_ShouldLeaveTheSameState()
    {
        SkipIfUnavailable();
        var id = await CreateEquipoAsync();
        var replacement = NewEquipo() with { Departamento = "Operaciones" };

        var first = await Client.PutAsJsonAsync($"/api/equipos/{id}", replacement);
        var second = await Client.PutAsJsonAsync($"/api/equipos/{id}", replacement);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(await first.Content.ReadAsStringAsync(), await second.Content.ReadAsStringAsync());

        var equipo = await second.Content.ReadFromJsonAsync<EquipoResponse>();
        Assert.Equal(replacement.Nombre, equipo!.Nombre);
        Assert.Equal("Operaciones", equipo.Departamento);
    }

    [SkippableFact]
    public async Task PutEquipo_ThatDoesNotExist_ShouldReturn404()
    {
        SkipIfUnavailable();

        var response = await Client.PutAsJsonAsync($"/api/equipos/{Guid.NewGuid()}", NewEquipo());

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "Equipo.NotFound");
    }

    [SkippableFact]
    public async Task PatchEquipo_WithMergePatch_ShouldChangeOnlySentFields()
    {
        SkipIfUnavailable();
        var original = NewEquipo() with { Departamento = "Finanzas" };
        var id = await CreateEquipoAsync(original);

        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/equipos/{id}")
        {
            Content = new StringContent("""{"departamento":"Contabilidad"}""", Encoding.UTF8, "application/merge-patch+json")
        };
        var response = await Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var equipo = await response.Content.ReadFromJsonAsync<EquipoResponse>();
        Assert.Equal(original.Nombre, equipo!.Nombre);
        Assert.Equal(original.Abreviatura, equipo.Abreviatura);
        Assert.Equal("Contabilidad", equipo.Departamento);
    }

    [SkippableFact]
    public async Task DeleteEquipo_Twice_ShouldReturn204BothTimes()
    {
        SkipIfUnavailable();
        var id = await CreateEquipoAsync();

        var first = await Client.DeleteAsync($"/api/equipos/{id}");
        var second = await Client.DeleteAsync($"/api/equipos/{id}");
        var afterDelete = await Client.GetAsync($"/api/equipos/{id}");

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }

    [SkippableFact]
    public async Task DeleteEquipo_ThatNeverExisted_ShouldReturn404()
    {
        SkipIfUnavailable();

        var response = await Client.DeleteAsync($"/api/equipos/{Guid.NewGuid()}");

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "Equipo.NotFound");
    }

    [SkippableFact]
    public async Task TorneoFlow_OverHttp_ShouldRegisterResultAndCalculateTablaAndGoleadores()
    {
        SkipIfUnavailable();

        // Torneo con dos equipos inscritos y en curso.
        var torneoResponse = await Client.PostWithKeyAsync("/api/torneos", new { nombre = $"Torneo {Suffix()}", fechaInicio = "2026-10-01", fechaFin = "2026-12-31" });
        Assert.Equal(HttpStatusCode.Created, torneoResponse.StatusCode);
        var torneoId = (await torneoResponse.Content.ReadFromJsonAsync<TorneoResponse>())!.Id;

        var localId = await CreateEquipoAsync();
        var visitanteId = await CreateEquipoAsync();
        foreach (var equipoId in new[] { localId, visitanteId })
        {
            var inscripcion = await Client.PostWithKeyAsync($"/api/torneos/{torneoId}/equipos", new { equipoId });
            Assert.Equal(HttpStatusCode.Created, inscripcion.StatusCode);
        }

        var duplicateInscripcion = await Client.PostWithKeyAsync($"/api/torneos/{torneoId}/equipos", new { equipoId = localId });
        await AssertProblemAsync(duplicateInscripcion, HttpStatusCode.Conflict, "Torneo.EquipoAlreadyRegistered");

        var enCurso = await Client.PatchAsJsonAsync($"/api/torneos/{torneoId}", new { estado = "EnCurso" });
        Assert.Equal("EnCurso", (await enCurso.Content.ReadFromJsonAsync<TorneoResponse>())!.Estado);

        // Integrantes registrados por equipo.
        var goleadorLocal = await RegisterJugadorAsync(localId, "Ana", "Pérez", 9);
        var goleadorVisitante = await RegisterJugadorAsync(visitanteId, "Luis", "Gómez", 10);

        // Partido programado (la fecha con zona horaria se guarda en UTC).
        var partidoResponse = await Client.PostWithKeyAsync("/api/partidos", new
        {
            torneoId,
            equipoLocalId = localId,
            equipoVisitanteId = visitanteId,
            fechaProgramada = "2026-10-10T09:00:00-06:00",
            sede = "Cancha principal"
        });
        Assert.Equal(HttpStatusCode.Created, partidoResponse.StatusCode);
        var partido = await partidoResponse.Content.ReadFromJsonAsync<PartidoDetalleResponse>();
        Assert.Equal(new DateTimeOffset(2026, 10, 10, 15, 0, 0, TimeSpan.Zero), partido!.FechaProgramada);
        Assert.Equal("Programado", partido.Estado);

        // Resultado 2-1 con sus goleadores; repetir con la misma clave devuelve la respuesta original.
        var resultado = new
        {
            golesLocal = 2,
            golesVisitante = 1,
            goles = new[]
            {
                new { jugadorId = goleadorLocal, equipoId = localId, minuto = 12 },
                new { jugadorId = goleadorLocal, equipoId = localId, minuto = 58 },
                new { jugadorId = goleadorVisitante, equipoId = visitanteId, minuto = 77 }
            }
        };
        var resultadoKey = Guid.NewGuid().ToString();
        var registered = await Client.PostWithKeyAsync($"/api/partidos/{partido.Id}/resultado", resultado, resultadoKey);
        var replayed = await Client.PostWithKeyAsync($"/api/partidos/{partido.Id}/resultado", resultado, resultadoKey);
        var withNewKey = await Client.PostWithKeyAsync($"/api/partidos/{partido.Id}/resultado", resultado);

        Assert.Equal(HttpStatusCode.Created, registered.StatusCode);
        var jugado = await registered.Content.ReadFromJsonAsync<PartidoDetalleResponse>();
        Assert.Equal("Jugado", jugado!.Estado);
        Assert.Equal(3, jugado.Goles.Count);
        Assert.Equal(HttpStatusCode.Created, replayed.StatusCode);
        Assert.True(replayed.Headers.Contains(ReplayedHeader));
        await AssertProblemAsync(withNewKey, HttpStatusCode.Conflict, "Partido.AlreadyPlayed");

        // Tabla de posiciones y goleadores.
        var tabla = await Client.GetFromJsonAsync<PagedResponse<FilaTablaPosicionesResponse>>($"/api/torneos/{torneoId}/tabla-posiciones");
        Assert.Collection(
            tabla!.Data,
            fila => Assert.Equal((localId, 1, 3, 1, 2), (fila.EquipoId, fila.Posicion, fila.Puntos, fila.DiferenciaGol, fila.GolesFavor)),
            fila => Assert.Equal((visitanteId, 2, 0, -1, 1), (fila.EquipoId, fila.Posicion, fila.Puntos, fila.DiferenciaGol, fila.GolesFavor)));

        // El torneo lo define la ruta: un torneoId en la query string no la reemplaza.
        var tablaConOtroTorneo = await Client.GetFromJsonAsync<PagedResponse<FilaTablaPosicionesResponse>>(
            $"/api/torneos/{torneoId}/tabla-posiciones?torneoId={Guid.NewGuid()}&sortBy=puntos&sortDirection=asc");
        Assert.Equal(new[] { visitanteId, localId }, tablaConOtroTorneo!.Data.Select(fila => fila.EquipoId));

        var goleadores = await Client.GetFromJsonAsync<PagedResponse<GoleadorResponse>>($"/api/torneos/{torneoId}/goleadores");
        var lider = goleadores!.Data[0];
        Assert.Equal((goleadorLocal, 1, 2, "Ana Pérez"), (lider.JugadorId, lider.Posicion, lider.Goles, lider.Jugador));

        // Filtros del calendario por fecha, equipo y estado.
        var jugados = await Client.GetFromJsonAsync<PagedResponse<PartidoResponse>>(
            $"/api/partidos?torneoId={torneoId}&equipoId={visitanteId}&estado=Jugado&fechaDesde=2026-10-10&fechaHasta=2026-10-10");
        Assert.Equal(partido.Id, Assert.Single(jugados!.Data).Id);

        // Un equipo con partidos no puede eliminarse ni retirarse del torneo.
        await AssertProblemAsync(await Client.DeleteAsync($"/api/equipos/{localId}"), HttpStatusCode.Conflict, "Equipo.HasPartidos");
        await AssertProblemAsync(await Client.DeleteAsync($"/api/torneos/{torneoId}/equipos/{localId}"), HttpStatusCode.Conflict, "Torneo.EquipoHasPartidos");
    }

    [SkippableFact]
    public async Task GetJugadores_OfUnknownEquipo_ShouldReturn404()
    {
        SkipIfUnavailable();

        var response = await Client.GetAsync($"/api/equipos/{Guid.NewGuid()}/jugadores");

        await AssertProblemAsync(response, HttpStatusCode.NotFound, "Equipo.NotFound");
    }

    [SkippableFact]
    public async Task RegisterJugador_ShouldReturn201WithLocationOfTheJugador()
    {
        SkipIfUnavailable();
        var equipoId = await CreateEquipoAsync();

        var response = await Client.PostWithKeyAsync($"/api/equipos/{equipoId}/jugadores",
            new { posicionJuegoId = 1, nombres = "Carlos", apellidos = "Martínez", numeroCamiseta = 1 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var jugador = await response.Content.ReadFromJsonAsync<JugadorResponse>();
        Assert.Equal($"/api/jugadores/{jugador!.Id}", response.Headers.Location!.AbsolutePath);
        Assert.Equal(equipoId, jugador.EquipoId);

        var integrantes = await Client.GetFromJsonAsync<PagedResponse<JugadorResponse>>($"/api/equipos/{equipoId}/jugadores");
        Assert.Equal(jugador.Id, Assert.Single(integrantes!.Data).Id);
    }

    private void SkipIfUnavailable() => Skip.IfNot(fixture.IsAvailable, fixture.UnavailableReason);

    private async Task<Guid> CreateEquipoAsync(EquipoBody? equipo = null)
    {
        var response = await Client.PostWithKeyAsync("/api/equipos", equipo ?? NewEquipo());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<EquipoResponse>())!.Id;
    }

    private async Task<Guid> RegisterJugadorAsync(Guid equipoId, string nombres, string apellidos, int numeroCamiseta)
    {
        var response = await Client.PostWithKeyAsync($"/api/equipos/{equipoId}/jugadores",
            new { posicionJuegoId = 4, nombres, apellidos, numeroCamiseta });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JugadorResponse>())!.Id;
    }

    private static async Task<JsonElement> AssertProblemAsync(HttpResponseMessage response, HttpStatusCode expectedStatus, string expectedErrorCode)
    {
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(expectedStatus == response.StatusCode, $"Se esperaba {(int)expectedStatus} y se obtuvo {(int)response.StatusCode}: {content}");
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = JsonDocument.Parse(content).RootElement.Clone();
        Assert.Equal((int)expectedStatus, problem.GetProperty("status").GetInt32());
        Assert.Equal(expectedErrorCode, problem.GetProperty("errorCode").GetString());
        return problem;
    }

    private static EquipoBody NewEquipo() => new($"Equipo {Suffix()}", NewAbreviatura(), "Sistemas");

    private static string NewAbreviatura() => Guid.NewGuid().ToString("N")[..5].ToUpperInvariant();

    private static string Suffix() => Guid.NewGuid().ToString("N")[..8];

    private sealed record EquipoBody(string Nombre, string Abreviatura, string? Departamento);
}
