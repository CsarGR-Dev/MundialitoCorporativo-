import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";

import { ErrorMessage } from "@/components/ErrorMessage";
import { EstadoPartidoBadge } from "@/components/EstadoPartidoBadge";
import { getPartido } from "@/lib/api/partidos.api";
import { formatFechaHora, formatMarcador } from "@/lib/format";

export const metadata: Metadata = { title: "Partido" };

export default async function PartidoPage({ params }: PageProps<"/partidos/[id]">) {
  const { id } = await params;
  const partido = await getPartido(id);
  if (!partido.ok) {
    if (partido.error.status === 404) {
      notFound();
    }
    return <ErrorMessage error={partido.error} />;
  }

  const p = partido.data;
  const golesOrdenados = [...p.goles].sort((a, b) => a.minuto - b.minuto);

  return (
    <>
      <p>
        <Link href={`/partidos?torneo=${p.torneoId}`}>← Partidos de {p.torneo}</Link>
      </p>
      <h1>
        {p.equipoLocal} {formatMarcador(p.golesLocal, p.golesVisitante)} {p.equipoVisitante}
      </h1>
      <p>
        <EstadoPartidoBadge estado={p.estado} /> · {formatFechaHora(p.fechaProgramada)} · {p.sede ?? "Sin sede"}
      </p>

      {p.estado === "Programado" && (
        <p>
          <Link href={`/partidos/${p.id}/resultado`}>Registrar resultado</Link>
        </p>
      )}

      {p.estado === "Jugado" && (
        <>
          <h2>Goles</h2>
          {golesOrdenados.length === 0 ? (
            <p className="muted">Partido sin goles.</p>
          ) : (
            <table>
              <thead>
                <tr>
                  <th className="num">Minuto</th>
                  <th>Jugador</th>
                  <th>Equipo</th>
                </tr>
              </thead>
              <tbody>
                {golesOrdenados.map((gol) => (
                  <tr key={gol.id}>
                    <td className="num">{gol.minuto}&apos;</td>
                    <td>{gol.jugador}</td>
                    <td>{gol.equipo}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </>
      )}
    </>
  );
}
