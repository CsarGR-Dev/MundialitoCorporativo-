import "server-only";

import { apiGet } from "@/lib/api/http-client";
import type { PagedResponse, PageQuery } from "@/lib/types/api";
import type { EstadoPartido, FilaTablaPosiciones, Goleador, Partido, Torneo, TorneoEquipo } from "@/lib/types/models";

export function getTorneos(query: PageQuery & { nombre?: string; estado?: string } = {}) {
  return apiGet<PagedResponse<Torneo>>("/api/torneos", { ...query });
}

export function getTorneoEquipos(torneoId: string, query: PageQuery = {}) {
  return apiGet<PagedResponse<TorneoEquipo>>(`/api/torneos/${encodeURIComponent(torneoId)}/equipos`, { ...query });
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
