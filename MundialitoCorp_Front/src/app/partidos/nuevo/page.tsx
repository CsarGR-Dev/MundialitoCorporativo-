import type { Metadata } from "next";
import Link from "next/link";

import { EmptyState } from "@/components/EmptyState";
import { ErrorMessage } from "@/components/ErrorMessage";
import { ActionForm } from "@/components/forms/ActionForm";
import { PartidoFields } from "@/components/forms/PartidoFields";
import { TorneoSelector } from "@/components/TorneoSelector";
import { getTorneoEquipos } from "@/lib/api/torneos.api";
import { formatFecha } from "@/lib/format";
import { param } from "@/lib/search-params";
import { resolveTorneo } from "@/lib/torneo-actual";

import { programarPartidoAction } from "../actions";

export const metadata: Metadata = { title: "Programar partido" };

const PATH = "/partidos/nuevo";

/** Calendario de partidos (ítem 8): un partido entre dos equipos inscritos, dentro de las fechas del torneo. */
export default async function ProgramarPartidoPage({ searchParams }: PageProps<"/partidos/nuevo">) {
  const params = await searchParams;
  const seleccion = await resolveTorneo(param(params, "torneo"));
  if (!seleccion.ok) {
    return <ErrorMessage error={seleccion.error} />;
  }

  const { torneos, actual } = seleccion.data;
  if (!actual) {
    return (
      <EmptyState>
        No hay torneos registrados. <Link href="/torneos/nuevo">Crear un torneo</Link>
      </EmptyState>
    );
  }

  const volver = `/partidos?torneo=${actual.id}`;
  const equipos = await getTorneoEquipos(actual.id, { pageSize: 100, sortBy: "equipo" });

  return (
    <>
      <Link href={volver} className="back-link">
        ← Partidos de {actual.nombre}
      </Link>
      <div className="page-head">
        <div>
          <p className="eyebrow">{actual.nombre}</p>
          <h1>Programar partido</h1>
          <p className="page-sub">
            Del {formatFecha(actual.fechaInicio)} al {formatFecha(actual.fechaFin)}. Solo participan los equipos inscritos en el torneo.
          </p>
        </div>
        <TorneoSelector torneos={torneos} actual={actual} action={PATH} />
      </div>

      {actual.estado === "Finalizado" ? (
        <EmptyState>El torneo está finalizado y no admite partidos nuevos.</EmptyState>
      ) : !equipos.ok ? (
        <ErrorMessage error={equipos.error} />
      ) : equipos.data.data.length < 2 ? (
        <EmptyState>
          Se necesitan al menos dos equipos inscritos. <Link href={`/torneos/${actual.id}`}>Inscribir equipos</Link>
        </EmptyState>
      ) : (
        <ActionForm
          action={programarPartidoAction.bind(null, actual.id)}
          submitLabel="Programar partido"
          pendingLabel="Programando…"
          errorTitle="No se pudo programar el partido."
          idempotent
          cancelHref={volver}
        >
          <PartidoFields equipos={equipos.data.data} />
        </ActionForm>
      )}
    </>
  );
}
