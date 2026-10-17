using System.Collections.Concurrent;
using System.Diagnostics;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MundialitoCorporativo.Application.Abstractions.Messaging;
using MundialitoCorporativo.Application.Common.Observability;
using MundialitoCorporativo.Domain.Common;

namespace MundialitoCorporativo.Application.Messaging;

/// <summary>
/// Despachador CQRS propio: resuelve el handler del Command o Query desde el contenedor,
/// ejecuta antes sus validadores y devuelve un <see cref="ValidationError"/> si la entrada no es válida.
/// Cada ejecución abre un span en la traza del request y registra su resultado y duración.
/// </summary>
internal sealed partial class Dispatcher(IServiceProvider serviceProvider, ILogger<Dispatcher> logger) : IDispatcher
{
    private const string CommandKind = "Command";
    private const string QueryKind = "Query";

    private static readonly ConcurrentDictionary<Type, object> Executors = new();

    public Task<Result> Send(ICommand command, CancellationToken cancellationToken = default)
    {
        var executor = (ICommandExecutor)Executors.GetOrAdd(
            command.GetType(),
            commandType => Activator.CreateInstance(typeof(CommandExecutor<>).MakeGenericType(commandType))!);

        return ObserveAsync(CommandKind, command.GetType(), () => executor.Execute(command, serviceProvider, cancellationToken));
    }

    public Task<Result<TResponse>> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
    {
        var executor = (IRequestExecutor<TResponse>)Executors.GetOrAdd(
            command.GetType(),
            commandType => Activator.CreateInstance(typeof(CommandExecutor<,>).MakeGenericType(commandType, typeof(TResponse)))!);

        return ObserveAsync(CommandKind, command.GetType(), () => executor.Execute(command, serviceProvider, cancellationToken));
    }

    public Task<Result<TResponse>> Query<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
    {
        var executor = (IRequestExecutor<TResponse>)Executors.GetOrAdd(
            query.GetType(),
            queryType => Activator.CreateInstance(typeof(QueryExecutor<,>).MakeGenericType(queryType, typeof(TResponse)))!);

        return ObserveAsync(QueryKind, query.GetType(), () => executor.Execute(query, serviceProvider, cancellationToken));
    }

    /// <summary>
    /// Solo se registran el nombre del Command o Query, el resultado y la duración; nunca su contenido.
    /// </summary>
    private async Task<TResult> ObserveAsync<TResult>(string kind, Type requestType, Func<Task<TResult>> execute)
        where TResult : Result
    {
        var requestName = requestType.Name;
        using var activity = ApplicationDiagnostics.ActivitySource.StartActivity($"{kind} {requestName}");
        activity?.SetTag("mundialito.request.kind", kind);
        activity?.SetTag("mundialito.request.name", requestName);

        var startedAt = Stopwatch.GetTimestamp();
        TResult result;
        try
        {
            result = await execute();
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
            LogRequestFailed(logger, kind, requestName, result.Error.Type, result.ErrorCode, elapsedMs);
        }
        else if (kind == CommandKind)
        {
            LogCommandSucceeded(logger, requestName, elapsedMs);
        }
        else
        {
            LogQuerySucceeded(logger, requestName, elapsedMs);
        }

        return result;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Command {RequestName} ejecutado en {ElapsedMs} ms")]
    private static partial void LogCommandSucceeded(ILogger logger, string requestName, double elapsedMs);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Query {RequestName} ejecutada en {ElapsedMs} ms")]
    private static partial void LogQuerySucceeded(ILogger logger, string requestName, double elapsedMs);

    [LoggerMessage(Level = LogLevel.Warning, Message = "{RequestKind} {RequestName} terminó con error {ErrorType} {ErrorCode} en {ElapsedMs} ms")]
    private static partial void LogRequestFailed(ILogger logger, string requestKind, string requestName, ErrorType errorType, string? errorCode, double elapsedMs);

    private static async Task<ValidationError?> ValidateAsync<TRequest>(TRequest request, IServiceProvider provider, CancellationToken cancellationToken)
    {
        var validators = provider.GetServices<IValidator<TRequest>>().ToList();
        if (validators.Count == 0)
        {
            return null;
        }

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));

        var errors = results
            .SelectMany(result => result.Errors)
            .Select(failure => new FieldError(ToCamelCase(failure.PropertyName), failure.ErrorMessage))
            .Distinct()
            .ToList();

        return errors.Count == 0 ? null : new ValidationError(errors);
    }

    private static string ToCamelCase(string propertyName) =>
        string.IsNullOrEmpty(propertyName) ? propertyName : char.ToLowerInvariant(propertyName[0]) + propertyName[1..];

    private interface ICommandExecutor
    {
        Task<Result> Execute(ICommand command, IServiceProvider provider, CancellationToken cancellationToken);
    }

    private interface IRequestExecutor<TResponse>
    {
        Task<Result<TResponse>> Execute(object request, IServiceProvider provider, CancellationToken cancellationToken);
    }

    private sealed class CommandExecutor<TCommand> : ICommandExecutor
        where TCommand : ICommand
    {
        public async Task<Result> Execute(ICommand command, IServiceProvider provider, CancellationToken cancellationToken)
        {
            var typedCommand = (TCommand)command;

            var validationError = await ValidateAsync(typedCommand, provider, cancellationToken);
            if (validationError is not null)
            {
                return validationError;
            }

            return await provider.GetRequiredService<ICommandHandler<TCommand>>().Handle(typedCommand, cancellationToken);
        }
    }

    private sealed class CommandExecutor<TCommand, TResponse> : IRequestExecutor<TResponse>
        where TCommand : ICommand<TResponse>
    {
        public async Task<Result<TResponse>> Execute(object request, IServiceProvider provider, CancellationToken cancellationToken)
        {
            var command = (TCommand)request;

            var validationError = await ValidateAsync(command, provider, cancellationToken);
            if (validationError is not null)
            {
                return validationError;
            }

            return await provider.GetRequiredService<ICommandHandler<TCommand, TResponse>>().Handle(command, cancellationToken);
        }
    }

    private sealed class QueryExecutor<TQuery, TResponse> : IRequestExecutor<TResponse>
        where TQuery : IQuery<TResponse>
    {
        public async Task<Result<TResponse>> Execute(object request, IServiceProvider provider, CancellationToken cancellationToken)
        {
            var query = (TQuery)request;

            var validationError = await ValidateAsync(query, provider, cancellationToken);
            if (validationError is not null)
            {
                return validationError;
            }

            return await provider.GetRequiredService<IQueryHandler<TQuery, TResponse>>().Handle(query, cancellationToken);
        }
    }
}
