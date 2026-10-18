/**
 * La API entrega las fechas en UTC; se muestran en la hora de El Salvador.
 */
const TIME_ZONE = "America/El_Salvador";
/** El Salvador no usa horario de verano: siempre UTC-6. */
const UTC_OFFSET = "-06:00";

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

/** Partes numéricas de la fecha y hora local, para el valor de un <input type="datetime-local">. */
const partesLocales = new Intl.DateTimeFormat("en-CA", {
  year: "numeric",
  month: "2-digit",
  day: "2-digit",
  hour: "2-digit",
  minute: "2-digit",
  hourCycle: "h23",
  timeZone: TIME_ZONE,
});

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

/** Fecha UTC de la API → valor de <input type="datetime-local"> en hora de El Salvador ("2026-10-20T15:00"). */
export function toDateTimeLocal(isoUtc: string): string {
  const partes = Object.fromEntries(partesLocales.formatToParts(new Date(isoUtc)).map((parte) => [parte.type, parte.value]));
  return `${partes.year}-${partes.month}-${partes.day}T${partes.hour}:${partes.minute}`;
}

/**
 * Valor de <input type="datetime-local"> (hora de El Salvador) → ISO 8601 en UTC para la API.
 * Un valor vacío o inválido se envía vacío y la API responde 400.
 */
export function fromDateTimeLocal(value: string): string {
  const fecha = new Date(`${value}:00${UTC_OFFSET}`);
  return Number.isNaN(fecha.getTime()) ? "" : fecha.toISOString();
}
