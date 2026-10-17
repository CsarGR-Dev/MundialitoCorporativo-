using MundialitoCorporativo.Api;
using MundialitoCorporativo.Api.Idempotency;
using MundialitoCorporativo.Application;
using MundialitoCorporativo.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Composition root: cada capa registra sus propios servicios.
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddPresentation();

var app = builder.Build();

// Errores inesperados → 500 en ProblemDetails; respuestas vacías de error (404 de ruta, 405) → ProblemDetails.
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// El enrutamiento va antes de la idempotencia para que el middleware sepa si el POST llega a un endpoint.
app.UseRouting();
app.UseMiddleware<IdempotencyMiddleware>();

app.MapControllers();

app.Run();

// Permite usar WebApplicationFactory<Program> en las pruebas de integración.
public partial class Program;
