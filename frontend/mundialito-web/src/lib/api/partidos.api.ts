import "server-only";

import { apiGet, apiPost } from "@/lib/api/http-client";
import type { PagedResponse, PageQuery } from "@/lib/types/api";
import type { Jugador, Partido, PartidoDetalle, RegistrarResultado } from "@/lib/types/models";

export interface PartidosFilter extends PageQuery {
  torneoId?: string;
  fechaDesde?: string;
  fechaHasta?: string;
  equipoId?: string;
  estado?: string;
}

export function getPartidos(filter: PartidosFilter = {}) {
  return apiGet<PagedResponse<Partido>>("/api/partidos", { ...filter });
}

export function getPartido(id: string) {
  return apiGet<PartidoDetalle>(`/api/partidos/${encodeURIComponent(id)}`);
}

/** Jugadores activos de un equipo, para elegir goleadores. */
export function getJugadoresActivos(equipoId: string) {
  return apiGet<PagedResponse<Jugador>>("/api/jugadores", { equipoId, activo: true, pageSize: 100, sortBy: "numeroCamiseta" });
}

/** Responde 201 con el detalle del partido ya jugado. */
export function registrarResultado(partidoId: string, resultado: RegistrarResultado, idempotencyKey: string) {
  return apiPost<PartidoDetalle>(`/api/partidos/${encodeURIComponent(partidoId)}/resultado`, resultado, idempotencyKey);
}
