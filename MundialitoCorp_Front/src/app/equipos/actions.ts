"use server";

import { redirect } from "next/navigation";

import { actualizarEquipo, crearEquipo, eliminarEquipo, registrarIntegrante } from "@/lib/api/equipos.api";
import { actualizarJugador, eliminarJugador } from "@/lib/api/jugadores.api";
import { checkbox, integer, optionalText, text, type FormState } from "@/lib/form-data";
import type { EquipoInput, JugadorInput } from "@/lib/types/models";

/**
 * Server Actions de equipos y de sus integrantes. Los jugadores se registran por equipo (ítem 8),
 * así que todas las acciones de jugadores vuelven a la página del equipo.
 */

function leerEquipo(formData: FormData): EquipoInput {
  return {
    nombre: text(formData, "nombre"),
    abreviatura: text(formData, "abreviatura"),
    departamento: optionalText(formData, "departamento"),
  };
}

function leerJugador(formData: FormData): JugadorInput {
  return {
    posicionJuegoId: integer(formData, "posicionJuegoId"),
    nombres: text(formData, "nombres"),
    apellidos: text(formData, "apellidos"),
    numeroCamiseta: integer(formData, "numeroCamiseta"),
  };
}

function equipoPath(equipoId: string): string {
  return `/equipos/${encodeURIComponent(equipoId)}`;
}

export async function crearEquipoAction(_state: FormState, formData: FormData): Promise<FormState> {
  const result = await crearEquipo(leerEquipo(formData), text(formData, "idempotencyKey"));
  if (!result.ok) {
    return { error: result.error };
  }
  redirect(equipoPath(result.data.id));
}

export async function actualizarEquipoAction(id: string, _state: FormState, formData: FormData): Promise<FormState> {
  const result = await actualizarEquipo(id, leerEquipo(formData));
  if (!result.ok) {
    return { error: result.error };
  }
  redirect(equipoPath(id));
}

export async function eliminarEquipoAction(id: string): Promise<FormState> {
  const result = await eliminarEquipo(id);
  if (!result.ok) {
    return { error: result.error };
  }
  redirect("/equipos");
}

export async function registrarIntegranteAction(equipoId: string, _state: FormState, formData: FormData): Promise<FormState> {
  const result = await registrarIntegrante(equipoId, leerJugador(formData), text(formData, "idempotencyKey"));
  if (!result.ok) {
    return { error: result.error };
  }
  redirect(equipoPath(equipoId));
}

export async function actualizarIntegranteAction(equipoId: string, jugadorId: string, _state: FormState, formData: FormData): Promise<FormState> {
  const result = await actualizarJugador(jugadorId, { ...leerJugador(formData), activo: checkbox(formData, "activo") });
  if (!result.ok) {
    return { error: result.error };
  }
  redirect(equipoPath(equipoId));
}

export async function eliminarIntegranteAction(equipoId: string, jugadorId: string): Promise<FormState> {
  const result = await eliminarJugador(jugadorId);
  if (!result.ok) {
    return { error: result.error };
  }
  redirect(equipoPath(equipoId));
}
