using MundialitoCorporativo.Application.Abstractions.Time;

namespace MundialitoCorporativo.UnitTests.Infrastructure;

internal sealed class FixedDateTimeProvider(DateTime utcNow) : IDateTimeProvider
{
    public DateTime UtcNow { get; } = utcNow;
}
