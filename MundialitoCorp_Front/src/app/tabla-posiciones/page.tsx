import type { Metadata } from "next";
import { Suspense } from "react";

import { EmptyState } from "@/components/EmptyState";
import { ErrorMessage } from "@/components/ErrorMessage";
import { FilterForm } from "@/components/FilterForm";
import { Pagination } from "@/components/Pagination";
import { TableSkeleton } from "@/components/TableSkeleton";
import { TorneoSelector } from "@/components/TorneoSelector";
import { getTablaPosiciones } from "@/lib/api/torneos.api";
import { formatDiferencia } from "@/lib/format";
import { pageQuery, param, searchKey, type SearchParams } from "@/lib/search-params";
import { resolveTorneo } from "@/lib/torneo-actual";

export const metadata: Metadata = { title: "Tabla de posiciones" };

const PATH = "/tabla-posiciones";

/**
 * Tabla automática del torneo, en el orden oficial: puntos, diferencia de gol y goles a favor (ítem 8).
 * La calcula la API con Dapper a partir de los partidos jugados.
 */
export default async function TablaPosicionesPage({ searchParams }: PageProps<"/tabla-posiciones">) {
  const params = await searchParams;
  const seleccion = await resolveTorneo(param(params, "torneo"));
  if (!seleccion.ok) {
    return <ErrorMessage error={seleccion.error} />;
  }

  const { torneos, actual } = seleccion.data;
  if (!actual) {
    return <EmptyState>No hay torneos registrados.</EmptyState>;
  }

  const equipo = param(params, "equipo");
  // Sin await: la tabla viaja al bloque de resultados y se transmite cuando llega, sin frenar la página.
  const tabla = getTablaPosiciones(actual.id, { ...pageQuery(params), equipo });

  return (
    <>
      <div className="page-head">
        <div>
          <p className="eyebrow">{actual.nombre}</p>
          <h1>Tabla de posiciones</h1>
          <p className="page-sub">
            Victoria 3 puntos, empate 1 y derrota 0. El orden es por puntos, diferencia de gol y goles a favor.
          </p>
        </div>
        <TorneoSelector torneos={torneos} actual={actual} action={PATH} />
      </div>

      <FilterForm action={PATH} limpiar={`${PATH}?torneo=${actual.id}`}>
        <input type="hidden" name="torneo" value={actual.id} />
        <label>
          Equipo
          <input name="equipo" defaultValue={equipo} placeholder="Nombre del equipo" />
        </label>
      </FilterForm>

      <Suspense key={searchKey(params)} fallback={<TableSkeleton />}>
        <TablaDelTorneo tabla={tabla} params={params} />
      </Suspense>
    </>
  );
}

/** Solo este bloque se vuelve a pedir al filtrar o cambiar de página. */
async function TablaDelTorneo({ tabla: consulta, params }: { tabla: ReturnType<typeof getTablaPosiciones>; params: SearchParams }) {
  const tabla = await consulta;

  if (!tabla.ok) {
    return <ErrorMessage error={tabla.error} />;
  }

  if (tabla.data.data.length === 0) {
    return <EmptyState>No hay equipos para los filtros seleccionados.</EmptyState>;
  }

  return (
    <>
      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <th className="num">Pos</th>
              <th>Equipo</th>
              <th className="num" title="Partidos jugados">PJ</th>
              <th className="num" title="Ganados">G</th>
              <th className="num" title="Empatados">E</th>
              <th className="num" title="Perdidos">P</th>
              <th className="num" title="Goles a favor">GF</th>
              <th className="num" title="Goles en contra">GC</th>
              <th className="num" title="Diferencia de gol">DG</th>
              <th className="num" title="Puntos">Pts</th>
            </tr>
          </thead>
          <tbody>
            {tabla.data.data.map((fila) => (
              <tr key={fila.equipoId}>
                <td className="num pos">{fila.posicion}</td>
                <td>{fila.equipo}</td>
                <td className="num">{fila.partidosJugados}</td>
                <td className="num">{fila.ganados}</td>
                <td className="num">{fila.empatados}</td>
                <td className="num">{fila.perdidos}</td>
                <td className="num">{fila.golesFavor}</td>
                <td className="num">{fila.golesContra}</td>
                <td className="num">{formatDiferencia(fila.diferenciaGol)}</td>
                <td className="num pts"><strong>{fila.puntos}</strong></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <Pagination pathname={PATH} searchParams={params} page={tabla.data} />
    </>
  );
}
