import type { EstadoTorneo } from "@/lib/types/models";

const ETIQUETA: Record<EstadoTorneo, string> = {
  Planificado: "Planificado",
  EnCurso: "En curso",
  Finalizado: "Finalizado",
};

export function EstadoTorneoBadge({ estado }: { estado: EstadoTorneo }) {
  return <span className={`badge badge-torneo-${estado.toLowerCase()}`}>{ETIQUETA[estado]}</span>;
}
