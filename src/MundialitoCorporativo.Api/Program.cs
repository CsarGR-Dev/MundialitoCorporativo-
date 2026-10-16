using MundialitoCorporativo.Application;
using MundialitoCorporativo.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Composition root: cada capa registra sus propios servicios.
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.Run();

// Permite usar WebApplicationFactory<Program> en las pruebas de integración.
public partial class Program;
