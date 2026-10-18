import type { Metadata } from "next";
import Link from "next/link";

import { ActionForm } from "@/components/forms/ActionForm";
import { EquipoFields } from "@/components/forms/EquipoFields";

import { crearEquipoAction } from "../actions";

export const metadata: Metadata = { title: "Nuevo equipo" };

export default function NuevoEquipoPage() {
  return (
    <>
      <Link href="/equipos" className="back-link">
        ← Equipos
      </Link>
      <div className="page-head">
        <div>
          <p className="eyebrow">Equipos</p>
          <h1>Nuevo equipo</h1>
          <p className="page-sub">Después de crearlo se registran sus integrantes y se inscribe en un torneo.</p>
        </div>
      </div>
      <ActionForm
        action={crearEquipoAction}
        submitLabel="Crear equipo"
        pendingLabel="Creando…"
        errorTitle="No se pudo crear el equipo."
        idempotent
        cancelHref="/equipos"
      >
        <EquipoFields />
      </ActionForm>
    </>
  );
}
