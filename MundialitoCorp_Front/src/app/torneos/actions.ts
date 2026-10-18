"use server";

import { redirect } from "next/navigation";

import { actualizarTorneo, cambiarEstadoTorneo, crearTorneo, eliminarTorneo, inscribirEquipo, retirarEquipo } from "@/lib/api/torneos.api";
import { optionalText, text, type FormState } from "@/lib/form-data";
import type { EstadoTorneo, TorneoInput } from "@/lib/types/models";

/**
 * Server Actions de torneos: arman el cuerpo, llaman a la API desde el servidor y, si todo sale bien,
 * redirigen a la página que muestra el cambio. Un error de la API vuelve al formulario como valor.
 */

function leerTorneo(formData: FormData): TorneoInput {
  return {
    nombre: text(formData, "nombre"),
    descripcion: optionalText(formData, "descripcion"),
    fechaInicio: text(formData, "fechaInicio"),
    fechaFin: optionalText(formData, "fechaFin"),
  };
}

export async function crearTorneoAction(_state: FormState, formData: FormData): Promise<FormState> {
  const result = await crearTorneo(leerTorneo(formData), text(formData, "idempotencyKey"));
  if (!result.ok) {
    return { error: result.error };
  }
  redirect(`/torneos/${encodeURIComponent(result.data.id)}`);
}

export async function actualizarTorneoAction(id: string, _state: FormState, formData: FormData): Promise<FormState> {
  const result = await actualizarTorneo(id, leerTorneo(formData));
  if (!result.ok) {
    return { error: result.error };
  }
  redirect(`/torneos/${encodeURIComponent(id)}`);
}

export async function cambiarEstadoTorneoAction(id: string, estado: EstadoTorneo): Promise<FormState> {
  const result = await cambiarEstadoTorneo(id, estado);
  if (!result.ok) {
    return { error: result.error };
  }
  redirect(`/torneos/${encodeURIComponent(id)}`);
}

export async function eliminarTorneoAction(id: string): Promise<FormState> {
  const result = await eliminarTorneo(id);
  if (!result.ok) {
    return { error: result.error };
  }
  redirect("/torneos");
}

export async function inscribirEquipoAction(torneoId: string, _state: FormState, formData: FormData): Promise<FormState> {
  const result = await inscribirEquipo(torneoId, text(formData, "equipoId"), text(formData, "idempotencyKey"));
  if (!result.ok) {
    return { error: result.error };
  }
  redirect(`/torneos/${encodeURIComponent(torneoId)}`);
}

export async function retirarEquipoAction(torneoId: string, equipoId: string): Promise<FormState> {
  const result = await retirarEquipo(torneoId, equipoId);
  if (!result.ok) {
    return { error: result.error };
  }
  redirect(`/torneos/${encodeURIComponent(torneoId)}`);
}
