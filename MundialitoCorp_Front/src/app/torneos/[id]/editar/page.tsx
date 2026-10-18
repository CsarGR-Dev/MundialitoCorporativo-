import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";

import { ErrorMessage } from "@/components/ErrorMessage";
import { ActionForm } from "@/components/forms/ActionForm";
import { TorneoFields } from "@/components/forms/TorneoFields";
import { getTorneo } from "@/lib/api/torneos.api";

import { actualizarTorneoAction } from "../../actions";

export const metadata: Metadata = { title: "Editar torneo" };

/** Edición completa (PUT). Un torneo finalizado no admite cambios: la API responde 409 y se muestra el motivo. */
export default async function EditarTorneoPage({ params }: PageProps<"/torneos/[id]/editar">) {
  const { id } = await params;
  const torneo = await getTorneo(id);
  if (!torneo.ok) {
    if (torneo.error.status === 404) {
      notFound();
    }
    return <ErrorMessage error={torneo.error} />;
  }

  const t = torneo.data;
  const detalle = `/torneos/${t.id}`;

  return (
    <>
      <Link href={detalle} className="back-link">
        ← {t.nombre}
      </Link>
      <div className="page-head">
        <div>
          <p className="eyebrow">Torneos</p>
          <h1>Editar torneo</h1>
        </div>
      </div>
      <ActionForm
        action={actualizarTorneoAction.bind(null, t.id)}
        submitLabel="Guardar cambios"
        pendingLabel="Guardando…"
        errorTitle="No se pudo guardar el torneo."
        cancelHref={detalle}
      >
        <TorneoFields torneo={t} />
      </ActionForm>
    </>
  );
}
