import { toDateTimeLocal } from "@/lib/format";
import type { Partido, TorneoEquipo } from "@/lib/types/models";

/**
 * Campos para programar o reprogramar un partido. Solo se ofrecen los equipos inscritos en el torneo;
 * que local y visitante sean distintos y que la fecha esté dentro del torneo lo valida la API.
 */
export function PartidoFields({ partido, equipos }: { partido?: Partido; equipos: TorneoEquipo[] }) {
  return (
    <div className="form-grid">
      <EquipoSelect name="equipoLocalId" label="Equipo local" equipos={equipos} defaultValue={partido?.equipoLocalId} />
      <EquipoSelect name="equipoVisitanteId" label="Equipo visitante" equipos={equipos} defaultValue={partido?.equipoVisitanteId} />
      <label>
        Fecha y hora (hora de El Salvador)
        <input
          type="datetime-local"
          name="fechaProgramada"
          defaultValue={partido ? toDateTimeLocal(partido.fechaProgramada) : undefined}
          required
        />
      </label>
      <label>
        Sede (opcional)
        <input name="sede" defaultValue={partido?.sede ?? ""} maxLength={100} />
      </label>
    </div>
  );
}

function EquipoSelect({ name, label, equipos, defaultValue }: { name: string; label: string; equipos: TorneoEquipo[]; defaultValue?: string }) {
  return (
    <label>
      {label}
      <select name={name} defaultValue={defaultValue ?? ""} required>
        <option value="" disabled>
          Seleccione
        </option>
        {equipos.map((equipo) => (
          <option key={equipo.equipoId} value={equipo.equipoId}>
            {equipo.equipo}
          </option>
        ))}
      </select>
    </label>
  );
}
