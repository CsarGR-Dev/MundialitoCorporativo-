using System.Text.Json;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using MundialitoCorporativo.Application.Common.Paging;
using MundialitoCorporativo.Application.Jugadores.Queries;
using MundialitoCorporativo.Application.Torneos.Queries;

namespace MundialitoCorporativo.Api.Http;

/// <summary>
/// Ajusta cómo se leen de la query string las Queries paginadas que reciben los GET:
/// los parámetros se publican en camelCase (pageNumber, sortBy, …) y no se enlazan las propiedades calculadas
/// (AllowedSortFields, Skip) ni las que asigna la ruta (por ejemplo, el torneoId de /api/torneos/{id}/goleadores).
/// </summary>
internal sealed class PagedQueryBindingMetadataProvider : IBindingMetadataProvider
{
    private static readonly HashSet<(Type Query, string Property)> RouteAssignedProperties =
    [
        (typeof(GetTorneoEquiposQuery), nameof(GetTorneoEquiposQuery.TorneoId)),
        (typeof(GetTablaPosicionesQuery), nameof(GetTablaPosicionesQuery.TorneoId)),
        (typeof(GetGoleadoresQuery), nameof(GetGoleadoresQuery.TorneoId)),
        (typeof(GetJugadoresQuery), nameof(GetJugadoresQuery.RequireEquipo))
    ];

    public void CreateBindingMetadata(BindingMetadataProviderContext context)
    {
        if (context.Key.MetadataKind != ModelMetadataKind.Property
            || context.Key.ContainerType is not { } queryType
            || !typeof(PagedQuery).IsAssignableFrom(queryType)
            || context.Key.PropertyInfo is not { } property)
        {
            return;
        }

        if (property.SetMethod is not { IsPublic: true } || RouteAssignedProperties.Contains((queryType, property.Name)))
        {
            context.BindingMetadata.IsBindingAllowed = false;
            return;
        }

        context.BindingMetadata.BinderModelName = JsonNamingPolicy.CamelCase.ConvertName(property.Name);
    }
}
