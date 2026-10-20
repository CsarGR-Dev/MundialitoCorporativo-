using FluentValidation;
using MediatR;
using MundialitoCorporativo.Domain.Common;

namespace MundialitoCorporativo.Application.Behaviors;

/// <summary>
/// Pipeline de MediatR: ejecuta los validadores de FluentValidation del Command o Query antes del handler.
/// Si la entrada no es válida devuelve un <see cref="ValidationError"/> (400) y el handler no se ejecuta.
/// </summary>
internal sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var validatorList = validators.ToList();
        if (validatorList.Count == 0)
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(validatorList.Select(validator => validator.ValidateAsync(context, cancellationToken)));

        var errors = results
            .SelectMany(result => result.Errors)
            .Select(failure => new FieldError(ToCamelCase(failure.PropertyName), failure.ErrorMessage))
            .Distinct()
            .ToList();

        return errors.Count == 0
            ? await next()
            : ResultFailure.Create<TResponse>(new ValidationError(errors));
    }

    private static string ToCamelCase(string propertyName) =>
        string.IsNullOrEmpty(propertyName) ? propertyName : char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
}
