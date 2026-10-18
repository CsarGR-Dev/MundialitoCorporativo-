import type { Metadata } from "next";
import Link from "next/link";

import { EmptyState } from "@/components/EmptyState";
import { ErrorMessage } from "@/components/ErrorMessage";
import { Pagination } from "@/components/Pagination";
import { SortableHeader } from "@/components/SortableHeader";
import { EQUIPOS_SORT_FIELDS, getEquipos } from "@/lib/api/equipos.api";
import { PAGE_SIZES, pageQuery, param } from "@/lib/search-params";

export const metadata: Metadata = { title: "Equipos" };

const PATH = "/equipos";

/** Equipos participantes con filtros por nombre y departamento, orden por columna y paginación (ítems 8 y 10). */
export default async function EquiposPage({ searchParams }: PageProps<"/equipos">) {
  const params = await searchParams;
  const query = pageQuery(params, EQUIPOS_SORT_FIELDS);
  const nombre = param(params, "nombre");
  const departamento = param(params, "departamento");
  const equipos = await getEquipos({ ...query, nombre, departamento });

  return (
    <>
      <h1>Equipos</h1>

      <form method="get" action={PATH} className="filters">
        <label>
          Nombre
          <input name="nombre" defaultValue={nombre} placeholder="Contiene…" />
        </label>
        <label>
          Departamento
          <input name="departamento" defaultValue={departamento} placeholder="Contiene…" />
        </label>
        <label>
          Por página
          <select name="pageSize" defaultValue={String(query.pageSize)}>
            {PAGE_SIZES.map((size) => (
              <option key={size} value={size}>
                {size}
              </option>
            ))}
          </select>
        </label>
        {query.sortBy && <input type="hidden" name="sortBy" value={query.sortBy} />}
        {query.sortDirection && <input type="hidden" name="sortDirection" value={query.sortDirection} />}
        <button type="submit">Filtrar</button>
        <Link href={PATH}>Limpiar</Link>
      </form>

      {!equipos.ok ? (
        <ErrorMessage error={equipos.error} />
      ) : equipos.data.data.length === 0 ? (
        <EmptyState>No hay equipos para los filtros seleccionados.</EmptyState>
      ) : (
        <>
          <table>
            <thead>
              <tr>
                <SortableHeader label="Nombre" field="nombre" pathname={PATH} searchParams={params} defaultField="nombre" />
                <SortableHeader label="Abreviatura" field="abreviatura" pathname={PATH} searchParams={params} defaultField="nombre" />
                <SortableHeader label="Departamento" field="departamento" pathname={PATH} searchParams={params} defaultField="nombre" />
                <th className="num">Integrantes</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {equipos.data.data.map((equipo) => (
                <tr key={equipo.id}>
                  <td>{equipo.nombre}</td>
                  <td>{equipo.abreviatura}</td>
                  <td>{equipo.departamento ?? "—"}</td>
                  <td className="num">{equipo.cantidadJugadores}</td>
                  <td>
                    <Link href={`/equipos/${equipo.id}`}>Ver integrantes</Link>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          <Pagination pathname={PATH} searchParams={params} page={equipos.data} />
        </>
      )}
    </>
  );
}
