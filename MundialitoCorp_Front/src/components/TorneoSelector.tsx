"use client";

import { useRouter } from "next/navigation";
import { useTransition, type FormEvent } from "react";

import type { Torneo } from "@/lib/types/models";

const ETIQUETA_ESTADO: Record<Torneo["estado"], string> = {
  Planificado: "planificado",
  EnCurso: "en curso",
  Finalizado: "finalizado",
};

/**
 * Elige el torneo de la vista; queda en la URL (?torneo=). Basta elegir en la lista: la navegación es
 * del lado del cliente, igual que la de los filtros, así no se recarga la página.
 */
export function TorneoSelector({ torneos, actual, action }: { torneos: Torneo[]; actual: Torneo; action: string }) {
  const router = useRouter();
  const [pendiente, iniciar] = useTransition();

  function ver(torneoId: string) {
    if (torneoId && torneoId !== actual.id) {
      iniciar(() => router.push(`${action}?torneo=${torneoId}`, { scroll: false }));
    }
  }

  function enviar(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    ver(String(new FormData(event.currentTarget).get("torneo") ?? ""));
  }

  return (
    <form method="get" action={action} className="torneo-selector" onSubmit={enviar}>
      <label>
        Torneo{" "}
        <select name="torneo" defaultValue={actual.id} onChange={(event) => ver(event.target.value)}>
          {torneos.map((torneo) => (
            <option key={torneo.id} value={torneo.id}>
              {torneo.nombre} ({ETIQUETA_ESTADO[torneo.estado]})
            </option>
          ))}
        </select>
      </label>
      <button type="submit" disabled={pendiente}>
        {pendiente ? "Abriendo…" : "Ver"}
      </button>
    </form>
  );
}
