import type { Metadata } from "next";
import Link from "next/link";
import { Suspense } from "react";

import { EmptyState } from "@/components/EmptyState";
import { ErrorMessage } from "@/components/ErrorMessage";
import { FilterForm } from "@/components/FilterForm";
import { Pagination } from "@/components/Pagination";
import { SortableHeader } from "@/components/SortableHeader";
import { TableSkeleton } from "@/components/TableSkeleton";
import { EQUIPOS_SORT_FIELDS, getEquipos } from "@/lib/api/equipos.api";
import { PAGE_SIZES, pageQuery, param, searchKey, type SearchParams } from "@/lib/search-params";

export const metadata: Metadata = { title: "Equipos" };

const PATH = "/equipos";

/** Equipos participantes con filtros por nombre y departamento, orden por columna y paginación (ítems 8 y 10). */
export default async function EquiposPage({ searchParams }: PageProps<"/equipos">) {
  const params = await searchParams;
  const query = pageQuery(params, EQUIPOS_SORT_FIELDS);
  const nombre = param(params, "nombre");
  const departamento = param(params, "departamento");
  // Sin await: la consulta viaja al bloque de resultados y se transmite cuando llega, sin frenar la página.
  const equipos = getEquipos({ ...query, nombre, departamento });

  return (
    <>
      <div className="page-head">
        <div>
          <p className="eyebrow">Participantes</p>
          <h1>Equipos</h1>
          <p className="page-sub">Equipos inscritos, el departamento que representan y la cantidad de integrantes.</p>
        </div>
      </div>

      <FilterForm action={PATH} limpiar={PATH}>
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
      </FilterForm>

      <Suspense key={searchKey(params)} fallback={<TableSkeleton />}>
        <ListaEquipos equipos={equipos} params={params} />
      </Suspense>
    </>
  );
}

/** Solo este bloque se vuelve a pedir al filtrar, ordenar o cambiar de página. */
async function ListaEquipos({ equipos: consulta, params }: { equipos: ReturnType<typeof getEquipos>; params: SearchParams }) {
  const equipos = await consulta;

  if (!equipos.ok) {
    return <ErrorMessage error={equipos.error} />;
  }

  if (equipos.data.data.length === 0) {
    return <EmptyState>No hay equipos para los filtros seleccionados.</EmptyState>;
  }

  return (
    <>
      <div className="table-wrap">
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
                  <div className="row-actions">
                    <Link href={`/equipos/${equipo.id}`}>Ver integrantes</Link>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <Pagination pathname={PATH} searchParams={params} page={equipos.data} />
    </>
  );
}
