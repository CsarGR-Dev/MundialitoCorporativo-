using System.Reflection;

namespace MundialitoCorporativo.Api;

/// <summary>
/// Punto de referencia al ensamblado de la capa Api (pruebas de arquitectura).
/// </summary>
public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
