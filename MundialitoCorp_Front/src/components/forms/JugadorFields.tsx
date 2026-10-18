import type { Jugador, PosicionJuego } from "@/lib/types/models";

/**
 * Campos de un integrante. Al editar se agrega "Activo": un jugador inactivo sigue en el historial
 * (sus goles cuentan) pero no aparece para elegir goleadores.
 */
export function JugadorFields({ jugador, posiciones }: { jugador?: Jugador; posiciones: PosicionJuego[] }) {
  return (
    <div className="form-grid">
      <label>
        Nombres
        <input name="nombres" defaultValue={jugador?.nombres} required minLength={2} maxLength={60} />
      </label>
      <label>
        Apellidos
        <input name="apellidos" defaultValue={jugador?.apellidos} required minLength={2} maxLength={60} />
      </label>
      <label>
        Número de camiseta
        <input type="number" name="numeroCamiseta" defaultValue={jugador?.numeroCamiseta} required min={1} max={99} />
      </label>
      <label>
        Posición
        <select name="posicionJuegoId" defaultValue={jugador?.posicionJuegoId ?? ""} required>
          <option value="" disabled>
            Seleccione
          </option>
          {posiciones
            .filter((posicion) => posicion.activo || posicion.id === jugador?.posicionJuegoId)
            .map((posicion) => (
              <option key={posicion.id} value={posicion.id}>
                {posicion.nombre}
              </option>
            ))}
        </select>
      </label>
      {jugador && (
        <label className="checkbox-label span-2">
          <input type="checkbox" name="activo" defaultChecked={jugador.activo} />
          Activo (puede ser elegido como goleador)
        </label>
      )}
    </div>
  );
}
