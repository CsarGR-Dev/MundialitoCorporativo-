import type { Metadata } from "next";
import Link from "next/link";

import { ActionForm } from "@/components/forms/ActionForm";
import { TorneoFields } from "@/components/forms/TorneoFields";

import { crearTorneoAction } from "../actions";

export const metadata: Metadata = { title: "Nuevo torneo" };

export default function NuevoTorneoPage() {
  return (
    <>
      <Link href="/torneos" className="back-link">
        ← Torneos
      </Link>
      <div className="page-head">
        <div>
          <p className="eyebrow">Torneos</p>
          <h1>Nuevo torneo</h1>
          <p className="page-sub">El torneo se crea como planificado; después se inscriben los equipos y se programa el calendario.</p>
        </div>
      </div>
      <ActionForm
        action={crearTorneoAction}
        submitLabel="Crear torneo"
        pendingLabel="Creando…"
        errorTitle="No se pudo crear el torneo."
        idempotent
        cancelHref="/torneos"
      >
        <TorneoFields />
      </ActionForm>
    </>
  );
}
