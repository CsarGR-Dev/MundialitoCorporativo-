using MundialitoCorporativo.Application.Abstractions.Time;

namespace MundialitoCorporativo.Infrastructure.Time;

internal sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
