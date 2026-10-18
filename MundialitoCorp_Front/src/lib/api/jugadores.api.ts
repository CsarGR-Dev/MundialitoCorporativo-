import "server-only";

import { apiDelete, apiGet, apiPut } from "@/lib/api/http-client";
import type { Jugador, JugadorUpdate } from "@/lib/types/models";

export function getJugador(id: string) {
  return apiGet<Jugador>(`/api/jugadores/${encodeURIComponent(id)}`);
}

/** Reemplazo completo; el equipo del jugador no cambia. */
export function actualizarJugador(id: string, jugador: JugadorUpdate) {
  return apiPut<Jugador>(`/api/jugadores/${encodeURIComponent(id)}`, jugador);
}

export function eliminarJugador(id: string) {
  return apiDelete(`/api/jugadores/${encodeURIComponent(id)}`);
}
