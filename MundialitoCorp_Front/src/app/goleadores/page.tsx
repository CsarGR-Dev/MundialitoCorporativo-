import type { Metadata } from "next";
import { Suspense } from "react";

import { EmptyState } from "@/components/EmptyState";
import { ErrorMessage } from "@/components/ErrorMessage";
import { FilterForm } from "@/components/FilterForm";
import { Pagination } from "@/components/Pagination";
import { SortableHeader } from "@/components/SortableHeader";
import { TableSkeleton } from "@/components/TableSkeleton";
import { TorneoSelector } from "@/components/TorneoSelector";
import { getGoleadores, getTorneoEquipos } from "@/lib/api/torneos.api";
import { pageQuery, param, searchKey, type SearchParams } from "@/lib/search-params";
import { resolveTorneo } from "@/lib/torneo-actual";

export const metadata: Metadata = { title: "Goleadores" };

const PATH = "/goleadores";
const SORT_FIELDS = ["posicion", "goles", "jugador", "equipo"] as const;

export default async function GoleadoresPage({ searchParams }: PageProps<"/goleadores">) {
  const params = await searchParams;
  const seleccion = await resolveTorneo(param(params, "torneo"));
  if (!seleccion.ok) {
    return <ErrorMessage error={seleccion.error} />;
  }

  const { torneos, actual } = seleccion.data;
  if (!actual) {
    return <EmptyState>No hay torneos registrados.</EmptyState>;
  }

  const equipoId = param(params, "equipoId");
  const jugador = param(params, "jugador");
  // El ranking se pide sin await: viaja al bloque de resultados y se transmite cuando llega, en paralelo
  // con los equipos del filtro, que sí hacen falta para pintar el formulario.
  const goleadores = getGoleadores(actual.id, { ...pageQuery(params, SORT_FIELDS), equipoId, jugador });
  const equipos = await getTorneoEquipos(actual.id, { pageSize: 100, sortBy: "equipo" });

  return (
    <>
      <div className="page-head">
        <div>
          <p className="eyebrow">{actual.nombre}</p>
          <h1>Goleadores</h1>
          <p className="page-sub">Jugadores con más goles en el torneo. Cada columna con flecha ordena el ranking.</p>
        </div>
        <TorneoSelector torneos={torneos} actual={actual} action={PATH} />
      </div>

      <FilterForm action={PATH} limpiar={`${PATH}?torneo=${actual.id}`}>
        <input type="hidden" name="torneo" value={actual.id} />
        <label>
          Equipo
          <select name="equipoId" defaultValue={equipoId ?? ""}>
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
          Jugador
          <input name="jugador" defaultValue={jugador} placeholder="Nombre o apellido" />
        </label>
      </FilterForm>

      <Suspense key={searchKey(params)} fallback={<TableSkeleton />}>
        <RankingGoleadores goleadores={goleadores} params={params} />
      </Suspense>
    </>
  );
}

/** Solo este bloque se vuelve a pedir al filtrar, ordenar o cambiar de página. */
async function RankingGoleadores({
  goleadores: consulta,
  params,
}: {
  goleadores: ReturnType<typeof getGoleadores>;
  params: SearchParams;
}) {
  const goleadores = await consulta;

  if (!goleadores.ok) {
    return <ErrorMessage error={goleadores.error} />;
  }

  if (goleadores.data.data.length === 0) {
    return <EmptyState>No hay goles registrados para los filtros seleccionados.</EmptyState>;
  }

  return (
    <>
      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <SortableHeader label="Pos" field="posicion" pathname={PATH} searchParams={params} defaultField="posicion" className="num" />
              <SortableHeader label="Jugador" field="jugador" pathname={PATH} searchParams={params} defaultField="posicion" />
              <th className="num">#</th>
              <th>Posición</th>
              <SortableHeader label="Equipo" field="equipo" pathname={PATH} searchParams={params} defaultField="posicion" />
              <SortableHeader label="Goles" field="goles" pathname={PATH} searchParams={params} defaultField="posicion" className="num" />
              <th className="num">Partidos con gol</th>
            </tr>
          </thead>
          <tbody>
            {goleadores.data.data.map((goleador) => (
              <tr key={goleador.jugadorId}>
                <td className="num">{goleador.posicion}</td>
                <td>{goleador.jugador}</td>
                <td className="num">{goleador.numeroCamiseta}</td>
                <td>{goleador.posicionJuego}</td>
                <td>{goleador.equipo}</td>
                <td className="num"><strong>{goleador.goles}</strong></td>
                <td className="num">{goleador.partidosConGol}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <Pagination pathname={PATH} searchParams={params} page={goleadores.data} />
    </>
  );
}
