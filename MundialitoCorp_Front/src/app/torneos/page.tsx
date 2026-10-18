import type { Metadata } from "next";
import Link from "next/link";
import { Suspense } from "react";

import { EmptyState } from "@/components/EmptyState";
import { ErrorMessage } from "@/components/ErrorMessage";
import { EstadoTorneoBadge } from "@/components/EstadoTorneoBadge";
import { FilterForm } from "@/components/FilterForm";
import { Pagination } from "@/components/Pagination";
import { SortableHeader } from "@/components/SortableHeader";
import { TableSkeleton } from "@/components/TableSkeleton";
import { TORNEOS_SORT_FIELDS, getTorneos } from "@/lib/api/torneos.api";
import { formatFecha } from "@/lib/format";
import { PAGE_SIZES, pageQuery, param, searchKey, type SearchParams } from "@/lib/search-params";
import { ESTADOS_TORNEO } from "@/lib/types/models";

export const metadata: Metadata = { title: "Torneos" };

const PATH = "/torneos";

/** Torneos con filtros por nombre y estado, orden por columna y paginación. */
export default async function TorneosPage({ searchParams }: PageProps<"/torneos">) {
  const params = await searchParams;
  const query = pageQuery(params, TORNEOS_SORT_FIELDS);
  const nombre = param(params, "nombre");
  const estado = param(params, "estado");
  // Sin await: la consulta viaja al bloque de resultados y se transmite cuando llega, sin frenar la página.
  const torneos = getTorneos({ ...query, nombre, estado });

  return (
    <>
      <div className="page-head">
        <div>
          <p className="eyebrow">Competencias</p>
          <h1>Torneos</h1>
          <p className="page-sub">Cada torneo tiene sus equipos inscritos, su calendario, su tabla de posiciones y sus goleadores.</p>
        </div>
        <div className="page-actions">
          <Link href="/torneos/nuevo" className="btn-link">
            Nuevo torneo
          </Link>
        </div>
      </div>

      <FilterForm action={PATH} limpiar={PATH}>
        <label>
          Nombre
          <input name="nombre" defaultValue={nombre} placeholder="Contiene…" />
        </label>
        <label>
          Estado
          <select name="estado" defaultValue={estado ?? ""}>
            <option value="">Todos</option>
            {ESTADOS_TORNEO.map((valor) => (
              <option key={valor} value={valor}>
                {valor}
              </option>
            ))}
          </select>
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
        <ListaTorneos torneos={torneos} params={params} />
      </Suspense>
    </>
  );
}

/** Solo este bloque se vuelve a pedir al filtrar, ordenar o cambiar de página. */
async function ListaTorneos({ torneos: consulta, params }: { torneos: ReturnType<typeof getTorneos>; params: SearchParams }) {
  const torneos = await consulta;

  if (!torneos.ok) {
    return <ErrorMessage error={torneos.error} />;
  }

  if (torneos.data.data.length === 0) {
    return <EmptyState>No hay torneos para los filtros seleccionados.</EmptyState>;
  }

  return (
    <>
      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <SortableHeader label="Nombre" field="nombre" pathname={PATH} searchParams={params} defaultField="fechaInicio" />
              <SortableHeader label="Inicio" field="fechaInicio" pathname={PATH} searchParams={params} defaultField="fechaInicio" />
              <th>Fin</th>
              <SortableHeader label="Estado" field="estado" pathname={PATH} searchParams={params} defaultField="fechaInicio" />
              <th className="num">Equipos</th>
              <th className="num">Partidos</th>
              <th />
            </tr>
          </thead>
          <tbody>
            {torneos.data.data.map((torneo) => (
              <tr key={torneo.id}>
                <td>{torneo.nombre}</td>
                <td className="nowrap">{formatFecha(torneo.fechaInicio)}</td>
                <td className="nowrap">{formatFecha(torneo.fechaFin)}</td>
                <td>
                  <EstadoTorneoBadge estado={torneo.estado} />
                </td>
                <td className="num">{torneo.cantidadEquipos}</td>
                <td className="num">{torneo.cantidadPartidos}</td>
                <td>
                  <div className="row-actions">
                    <Link href={`/torneos/${torneo.id}`}>Ver</Link>
                    {torneo.estado !== "Finalizado" && <Link href={`/torneos/${torneo.id}/editar`}>Editar</Link>}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <Pagination pathname={PATH} searchParams={params} page={torneos.data} />
    </>
  );
}
