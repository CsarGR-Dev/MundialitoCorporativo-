import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";

import { ErrorMessage } from "@/components/ErrorMessage";
import { ActionForm } from "@/components/forms/ActionForm";
import { EquipoFields } from "@/components/forms/EquipoFields";
import { getEquipo } from "@/lib/api/equipos.api";

import { actualizarEquipoAction } from "../../actions";

export const metadata: Metadata = { title: "Editar equipo" };

export default async function EditarEquipoPage({ params }: PageProps<"/equipos/[id]/editar">) {
  const { id } = await params;
  const equipo = await getEquipo(id);
  if (!equipo.ok) {
    if (equipo.error.status === 404) {
      notFound();
    }
    return <ErrorMessage error={equipo.error} />;
  }

  const e = equipo.data;
  const detalle = `/equipos/${e.id}`;

  return (
    <>
      <Link href={detalle} className="back-link">
        ← {e.nombre}
      </Link>
      <div className="page-head">
        <div>
          <p className="eyebrow">Equipos</p>
          <h1>Editar equipo</h1>
        </div>
      </div>
      <ActionForm
        action={actualizarEquipoAction.bind(null, e.id)}
        submitLabel="Guardar cambios"
        pendingLabel="Guardando…"
        errorTitle="No se pudo guardar el equipo."
        cancelHref={detalle}
      >
        <EquipoFields equipo={e} />
      </ActionForm>
    </>
  );
}
