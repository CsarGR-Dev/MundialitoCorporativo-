import "server-only";

import { getTorneos } from "@/lib/api/torneos.api";
import type { ApiResult } from "@/lib/types/api";
import type { Torneo } from "@/lib/types/models";

export interface TorneoSeleccion {
  torneos: Torneo[];
  actual: Torneo | undefined;
}

/**
 * Partidos, tabla y goleadores se muestran por torneo. Se usa el de la URL (?torneo=) y, si no viene,
 * el que está en curso o, en su defecto, el más reciente.
 */
export async function resolveTorneo(torneoId: string | undefined): Promise<ApiResult<TorneoSeleccion>> {
  const result = await getTorneos({ pageSize: 100, sortBy: "fechaInicio", sortDirection: "desc" });
  if (!result.ok) {
    return result;
  }

  const torneos = result.data.data;
  const actual =
    torneos.find((torneo) => torneo.id === torneoId) ??
    torneos.find((torneo) => torneo.estado === "EnCurso") ??
    torneos[0];

  return { ok: true, data: { torneos, actual } };
}
