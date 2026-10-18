/**
 * La API entrega las fechas en UTC; se muestran en la hora de El Salvador.
 */
const TIME_ZONE = "America/El_Salvador";

const fechaHora = new Intl.DateTimeFormat("es-SV", {
  day: "numeric",
  month: "short",
  year: "numeric",
  hour: "2-digit",
  minute: "2-digit",
  hourCycle: "h23",
  timeZone: TIME_ZONE,
});

const soloFecha = new Intl.DateTimeFormat("es-SV", { dateStyle: "medium", timeZone: "UTC" });


export function formatFechaHora(isoUtc: string): string {
  return fechaHora.format(new Date(isoUtc));
}

/** Fechas sin hora (DateOnly de la API, "2026-10-01"). */
export function formatFecha(fecha: string | null | undefined): string {
  return fecha ? soloFecha.format(new Date(`${fecha}T00:00:00Z`)) : "—";
}

export function formatMarcador(golesLocal: number | null, golesVisitante: number | null): string {
  return golesLocal === null || golesVisitante === null ? "vs" : `${golesLocal} - ${golesVisitante}`;
}

export function formatDiferencia(valor: number): string {
  return valor > 0 ? `+${valor}` : String(valor);
}
