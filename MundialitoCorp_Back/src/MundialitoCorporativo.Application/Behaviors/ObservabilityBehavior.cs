using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Common.Observability;
using MundialitoCorporativo.Domain.Common;

namespace MundialitoCorporativo.Application.Behaviors;

/// <summary>
/// Pipeline de MediatR (el más externo): abre un span por Command o Query en la traza del request y registra
/// su resultado y duración. Solo se registran el nombre, el resultado y la duración; nunca el contenido.
/// </summary>
internal sealed class ObservabilityBehavior<TRequest, TResponse>(ILogger<ObservabilityBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    private const string CommandKind = "Command";
    private const string QueryKind = "Query";

    private static readonly string RequestKind = typeof(TRequest).GetInterfaces()
        .Any(contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IQuery<>))
        ? QueryKind
        : CommandKind;

    private static readonly string RequestName = typeof(TRequest).Name;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        using var activity = ApplicationDiagnostics.ActivitySource.StartActivity($"{RequestKind} {RequestName}");
        activity?.SetTag("mundialito.request.kind", RequestKind);
        activity?.SetTag("mundialito.request.name", RequestName);

        var startedAt = Stopwatch.GetTimestamp();
        TResponse result;
        try
        {
            result = await next();
        }
        catch (Exception exception)
        {
            activity?.SetStatus(ActivityStatusCode.Error, exception.GetType().Name);
            activity?.AddException(exception);
            throw;
        }

        var elapsedMs = Math.Round(Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds, 1);

        if (result.IsFailure)
        {
            activity?.SetTag("mundialito.error.type", result.Error.Type.ToString());
            activity?.SetTag("mundialito.error.code", result.ErrorCode);
            RequestLog.Failed(logger, RequestKind, RequestName, result.Error.Type, result.ErrorCode, elapsedMs);
        }
        else if (RequestKind == CommandKind)
        {
            RequestLog.CommandSucceeded(logger, RequestName, elapsedMs);
        }
        else
        {
            RequestLog.QuerySucceeded(logger, RequestName, elapsedMs);
        }

        return result;
    }
}

internal static partial class RequestLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Command {RequestName} ejecutado en {ElapsedMs} ms")]
    public static partial void CommandSucceeded(ILogger logger, string requestName, double elapsedMs);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Query {RequestName} ejecutada en {ElapsedMs} ms")]
    public static partial void QuerySucceeded(ILogger logger, string requestName, double elapsedMs);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{RequestKind} {RequestName} terminó con error {ErrorType} {ErrorCode} en {ElapsedMs} ms")]
    public static partial void Failed(ILogger logger, string requestKind, string requestName, ErrorType errorType, string? errorCode, double elapsedMs);
}
