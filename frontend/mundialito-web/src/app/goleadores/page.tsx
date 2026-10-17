import type { Metadata } from "next";

import { EmptyState } from "@/components/EmptyState";
import { ErrorMessage } from "@/components/ErrorMessage";
import { Pagination } from "@/components/Pagination";
import { SortableHeader } from "@/components/SortableHeader";
import { TorneoSelector } from "@/components/TorneoSelector";
import { getGoleadores, getTorneoEquipos } from "@/lib/api/torneos.api";
import { pageQuery, param } from "@/lib/search-params";
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
  const [goleadores, equipos] = await Promise.all([
    getGoleadores(actual.id, { ...pageQuery(params, SORT_FIELDS), equipoId, jugador }),
    getTorneoEquipos(actual.id, { pageSize: 100, sortBy: "equipo" }),
  ]);

  return (
    <>
      <h1>Goleadores</h1>
      <TorneoSelector torneos={torneos} actual={actual} action={PATH} />

      <form method="get" action={PATH} className="filters">
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
        <button type="submit">Filtrar</button>
      </form>

      {!goleadores.ok ? (
        <ErrorMessage error={goleadores.error} />
      ) : goleadores.data.data.length === 0 ? (
        <EmptyState>No hay goles registrados para los filtros seleccionados.</EmptyState>
      ) : (
        <>
          <table>
            <thead>
              <tr>
                <SortableHeader label="Pos" field="posicion" pathname={PATH} searchParams={params} defaultField="posicion" />
                <SortableHeader label="Jugador" field="jugador" pathname={PATH} searchParams={params} defaultField="posicion" />
                <th className="num">#</th>
                <th>Posición</th>
                <SortableHeader label="Equipo" field="equipo" pathname={PATH} searchParams={params} defaultField="posicion" />
                <SortableHeader label="Goles" field="goles" pathname={PATH} searchParams={params} defaultField="posicion" />
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
          <Pagination pathname={PATH} searchParams={params} page={goleadores.data} />
        </>
      )}
    </>
  );
}
