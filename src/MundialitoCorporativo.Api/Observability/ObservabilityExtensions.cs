using System.Globalization;
using MundialitoCorporativo.Application.Common.Observability;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting.Compact;

namespace MundialitoCorporativo.Api.Observability;

/// <summary>
/// Observabilidad del requerimiento ítem 7: Serilog como proveedor de ILogger, trazabilidad por request
/// y, opcionalmente, exportación de trazas con OpenTelemetry.
/// </summary>
public static class ObservabilityExtensions
{
    private const string TextTemplate =
        "[{Timestamp:HH:mm:ss.fff} {Level:u3}] {Message:lj} [TraceId={TraceId} CorrelationId={CorrelationId}]{NewLine}{Exception}";

    public static IServiceCollection AddObservability(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(ObservabilityOptions.SectionName);
        services.Configure<ObservabilityOptions>(section);
        var options = section.Get<ObservabilityOptions>() ?? new ObservabilityOptions();

        // El código de las capas usa ILogger<T>; Serilog solo se conoce aquí.
        // Niveles mínimos en la sección "Serilog" de appsettings.
        services.AddSerilog((serviceProvider, logger) =>
        {
            logger
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithProperty("Application", options.ServiceName)
                .Enrich.WithProperty("Environment", serviceProvider.GetRequiredService<IHostEnvironment>().EnvironmentName);

            if (string.Equals(options.ConsoleFormat, "Text", StringComparison.OrdinalIgnoreCase))
            {
                logger.WriteTo.Console(outputTemplate: TextTemplate, formatProvider: CultureInfo.InvariantCulture);
            }
            else
            {
                logger.WriteTo.Console(new RenderedCompactJsonFormatter());
            }

            if (!string.IsNullOrWhiteSpace(options.LogFilePath))
            {
                logger.WriteTo.File(
                    new CompactJsonFormatter(),
                    options.LogFilePath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: options.RetainedLogFiles);
            }
        });

        if (!string.IsNullOrWhiteSpace(options.OtlpEndpoint))
        {
            var otlpEndpoint = new Uri(options.OtlpEndpoint);

            services.AddOpenTelemetry()
                .ConfigureResource(resource => resource.AddService(options.ServiceName))
                .WithTracing(tracing => tracing
                    .AddAspNetCoreInstrumentation(instrumentation =>
                        instrumentation.Filter = context => !context.Request.Path.StartsWithSegments("/openapi"))
                    .AddSqlClientInstrumentation()
                    .AddSource(ApplicationDiagnostics.ActivitySourceName)
                    .AddOtlpExporter(exporter => exporter.Endpoint = otlpEndpoint));
        }

        return services;
    }

    /// <summary>
    /// Va primero en el pipeline: así el CorrelationId, el TraceId y la duración cubren también
    /// las respuestas de error que genera el manejador de excepciones.
    /// </summary>
    public static IApplicationBuilder UseRequestObservability(this IApplicationBuilder app) =>
        app.UseMiddleware<CorrelationIdMiddleware>()
            .UseMiddleware<RequestTimingMiddleware>();
}
