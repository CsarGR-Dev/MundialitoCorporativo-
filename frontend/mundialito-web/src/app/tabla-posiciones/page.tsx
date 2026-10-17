import type { Metadata } from "next";

import { EmptyState } from "@/components/EmptyState";
import { ErrorMessage } from "@/components/ErrorMessage";
import { Pagination } from "@/components/Pagination";
import { TorneoSelector } from "@/components/TorneoSelector";
import { getTablaPosiciones } from "@/lib/api/torneos.api";
import { formatDiferencia } from "@/lib/format";
import { pageQuery, param } from "@/lib/search-params";
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
  const tabla = await getTablaPosiciones(actual.id, { ...pageQuery(params), equipo });

  return (
    <>
      <h1>Tabla de posiciones</h1>
      <TorneoSelector torneos={torneos} actual={actual} action={PATH} />

      <form method="get" action={PATH} className="filters">
        <input type="hidden" name="torneo" value={actual.id} />
        <label>
          Equipo
          <input name="equipo" defaultValue={equipo} placeholder="Nombre del equipo" />
        </label>
        <button type="submit">Filtrar</button>
      </form>

      {!tabla.ok ? (
        <ErrorMessage error={tabla.error} />
      ) : tabla.data.data.length === 0 ? (
        <EmptyState>No hay equipos para los filtros seleccionados.</EmptyState>
      ) : (
        <>
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
                  <td className="num">{fila.posicion}</td>
                  <td>{fila.equipo}</td>
                  <td className="num">{fila.partidosJugados}</td>
                  <td className="num">{fila.ganados}</td>
                  <td className="num">{fila.empatados}</td>
                  <td className="num">{fila.perdidos}</td>
                  <td className="num">{fila.golesFavor}</td>
                  <td className="num">{fila.golesContra}</td>
                  <td className="num">{formatDiferencia(fila.diferenciaGol)}</td>
                  <td className="num"><strong>{fila.puntos}</strong></td>
                </tr>
              ))}
            </tbody>
          </table>
          <Pagination pathname={PATH} searchParams={params} page={tabla.data} />
          <p className="muted small">Victoria 3 puntos, empate 1, derrota 0. Orden: puntos, diferencia de gol, goles a favor.</p>
        </>
      )}
    </>
  );
}
