using System.Reflection;

namespace MundialitoCorporativo.Domain;

/// <summary>
/// Punto de referencia al ensamblado de la capa Domain (registro de dependencias y pruebas de arquitectura).
/// </summary>
public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
