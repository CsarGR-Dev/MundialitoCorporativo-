using System.Collections.Concurrent;
using System.Reflection;
using MundialitoCorporativo.Domain.Common;

namespace MundialitoCorporativo.Application.Behaviors;

/// <summary>
/// Crea un resultado fallido del tipo que espera el pipeline (<see cref="Result"/> o <see cref="Result{TValue}"/>),
/// para que un behavior pueda cortar la ejecución sin conocer el tipo concreto de la respuesta.
/// </summary>
internal static class ResultFailure
{
    private static readonly MethodInfo GenericFailure = typeof(Result)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(method => method.Name == nameof(Result.Failure) && method.IsGenericMethodDefinition);

    private static readonly ConcurrentDictionary<Type, Func<Error, Result>> Factories = new();

    public static TResponse Create<TResponse>(Error error)
        where TResponse : Result
    {
        if (typeof(TResponse) == typeof(Result))
        {
            return (TResponse)Result.Failure(error);
        }

        var factory = Factories.GetOrAdd(typeof(TResponse), responseType =>
        {
            var failure = GenericFailure.MakeGenericMethod(responseType.GetGenericArguments()[0]);
            return value => (Result)failure.Invoke(null, [value])!;
        });

        return (TResponse)factory(error);
    }
}
