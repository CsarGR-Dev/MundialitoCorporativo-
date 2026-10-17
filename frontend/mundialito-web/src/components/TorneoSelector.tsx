import type { Torneo } from "@/lib/types/models";

const ETIQUETA_ESTADO: Record<Torneo["estado"], string> = {
  Planificado: "planificado",
  EnCurso: "en curso",
  Finalizado: "finalizado",
};

/**
 * Elige el torneo de la vista. Es un formulario GET: el torneo queda en la URL (?torneo=) sin JavaScript.
 */
export function TorneoSelector({ torneos, actual, action }: { torneos: Torneo[]; actual: Torneo; action: string }) {
  return (
    <form method="get" action={action} className="torneo-selector">
      <label>
        Torneo{" "}
        <select name="torneo" defaultValue={actual.id}>
          {torneos.map((torneo) => (
            <option key={torneo.id} value={torneo.id}>
              {torneo.nombre} ({ETIQUETA_ESTADO[torneo.estado]})
            </option>
          ))}
        </select>
      </label>
      <button type="submit">Ver</button>
    </form>
  );
}
