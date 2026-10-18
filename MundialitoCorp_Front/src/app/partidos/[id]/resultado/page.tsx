import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";

import { ErrorMessage } from "@/components/ErrorMessage";
import { ResultadoForm } from "@/components/forms/ResultadoForm";
import { getJugadoresActivos, getPartido } from "@/lib/api/partidos.api";
import { formatFechaHora } from "@/lib/format";
import type { Jugador } from "@/lib/types/models";

import { registrarResultadoAction } from "./actions";

export const metadata: Metadata = { title: "Registrar resultado" };

export default async function RegistrarResultadoPage({ params }: PageProps<"/partidos/[id]/resultado">) {
  const { id } = await params;
  const partido = await getPartido(id);
  if (!partido.ok) {
    if (partido.error.status === 404) {
      notFound();
    }
    return <ErrorMessage error={partido.error} />;
  }

  const p = partido.data;
  const volver = <Link href={`/partidos/${p.id}`}>← Volver al partido</Link>;

  if (p.estado !== "Programado") {
    return (
      <>
        <p>{volver}</p>
        <h1>Registrar resultado</h1>
        <div className="alert alert-ok">
          {p.estado === "Jugado" ? "Este partido ya tiene su resultado registrado." : "El partido está cancelado; no admite resultado."}
        </div>
      </>
    );
  }

  const [jugadoresLocal, jugadoresVisitante] = await Promise.all([
    getJugadoresActivos(p.equipoLocalId),
    getJugadoresActivos(p.equipoVisitanteId),
  ]);
  if (!jugadoresLocal.ok) {
    return <ErrorMessage error={jugadoresLocal.error} />;
  }
  if (!jugadoresVisitante.ok) {
    return <ErrorMessage error={jugadoresVisitante.error} />;
  }

  return (
    <>
      <p>{volver}</p>
      <h1>Registrar resultado</h1>
      <p>
        {p.equipoLocal} vs {p.equipoVisitante} · {formatFechaHora(p.fechaProgramada)} · {p.sede ?? "Sin sede"}
      </p>
      <ResultadoForm
        local={{ id: p.equipoLocalId, nombre: p.equipoLocal, jugadores: toOpciones(jugadoresLocal.data.data) }}
        visitante={{ id: p.equipoVisitanteId, nombre: p.equipoVisitante, jugadores: toOpciones(jugadoresVisitante.data.data) }}
        action={registrarResultadoAction.bind(null, p.id)}
      />
    </>
  );
}

function toOpciones(jugadores: Jugador[]) {
  return jugadores.map((jugador) => ({
    id: jugador.id,
    nombre: `${jugador.nombres} ${jugador.apellidos}`,
    numeroCamiseta: jugador.numeroCamiseta,
  }));
}
