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
  const volver = (
    <Link href={`/partidos/${p.id}`} className="back-link">
      ← Volver al partido
    </Link>
  );

  if (p.estado !== "Programado") {
    return (
      <>
        {volver}
        <div className="page-head">
          <div>
            <p className="eyebrow">{p.equipoLocal} vs {p.equipoVisitante}</p>
            <h1>Registrar resultado</h1>
          </div>
        </div>
        <div className="alert alert-ok">
          <div className="alert-body">
            {p.estado === "Jugado" ? "Este partido ya tiene su resultado registrado." : "El partido está cancelado; no admite resultado."}
          </div>
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
      {volver}
      <div className="page-head">
        <div>
          <p className="eyebrow">
            {p.equipoLocal} vs {p.equipoVisitante}
          </p>
          <h1>Registrar resultado</h1>
          <p className="page-sub">
            {formatFechaHora(p.fechaProgramada)} · {p.sede ?? "Sin sede"} · Indique el marcador y, por cada gol, el goleador y el minuto.
          </p>
        </div>
      </div>
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
