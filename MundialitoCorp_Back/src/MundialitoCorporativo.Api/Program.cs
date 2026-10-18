using MundialitoCorporativo.Api;
using MundialitoCorporativo.Api.Idempotency;
using MundialitoCorporativo.Api.Observability;
using MundialitoCorporativo.Application;
using MundialitoCorporativo.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Composition root: cada capa registra sus propios servicios.
builder.Services
    .AddObservability(builder.Configuration)
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddPresentation();

var app = builder.Build();

// Base lista antes de atender peticiones: esquema de Db/ y seed si está vacía (ítem 9).
await app.Services.InitializeDatabaseAsync();

// CorrelationId, TraceId y duración de cada request (incluye las respuestas de error).
app.UseRequestObservability();

// Errores inesperados → 500 en ProblemDetails; respuestas vacías de error (404 de ruta, 405) → ProblemDetails.
app.UseExceptionHandler();
app.UseStatusCodePages();

// Documento OpenAPI en /openapi/v1.json y Scalar en /scalar para explorar y probar los endpoints.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options => options
        .WithTitle("Mundialito Corporativo API")
        .SortTagsAlphabetically());
}

// El enrutamiento va antes de la idempotencia para que el middleware sepa si el POST llega a un endpoint.
app.UseRouting();
app.UseMiddleware<IdempotencyMiddleware>();

app.MapControllers();

app.Run();

// Permite usar WebApplicationFactory<Program> en las pruebas de integración.
public partial class Program;
