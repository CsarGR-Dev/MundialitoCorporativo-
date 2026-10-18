import type { Torneo } from "@/lib/types/models";

/** Campos de alta y edición de un torneo. Los límites replican los de la API para avisar antes de enviar. */
export function TorneoFields({ torneo }: { torneo?: Torneo }) {
  return (
    <div className="form-grid">
      <label className="span-2">
        Nombre
        <input name="nombre" defaultValue={torneo?.nombre} required minLength={3} maxLength={100} />
      </label>
      <label className="span-2">
        Descripción (opcional)
        <textarea name="descripcion" defaultValue={torneo?.descripcion ?? ""} maxLength={250} rows={3} />
      </label>
      <label>
        Fecha de inicio
        <input type="date" name="fechaInicio" defaultValue={torneo?.fechaInicio} required />
      </label>
      <label>
        Fecha de fin (opcional)
        <input type="date" name="fechaFin" defaultValue={torneo?.fechaFin ?? ""} />
      </label>
    </div>
  );
}
