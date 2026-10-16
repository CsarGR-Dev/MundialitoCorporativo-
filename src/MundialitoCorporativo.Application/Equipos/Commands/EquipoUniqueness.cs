using MundialitoCorporativo.Application.Abstractions.Persistence;
using MundialitoCorporativo.Domain.Common;
using MundialitoCorporativo.Domain.Equipos;

namespace MundialitoCorporativo.Application.Equipos.Commands;

/// <summary>
/// RN-01: nombre y abreviatura únicos entre equipos no eliminados. Compartido por crear, reemplazar y modificar.
/// </summary>
internal static class EquipoUniqueness
{
    public static async Task<Error?> CheckAsync(
        IEquipoRepository equipoRepository,
        string nombre,
        string abreviatura,
        Guid? excludeId,
        CancellationToken cancellationToken)
    {
        if (await equipoRepository.ExistsByNombreAsync(nombre, excludeId, cancellationToken))
        {
            return EquipoErrors.DuplicateNombre(nombre.Trim());
        }

        if (await equipoRepository.ExistsByAbreviaturaAsync(abreviatura, excludeId, cancellationToken))
        {
            return EquipoErrors.DuplicateAbreviatura(Equipo.NormalizeAbreviatura(abreviatura));
        }

        return null;
    }
}
