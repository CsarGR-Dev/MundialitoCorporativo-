import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";

import { ErrorMessage } from "@/components/ErrorMessage";
import { ActionForm } from "@/components/forms/ActionForm";
import { PartidoFields } from "@/components/forms/PartidoFields";
import { getPartido } from "@/lib/api/partidos.api";
import { getTorneoEquipos } from "@/lib/api/torneos.api";

import { actualizarPartidoAction } from "../../actions";

export const metadata: Metadata = { title: "Reprogramar partido" };

/** Reprogramación completa (PUT): equipos, fecha y sede. Solo para partidos programados. */
export default async function EditarPartidoPage({ params }: PageProps<"/partidos/[id]/editar">) {
  const { id } = await params;
  const partido = await getPartido(id);
  if (!partido.ok) {
    if (partido.error.status === 404) {
      notFound();
    }
    return <ErrorMessage error={partido.error} />;
  }

  const p = partido.data;
  const detalle = `/partidos/${p.id}`;
  const encabezado = (
    <>
      <Link href={detalle} className="back-link">
        ← Volver al partido
      </Link>
      <div className="page-head">
        <div>
          <p className="eyebrow">
            {p.torneo} · {p.equipoLocal} vs {p.equipoVisitante}
          </p>
          <h1>Reprogramar partido</h1>
        </div>
      </div>
    </>
  );

  if (p.estado !== "Programado") {
    return (
      <>
        {encabezado}
        <div className="alert alert-ok">
          <div className="alert-body">
            {p.estado === "Jugado" ? "El partido ya se jugó; no puede reprogramarse." : "El partido está cancelado; no puede reprogramarse."}
          </div>
        </div>
      </>
    );
  }

  const equipos = await getTorneoEquipos(p.torneoId, { pageSize: 100, sortBy: "equipo" });
  if (!equipos.ok) {
    return <ErrorMessage error={equipos.error} />;
  }

  return (
    <>
      {encabezado}
      <ActionForm
        action={actualizarPartidoAction.bind(null, p.id)}
        submitLabel="Guardar cambios"
        pendingLabel="Guardando…"
        errorTitle="No se pudo reprogramar el partido."
        cancelHref={detalle}
      >
        <PartidoFields partido={p} equipos={equipos.data.data} />
      </ActionForm>
    </>
  );
}
