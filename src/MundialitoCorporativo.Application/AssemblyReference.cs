using System.Reflection;

namespace MundialitoCorporativo.Application;

/// <summary>
/// Punto de referencia al ensamblado de la capa Application (registro de handlers y pruebas de arquitectura).
/// </summary>
public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
