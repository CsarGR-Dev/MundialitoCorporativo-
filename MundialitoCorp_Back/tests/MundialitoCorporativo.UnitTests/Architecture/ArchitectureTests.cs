using System.Reflection;
using NetArchTest.Rules;

namespace MundialitoCorporativo.UnitTests.Architecture;

/// <summary>
/// Verifica las reglas de Clean Architecture del requerimiento (ítem 2):
/// Domain no depende de ninguna otra capa, Application depende solo de Domain,
/// Infrastructure implementa contratos y Api solo orquesta.
/// </summary>
public class ArchitectureTests
{
    private const string DomainNamespace = "MundialitoCorporativo.Domain";
    private const string ApplicationNamespace = "MundialitoCorporativo.Application";
    private const string InfrastructureNamespace = "MundialitoCorporativo.Infrastructure";
    private const string ApiNamespace = "MundialitoCorporativo.Api";

    private static readonly Assembly DomainAssembly = MundialitoCorporativo.Domain.AssemblyReference.Assembly;
    private static readonly Assembly ApplicationAssembly = MundialitoCorporativo.Application.AssemblyReference.Assembly;
    private static readonly Assembly InfrastructureAssembly = MundialitoCorporativo.Infrastructure.AssemblyReference.Assembly;
    private static readonly Assembly ApiAssembly = MundialitoCorporativo.Api.AssemblyReference.Assembly;

    [Theory]
    [InlineData(ApplicationNamespace)]
    [InlineData(InfrastructureNamespace)]
    [InlineData(ApiNamespace)]
    public void Domain_ShouldNotDependOnAnyOtherLayer(string layer)
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn(layer)
            .GetResult();

        AssertSuccessful(result);
    }

    [Fact]
    public void Domain_ShouldOnlyReferenceBaseClassLibrary()
    {
        var externalReferences = DomainAssembly.GetReferencedAssemblies()
            .Select(reference => reference.Name!)
            .Where(name => !name.StartsWith("System", StringComparison.Ordinal)
                        && name is not "netstandard" and not "mscorlib")
            .ToList();

        Assert.Empty(externalReferences);
    }

    [Theory]
    [InlineData(InfrastructureNamespace)]
    [InlineData(ApiNamespace)]
    public void Application_ShouldDependOnlyOnDomain(string layer)
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn(layer)
            .GetResult();

        AssertSuccessful(result);
    }

    [Fact]
    public void Application_ShouldOnlyReferenceDomainAssembly()
    {
        var layerReferences = ApplicationAssembly.GetReferencedAssemblies()
            .Select(reference => reference.Name!)
            .Where(name => name.StartsWith("MundialitoCorporativo", StringComparison.Ordinal))
            .ToList();

        Assert.All(layerReferences, name => Assert.Equal(DomainNamespace, name));
    }

    [Theory]
    [InlineData("Microsoft.EntityFrameworkCore")]
    [InlineData("Dapper")]
    [InlineData("Microsoft.Data.SqlClient")]
    public void Application_ShouldNotDependOnPersistenceTechnologies(string technology)
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOn(technology)
            .GetResult();

        AssertSuccessful(result);
    }

    [Fact]
    public void Infrastructure_ShouldNotDependOnApi()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApiNamespace)
            .GetResult();

        AssertSuccessful(result);
    }

    [Theory]
    [InlineData(InfrastructureNamespace)]
    [InlineData("Microsoft.EntityFrameworkCore")]
    [InlineData("Dapper")]
    public void Controllers_ShouldOnlyOrchestrateThroughApplication(string dependency)
    {
        var result = Types.InAssembly(ApiAssembly)
            .That()
            .ResideInNamespace($"{ApiNamespace}.Controllers")
            .ShouldNot()
            .HaveDependencyOn(dependency)
            .GetResult();

        AssertSuccessful(result);
    }

    private static void AssertSuccessful(TestResult result)
    {
        var failingTypes = result.FailingTypeNames ?? [];

        Assert.True(result.IsSuccessful, $"Tipos que violan la regla: {string.Join(", ", failingTypes)}");
    }
}
