import type { Metadata } from "next";
import Link from "next/link";

import { EmptyState } from "@/components/EmptyState";
import { ErrorMessage } from "@/components/ErrorMessage";
import { EstadoPartidoBadge } from "@/components/EstadoPartidoBadge";
import { Pagination } from "@/components/Pagination";
import { SortableHeader } from "@/components/SortableHeader";
import { TorneoSelector } from "@/components/TorneoSelector";
import { getPartidos } from "@/lib/api/partidos.api";
import { getTorneoEquipos } from "@/lib/api/torneos.api";
import { formatFechaHora, formatMarcador } from "@/lib/format";
import { pageQuery, param } from "@/lib/search-params";
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

  const [partidos, equipos] = await Promise.all([
    getPartidos({ ...pageQuery(params, SORT_FIELDS), torneoId: actual.id, ...filtros }),
    getTorneoEquipos(actual.id, { pageSize: 100, sortBy: "equipo" }),
  ]);

  return (
    <>
      <h1>Partidos</h1>
      <TorneoSelector torneos={torneos} actual={actual} action={PATH} />

      <form method="get" action={PATH} className="filters">
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
        <button type="submit">Filtrar</button>
        <Link href={`${PATH}?torneo=${actual.id}`}>Limpiar</Link>
      </form>

      {!partidos.ok ? (
        <ErrorMessage error={partidos.error} />
      ) : partidos.data.data.length === 0 ? (
        <EmptyState>No hay partidos para los filtros seleccionados.</EmptyState>
      ) : (
        <>
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
                  <td>{formatFechaHora(partido.fechaProgramada)}</td>
                  <td>{partido.equipoLocal}</td>
                  <td><strong>{formatMarcador(partido.golesLocal, partido.golesVisitante)}</strong></td>
                  <td>{partido.equipoVisitante}</td>
                  <td>{partido.sede ?? "—"}</td>
                  <td><EstadoPartidoBadge estado={partido.estado} /></td>
                  <td>
                    <Link href={`/partidos/${partido.id}`}>Ver</Link>
                    {partido.estado === "Programado" && (
                      <>
                        {" · "}
                        <Link href={`/partidos/${partido.id}/resultado`}>Registrar resultado</Link>
                      </>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          <Pagination pathname={PATH} searchParams={params} page={partidos.data} />
        </>
      )}
    </>
  );
}
