"use server";

import { redirect } from "next/navigation";

import { registrarResultado } from "@/lib/api/partidos.api";
import type { ApiError } from "@/lib/types/api";

export interface ResultadoFormState {
  error?: ApiError;
}

/**
 * Server Action del formulario: arma el cuerpo y llama a la API desde el servidor con la Idempotency-Key
 * que generó el formulario. Las reglas (marcador, goleadores, minutos) las valida la API; aquí solo se
 * convierten los campos. Si todo sale bien, redirige al detalle del partido.
 */
export async function registrarResultadoAction(partidoId: string, _previous: ResultadoFormState, formData: FormData): Promise<ResultadoFormState> {
  const idempotencyKey = String(formData.get("idempotencyKey") ?? "");
  const jugadores = formData.getAll("golJugadorId").map(String);
  const equipos = formData.getAll("golEquipoId").map(String);
  const minutos = formData.getAll("golMinuto").map(toInteger);

  const result = await registrarResultado(
    partidoId,
    {
      golesLocal: toInteger(formData.get("golesLocal")),
      golesVisitante: toInteger(formData.get("golesVisitante")),
      goles: jugadores.map((jugadorId, index) => ({ jugadorId, equipoId: equipos[index], minuto: minutos[index] })),
    },
    idempotencyKey,
  );

  if (!result.ok) {
    return { error: result.error };
  }

  redirect(`/partidos/${encodeURIComponent(partidoId)}`);
}

/** Un valor vacío o no numérico se envía como null y la API responde 400 con el campo inválido. */
function toInteger(value: FormDataEntryValue | null): number {
  const text = typeof value === "string" ? value.trim() : "";
  return text === "" ? Number.NaN : Number(text);
}
