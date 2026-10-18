using System.Reflection;

namespace MundialitoCorporativo.Infrastructure;

/// <summary>
/// Punto de referencia al ensamblado de la capa Infrastructure (registro de implementaciones y pruebas de arquitectura).
/// </summary>
public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
