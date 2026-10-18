import "server-only";

import { apiDelete, apiGet, apiPatch, apiPost, apiPut } from "@/lib/api/http-client";
import type { PagedResponse, PageQuery } from "@/lib/types/api";
import type { Jugador, Partido, PartidoDetalle, PartidoInput, RegistrarResultado } from "@/lib/types/models";

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

/** Programa un partido del calendario; los dos equipos deben estar inscritos en el torneo. */
export function programarPartido(partido: PartidoInput, idempotencyKey: string) {
  return apiPost<PartidoDetalle>("/api/partidos", partido, idempotencyKey);
}

/** Reprogramación completa (equipos, fecha y sede); el torneo no cambia y solo aplica a partidos programados. */
export function actualizarPartido(id: string, partido: Omit<PartidoInput, "torneoId">) {
  return apiPut<PartidoDetalle>(`/api/partidos/${encodeURIComponent(id)}`, partido);
}

export function cancelarPartido(id: string) {
  return apiPatch<PartidoDetalle>(`/api/partidos/${encodeURIComponent(id)}`, { estado: "Cancelado" });
}

/** Eliminación lógica; un partido jugado no puede eliminarse (409). */
export function eliminarPartido(id: string) {
  return apiDelete(`/api/partidos/${encodeURIComponent(id)}`);
}

/** Jugadores activos de un equipo, para elegir goleadores. */
export function getJugadoresActivos(equipoId: string) {
  return apiGet<PagedResponse<Jugador>>("/api/jugadores", { equipoId, activo: true, pageSize: 100, sortBy: "numeroCamiseta" });
}

/** Responde 201 con el detalle del partido ya jugado. */
export function registrarResultado(partidoId: string, resultado: RegistrarResultado, idempotencyKey: string) {
  return apiPost<PartidoDetalle>(`/api/partidos/${encodeURIComponent(partidoId)}/resultado`, resultado, idempotencyKey);
}
