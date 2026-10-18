import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";

import { ErrorMessage } from "@/components/ErrorMessage";
import { ActionForm } from "@/components/forms/ActionForm";
import { JugadorFields } from "@/components/forms/JugadorFields";
import { getEquipo, getPosicionesJuego } from "@/lib/api/equipos.api";

import { registrarIntegranteAction } from "../../../actions";

export const metadata: Metadata = { title: "Registrar integrante" };

/** Registro de jugadores por equipo (ítem 8). El número de camiseta no puede repetirse dentro del equipo. */
export default async function NuevoIntegrantePage({ params }: PageProps<"/equipos/[id]/jugadores/nuevo">) {
  const { id } = await params;
  const [equipo, posiciones] = await Promise.all([getEquipo(id), getPosicionesJuego()]);
  if (!equipo.ok) {
    if (equipo.error.status === 404) {
      notFound();
    }
    return <ErrorMessage error={equipo.error} />;
  }
  if (!posiciones.ok) {
    return <ErrorMessage error={posiciones.error} />;
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
          <p className="eyebrow">{e.nombre}</p>
          <h1>Registrar integrante</h1>
        </div>
      </div>
      <ActionForm
        action={registrarIntegranteAction.bind(null, e.id)}
        submitLabel="Registrar integrante"
        pendingLabel="Registrando…"
        errorTitle="No se pudo registrar el integrante."
        idempotent
        cancelHref={detalle}
      >
        <JugadorFields posiciones={posiciones.data.data} />
      </ActionForm>
    </>
  );
}
