import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";

import { EmptyState } from "@/components/EmptyState";
import { ErrorMessage } from "@/components/ErrorMessage";
import { EstadoPartidoBadge } from "@/components/EstadoPartidoBadge";
import { ConfirmActionButton } from "@/components/forms/ConfirmActionButton";
import { getPartido } from "@/lib/api/partidos.api";
import { formatFechaHora, formatMarcador } from "@/lib/format";

import { cancelarPartidoAction, eliminarPartidoAction } from "../actions";

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
      <Link href={`/partidos?torneo=${p.torneoId}`} className="back-link">
        ← Partidos de {p.torneo}
      </Link>

      <h1 className="scoreboard">
        <span className="equipo">{p.equipoLocal}</span>
        <span className="marcador">{formatMarcador(p.golesLocal, p.golesVisitante)}</span>
        <span className="equipo">{p.equipoVisitante}</span>
      </h1>

      <p className="meta-row">
        <EstadoPartidoBadge estado={p.estado} />
        <span>{formatFechaHora(p.fechaProgramada)}</span>
        <span>·</span>
        <span>{p.sede ?? "Sin sede"}</span>
      </p>

      {p.estado !== "Jugado" && (
        <div className="actions-row page-actions">
          {p.estado === "Programado" && (
            <>
              <Link href={`/partidos/${p.id}/resultado`} className="btn-link">
                Registrar resultado
              </Link>
              <Link href={`/partidos/${p.id}/editar`} className="btn-secondary">
                Reprogramar
              </Link>
              <ConfirmActionButton
                action={cancelarPartidoAction.bind(null, p.id)}
                label="Cancelar partido"
                pendingLabel="Cancelando…"
                confirmMessage="¿Cancelar el partido? Un partido cancelado no admite resultado."
                errorTitle="No se pudo cancelar el partido."
              />
            </>
          )}
          <ConfirmActionButton
            action={eliminarPartidoAction.bind(null, p.id, p.torneoId)}
            label="Eliminar"
            pendingLabel="Eliminando…"
            confirmMessage="¿Eliminar el partido del calendario?"
            errorTitle="No se pudo eliminar el partido."
            variant="danger"
          />
        </div>
      )}

      {p.estado === "Jugado" && (
        <>
          <h2>Goles</h2>
          {golesOrdenados.length === 0 ? (
            <EmptyState>Partido sin goles.</EmptyState>
          ) : (
            <div className="table-wrap">
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
            </div>
          )}
        </>
      )}
    </>
  );
}
