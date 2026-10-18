using MundialitoCorporativo.Infrastructure.Initialization;

namespace MundialitoCorporativo.UnitTests.Infrastructure;

/// <summary>
/// Scripts de estructura incluidos en el ensamblado, que la API ejecuta al iniciar (ítem 9).
/// </summary>
public class SqlScriptsTests
{
    [Fact]
    public void Schema_ShouldBeEmbeddedInExecutionOrder()
    {
        Assert.Equal([SqlScripts.CrearBaseDatos, SqlScripts.VistasTablaPosicionesGoleadores], SqlScripts.Schema);

        Assert.All(SqlScripts.Schema, fileName => Assert.Contains("USE Mundialito;", SqlScripts.Load(fileName), StringComparison.Ordinal));
    }

    [Fact]
    public void ForDatabase_ShouldReplaceDatabaseName()
    {
        var script = SqlScripts.ForDatabase(SqlScripts.Load(SqlScripts.CrearBaseDatos), "Mundialito_Docker");

        Assert.Contains("IF DB_ID(N'Mundialito_Docker') IS NULL", script, StringComparison.Ordinal);
        Assert.Contains("CREATE DATABASE Mundialito_Docker ", script, StringComparison.Ordinal);
        Assert.Contains("USE Mundialito_Docker;", script, StringComparison.Ordinal);
        Assert.DoesNotContain("USE Mundialito;", script, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Mundialito; DROP DATABASE master")]
    [InlineData("Mundialito]")]
    [InlineData("1Mundialito")]
    [InlineData("")]
    public void ForDatabase_WithUnsafeName_ShouldBeRejected(string databaseName)
    {
        Assert.Throws<ArgumentException>(() => SqlScripts.ForDatabase("USE Mundialito;", databaseName));
    }

    [Fact]
    public void SplitBatches_ShouldSplitOnlyOnGoLines()
    {
        const string script = "SELECT 1;\nGO\nSELECT Goles FROM dbo.Goles;\n  go  \nSELECT 3;\n";

        var batches = SqlScripts.SplitBatches(script).Select(batch => batch.Trim()).ToList();

        Assert.Equal(["SELECT 1;", "SELECT Goles FROM dbo.Goles;", "SELECT 3;"], batches);
    }
}
