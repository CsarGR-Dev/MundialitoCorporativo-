import "server-only";

import { apiDelete, apiGet, apiPatch, apiPost, apiPut } from "@/lib/api/http-client";
import type { PagedResponse, PageQuery } from "@/lib/types/api";
import type { EstadoPartido, EstadoTorneo, FilaTablaPosiciones, Goleador, Partido, Torneo, TorneoEquipo, TorneoInput } from "@/lib/types/models";

export const TORNEOS_SORT_FIELDS = ["fechaInicio", "nombre", "estado"] as const;

export function getTorneos(query: PageQuery & { nombre?: string; estado?: string } = {}) {
  return apiGet<PagedResponse<Torneo>>("/api/torneos", { ...query });
}

export function getTorneo(id: string) {
  return apiGet<Torneo>(`/api/torneos/${encodeURIComponent(id)}`);
}

export function crearTorneo(torneo: TorneoInput, idempotencyKey: string) {
  return apiPost<Torneo>("/api/torneos", torneo, idempotencyKey);
}

export function actualizarTorneo(id: string, torneo: TorneoInput) {
  return apiPut<Torneo>(`/api/torneos/${encodeURIComponent(id)}`, torneo);
}

/** Planificado → EnCurso → Finalizado; la API rechaza con 409 cualquier otra transición. */
export function cambiarEstadoTorneo(id: string, estado: EstadoTorneo) {
  return apiPatch<Torneo>(`/api/torneos/${encodeURIComponent(id)}`, { estado });
}

/** Eliminación lógica; 409 si el torneo tiene partidos jugados. */
export function eliminarTorneo(id: string) {
  return apiDelete(`/api/torneos/${encodeURIComponent(id)}`);
}

export function getTorneoEquipos(torneoId: string, query: PageQuery = {}) {
  return apiGet<PagedResponse<TorneoEquipo>>(`/api/torneos/${encodeURIComponent(torneoId)}/equipos`, { ...query });
}

export function inscribirEquipo(torneoId: string, equipoId: string, idempotencyKey: string) {
  return apiPost<{ torneoId: string; equipoId: string }>(`/api/torneos/${encodeURIComponent(torneoId)}/equipos`, { equipoId }, idempotencyKey);
}

/** Solo si el equipo no tiene partidos en el torneo (409 en caso contrario). */
export function retirarEquipo(torneoId: string, equipoId: string) {
  return apiDelete(`/api/torneos/${encodeURIComponent(torneoId)}/equipos/${encodeURIComponent(equipoId)}`);
}

export function getTablaPosiciones(torneoId: string, query: PageQuery & { equipo?: string } = {}) {
  return apiGet<PagedResponse<FilaTablaPosiciones>>(`/api/torneos/${encodeURIComponent(torneoId)}/tabla-posiciones`, { ...query });
}

export function getGoleadores(torneoId: string, query: PageQuery & { equipoId?: string; jugador?: string } = {}) {
  return apiGet<PagedResponse<Goleador>>(`/api/torneos/${encodeURIComponent(torneoId)}/goleadores`, { ...query });
}

export function getTorneoPartidos(torneoId: string, query: PageQuery & { estado?: EstadoPartido } = {}) {
  return apiGet<PagedResponse<Partido>>(`/api/torneos/${encodeURIComponent(torneoId)}/partidos`, { ...query });
}
