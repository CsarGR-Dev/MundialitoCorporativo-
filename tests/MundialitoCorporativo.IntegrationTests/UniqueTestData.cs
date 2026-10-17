namespace MundialitoCorporativo.IntegrationTests;

/// <summary>
/// Valores únicos para los datos de prueba. La abreviatura de equipo es única en la base y solo admite
/// 2 a 5 caracteres; con valores aleatorios dos pruebas podían chocar y fallar de forma intermitente.
/// Un contador compartido por todo el proceso garantiza que no se repitan dentro de una ejecución
/// (cada ejecución usa su propia base temporal).
/// </summary>
internal static class UniqueTestData
{
    private static int _counter;

    /// <summary>5 letras: el prefijo indicado y 4 letras del contador (hasta 456 976 valores por ejecución).</summary>
    public static string Abreviatura(char prefijo)
    {
        var value = Interlocked.Increment(ref _counter);
        var letters = new char[5];
        letters[0] = prefijo;

        for (var index = letters.Length - 1; index > 0; index--)
        {
            letters[index] = (char)('A' + (value % 26));
            value /= 26;
        }

        return new string(letters);
    }
}
