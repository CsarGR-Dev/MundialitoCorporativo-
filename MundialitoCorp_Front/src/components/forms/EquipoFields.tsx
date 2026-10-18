import type { Equipo } from "@/lib/types/models";

/** Campos de alta y edición de un equipo. */
export function EquipoFields({ equipo }: { equipo?: Equipo }) {
  return (
    <div className="form-grid">
      <label className="span-2">
        Nombre
        <input name="nombre" defaultValue={equipo?.nombre} required minLength={3} maxLength={100} />
      </label>
      <label>
        Abreviatura
        <input name="abreviatura" defaultValue={equipo?.abreviatura} required minLength={2} maxLength={5} placeholder="Ej.: TEC" />
      </label>
      <label>
        Departamento (opcional)
        <input name="departamento" defaultValue={equipo?.departamento ?? ""} maxLength={100} />
      </label>
    </div>
  );
}
