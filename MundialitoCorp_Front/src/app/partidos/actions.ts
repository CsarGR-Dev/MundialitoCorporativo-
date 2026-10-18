"use server";

import { redirect } from "next/navigation";

import { actualizarPartido, cancelarPartido, eliminarPartido, programarPartido } from "@/lib/api/partidos.api";
import { fromDateTimeLocal } from "@/lib/format";
import { optionalText, text, type FormState } from "@/lib/form-data";
import type { PartidoInput } from "@/lib/types/models";

/**
 * Server Actions del calendario de partidos. El registro del resultado está en partidos/[id]/resultado.
 * La fecha se captura en hora de El Salvador y se envía en UTC.
 */

function leerPartido(formData: FormData): Omit<PartidoInput, "torneoId"> {
  return {
    equipoLocalId: text(formData, "equipoLocalId"),
    equipoVisitanteId: text(formData, "equipoVisitanteId"),
    fechaProgramada: fromDateTimeLocal(text(formData, "fechaProgramada")),
    sede: optionalText(formData, "sede"),
  };
}

function partidoPath(id: string): string {
  return `/partidos/${encodeURIComponent(id)}`;
}

export async function programarPartidoAction(torneoId: string, _state: FormState, formData: FormData): Promise<FormState> {
  const result = await programarPartido({ torneoId, ...leerPartido(formData) }, text(formData, "idempotencyKey"));
  if (!result.ok) {
    return { error: result.error };
  }
  redirect(partidoPath(result.data.id));
}

export async function actualizarPartidoAction(id: string, _state: FormState, formData: FormData): Promise<FormState> {
  const result = await actualizarPartido(id, leerPartido(formData));
  if (!result.ok) {
    return { error: result.error };
  }
  redirect(partidoPath(id));
}

export async function cancelarPartidoAction(id: string): Promise<FormState> {
  const result = await cancelarPartido(id);
  if (!result.ok) {
    return { error: result.error };
  }
  redirect(partidoPath(id));
}

export async function eliminarPartidoAction(id: string, torneoId: string): Promise<FormState> {
  const result = await eliminarPartido(id);
  if (!result.ok) {
    return { error: result.error };
  }
  redirect(`/partidos?torneo=${encodeURIComponent(torneoId)}`);
}
