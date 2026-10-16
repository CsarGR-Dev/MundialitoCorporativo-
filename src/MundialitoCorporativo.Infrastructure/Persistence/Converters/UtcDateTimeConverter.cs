using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MundialitoCorporativo.Infrastructure.Persistence.Converters;

/// <summary>
/// SQL Server no guarda el tipo de fecha (DateTimeKind). Todas las fechas se almacenan en UTC:
/// una fecha local se convierte, una sin zona se asume UTC, y al leer se marcan como UTC.
/// </summary>
internal sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    value => ToUtc(value),
    value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
{
    internal static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}

internal sealed class NullableUtcDateTimeConverter() : ValueConverter<DateTime?, DateTime?>(
    value => value == null ? null : UtcDateTimeConverter.ToUtc(value.Value),
    value => value == null ? null : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc));
