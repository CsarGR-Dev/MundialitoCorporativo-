namespace MundialitoCorporativo.Domain.Common;

/// <summary>
/// Genera Guid ordenados según el criterio de comparación de SQL Server (los últimos bytes son los más
/// significativos). Evita la fragmentación del índice agrupado sin depender de NEWSEQUENTIALID().
/// </summary>
public static class SequentialGuid
{
    private static long _counter = DateTime.UtcNow.Ticks;

    public static Guid NewGuid()
    {
        var guidBytes = Guid.NewGuid().ToByteArray();
        var counterBytes = BitConverter.GetBytes(Interlocked.Increment(ref _counter));

        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(counterBytes);
        }

        guidBytes[08] = counterBytes[1];
        guidBytes[09] = counterBytes[0];
        guidBytes[10] = counterBytes[7];
        guidBytes[11] = counterBytes[6];
        guidBytes[12] = counterBytes[5];
        guidBytes[13] = counterBytes[4];
        guidBytes[14] = counterBytes[3];
        guidBytes[15] = counterBytes[2];

        return new Guid(guidBytes);
    }
}
