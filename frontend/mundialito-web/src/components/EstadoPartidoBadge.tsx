import type { EstadoPartido } from "@/lib/types/models";

export function EstadoPartidoBadge({ estado }: { estado: EstadoPartido }) {
  return <span className={`badge badge-${estado.toLowerCase()}`}>{estado}</span>;
}
