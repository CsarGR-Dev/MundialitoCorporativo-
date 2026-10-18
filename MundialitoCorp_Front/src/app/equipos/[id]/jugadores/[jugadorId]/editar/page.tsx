import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";

import { ErrorMessage } from "@/components/ErrorMessage";
import { ActionForm } from "@/components/forms/ActionForm";
import { JugadorFields } from "@/components/forms/JugadorFields";
import { getPosicionesJuego } from "@/lib/api/equipos.api";
import { getJugador } from "@/lib/api/jugadores.api";

import { actualizarIntegranteAction } from "../../../../actions";

export const metadata: Metadata = { title: "Editar integrante" };

export default async function EditarIntegrantePage({ params }: PageProps<"/equipos/[id]/jugadores/[jugadorId]/editar">) {
  const { id, jugadorId } = await params;
  const [jugador, posiciones] = await Promise.all([getJugador(jugadorId), getPosicionesJuego()]);
  if (!jugador.ok) {
    if (jugador.error.status === 404) {
      notFound();
    }
    return <ErrorMessage error={jugador.error} />;
  }
  if (!posiciones.ok) {
    return <ErrorMessage error={posiciones.error} />;
  }

  const j = jugador.data;
  // La ruta anida al jugador en su equipo: otro equipo en la URL es una dirección que no existe.
  if (j.equipoId !== id) {
    notFound();
  }
  const detalle = `/equipos/${j.equipoId}`;

  return (
    <>
      <Link href={detalle} className="back-link">
        ← {j.equipo}
      </Link>
      <div className="page-head">
        <div>
          <p className="eyebrow">{j.equipo}</p>
          <h1>Editar integrante</h1>
        </div>
      </div>
      <ActionForm
        action={actualizarIntegranteAction.bind(null, j.equipoId, j.id)}
        submitLabel="Guardar cambios"
        pendingLabel="Guardando…"
        errorTitle="No se pudo guardar el integrante."
        cancelHref={detalle}
      >
        <JugadorFields jugador={j} posiciones={posiciones.data.data} />
      </ActionForm>
    </>
  );
}
