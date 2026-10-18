import "server-only";

import { apiDelete, apiGet, apiPost, apiPut } from "@/lib/api/http-client";
import type { PagedResponse, PageQuery } from "@/lib/types/api";
import type { Equipo, EquipoInput, Jugador, JugadorInput, PosicionJuego } from "@/lib/types/models";

export const EQUIPOS_SORT_FIELDS = ["nombre", "abreviatura", "departamento", "fechaCreacion"] as const;

export function getEquipos(query: PageQuery & { nombre?: string; departamento?: string } = {}) {
  return apiGet<PagedResponse<Equipo>>("/api/equipos", { ...query });
}

export function getEquipo(id: string) {
  return apiGet<Equipo>(`/api/equipos/${encodeURIComponent(id)}`);
}

export function crearEquipo(equipo: EquipoInput, idempotencyKey: string) {
  return apiPost<Equipo>("/api/equipos", equipo, idempotencyKey);
}

export function actualizarEquipo(id: string, equipo: EquipoInput) {
  return apiPut<Equipo>(`/api/equipos/${encodeURIComponent(id)}`, equipo);
}

/** Eliminación lógica; 409 si el equipo tiene partidos. */
export function eliminarEquipo(id: string) {
  return apiDelete(`/api/equipos/${encodeURIComponent(id)}`);
}

/** Integrantes del equipo; responde 404 si el equipo no existe. */
export function getIntegrantes(equipoId: string, query: PageQuery & { posicionJuegoId?: string; nombre?: string } = {}) {
  return apiGet<PagedResponse<Jugador>>(`/api/equipos/${encodeURIComponent(equipoId)}/jugadores`, { ...query });
}

export function registrarIntegrante(equipoId: string, jugador: JugadorInput, idempotencyKey: string) {
  return apiPost<Jugador>(`/api/equipos/${encodeURIComponent(equipoId)}/jugadores`, jugador, idempotencyKey);
}

export function getPosicionesJuego() {
  return apiGet<PagedResponse<PosicionJuego>>("/api/posiciones-juego", { pageSize: 100, sortBy: "orden" });
}
