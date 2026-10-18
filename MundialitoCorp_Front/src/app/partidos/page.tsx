import type { Metadata } from "next";
import Link from "next/link";
import { Suspense } from "react";

import { EmptyState } from "@/components/EmptyState";
import { ErrorMessage } from "@/components/ErrorMessage";
import { EstadoPartidoBadge } from "@/components/EstadoPartidoBadge";
import { FilterForm } from "@/components/FilterForm";
import { Pagination } from "@/components/Pagination";
import { SortableHeader } from "@/components/SortableHeader";
import { TableSkeleton } from "@/components/TableSkeleton";
import { TorneoSelector } from "@/components/TorneoSelector";
import { getPartidos } from "@/lib/api/partidos.api";
import { getTorneoEquipos } from "@/lib/api/torneos.api";
import { formatFechaHora, formatMarcador } from "@/lib/format";
import { pageQuery, param, searchKey, type SearchParams } from "@/lib/search-params";
import { resolveTorneo } from "@/lib/torneo-actual";
import { ESTADOS_PARTIDO } from "@/lib/types/models";

export const metadata: Metadata = { title: "Partidos" };

const PATH = "/partidos";
const SORT_FIELDS = ["fechaProgramada", "estado", "torneo"] as const;

/** Calendario del torneo con filtros por rango de fechas, equipo y estado (ítem 8). */
export default async function PartidosPage({ searchParams }: PageProps<"/partidos">) {
  const params = await searchParams;
  const seleccion = await resolveTorneo(param(params, "torneo"));
  if (!seleccion.ok) {
    return <ErrorMessage error={seleccion.error} />;
  }

  const { torneos, actual } = seleccion.data;
  if (!actual) {
    return <EmptyState>No hay torneos registrados.</EmptyState>;
  }

  const filtros = {
    fechaDesde: param(params, "fechaDesde"),
    fechaHasta: param(params, "fechaHasta"),
    equipoId: param(params, "equipoId"),
    estado: param(params, "estado"),
  };

  // El calendario se pide sin await: viaja al bloque de resultados y se transmite cuando llega, en paralelo
  // con los equipos del filtro, que sí hacen falta para pintar el formulario.
  const partidos = getPartidos({ ...pageQuery(params, SORT_FIELDS), torneoId: actual.id, ...filtros });
  const equipos = await getTorneoEquipos(actual.id, { pageSize: 100, sortBy: "equipo" });

  return (
    <>
      <div className="page-head">
        <div>
          <p className="eyebrow">{actual.nombre}</p>
          <h1>Partidos</h1>
          <p className="page-sub">Calendario del torneo con el resultado de los partidos jugados.</p>
        </div>
        <div className="page-actions">
          <TorneoSelector torneos={torneos} actual={actual} action={PATH} />
          {actual.estado !== "Finalizado" && (
            <Link href={`${PATH}/nuevo?torneo=${actual.id}`} className="btn-link">
              Programar partido
            </Link>
          )}
        </div>
      </div>

      <FilterForm action={PATH} limpiar={`${PATH}?torneo=${actual.id}`}>
        <input type="hidden" name="torneo" value={actual.id} />
        <label>
          Desde
          <input type="date" name="fechaDesde" defaultValue={filtros.fechaDesde} />
        </label>
        <label>
          Hasta
          <input type="date" name="fechaHasta" defaultValue={filtros.fechaHasta} />
        </label>
        <label>
          Equipo
          <select name="equipoId" defaultValue={filtros.equipoId ?? ""}>
            <option value="">Todos</option>
            {equipos.ok &&
              equipos.data.data.map((equipo) => (
                <option key={equipo.equipoId} value={equipo.equipoId}>
                  {equipo.equipo}
                </option>
              ))}
          </select>
        </label>
        <label>
          Estado
          <select name="estado" defaultValue={filtros.estado ?? ""}>
            <option value="">Todos</option>
            {ESTADOS_PARTIDO.map((estado) => (
              <option key={estado} value={estado}>
                {estado}
              </option>
            ))}
          </select>
        </label>
      </FilterForm>

      <Suspense key={searchKey(params)} fallback={<TableSkeleton />}>
        <CalendarioPartidos partidos={partidos} params={params} />
      </Suspense>
    </>
  );
}

/** Solo este bloque se vuelve a pedir al filtrar, ordenar o cambiar de página. */
async function CalendarioPartidos({
  partidos: consulta,
  params,
}: {
  partidos: ReturnType<typeof getPartidos>;
  params: SearchParams;
}) {
  const partidos = await consulta;

  if (!partidos.ok) {
    return <ErrorMessage error={partidos.error} />;
  }

  if (partidos.data.data.length === 0) {
    return <EmptyState>No hay partidos para los filtros seleccionados.</EmptyState>;
  }

  return (
    <>
      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <SortableHeader label="Fecha" field="fechaProgramada" pathname={PATH} searchParams={params} defaultField="fechaProgramada" />
              <th>Local</th>
              <th>Marcador</th>
              <th>Visitante</th>
              <th>Sede</th>
              <SortableHeader label="Estado" field="estado" pathname={PATH} searchParams={params} defaultField="fechaProgramada" />
              <th />
            </tr>
          </thead>
          <tbody>
            {partidos.data.data.map((partido) => (
              <tr key={partido.id}>
                <td className="nowrap">{formatFechaHora(partido.fechaProgramada)}</td>
                <td>{partido.equipoLocal}</td>
                <td className="nowrap">
                  <strong>{formatMarcador(partido.golesLocal, partido.golesVisitante)}</strong>
                </td>
                <td>{partido.equipoVisitante}</td>
                <td className="nowrap">{partido.sede ?? "—"}</td>
                <td><EstadoPartidoBadge estado={partido.estado} /></td>
                <td>
                  <div className="row-actions">
                    <Link href={`/partidos/${partido.id}`}>Ver</Link>
                    {partido.estado === "Programado" && (
                      <>
                        <Link href={`/partidos/${partido.id}/resultado`} title="Registrar resultado">
                        Resultado
                      </Link>
                        <Link href={`/partidos/${partido.id}/editar`}>Editar</Link>
                      </>
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <Pagination pathname={PATH} searchParams={params} page={partidos.data} />
    </>
  );
}
