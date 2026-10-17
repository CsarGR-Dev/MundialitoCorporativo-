"use client";

import { startTransition, useActionState, useEffect, useRef, useState, type FormEvent } from "react";

import { ErrorMessage } from "@/components/ErrorMessage";
import { newIdempotencyKey } from "@/lib/idempotency-key";
import type { ApiError } from "@/lib/types/api";

interface JugadorOpcion {
  id: string;
  nombre: string;
  numeroCamiseta: number;
}

interface EquipoFormulario {
  id: string;
  nombre: string;
  jugadores: JugadorOpcion[];
}

interface ResultadoFormProps {
  local: EquipoFormulario;
  visitante: EquipoFormulario;
  action: (state: { error?: ApiError }, formData: FormData) => Promise<{ error?: ApiError }>;
}

const MAX_GOLES = 20;

/**
 * Marcador y un goleador con minuto por cada gol. No conoce la API: envía el formulario a la Server Action.
 * La Idempotency-Key se crea en el primer envío y se conserva mientras ese intento está en curso, así un doble
 * envío no registra dos veces. Si la API responde con error, el siguiente intento usa una clave nueva
 * (la API guarda también las respuestas de error, y otro cuerpo con la misma clave respondería 409).
 */
export function ResultadoForm({ local, visitante, action }: ResultadoFormProps) {
  const [state, dispatch, pending] = useActionState(action, {});
  const [golesLocal, setGolesLocal] = useState(0);
  const [golesVisitante, setGolesVisitante] = useState(0);
  const idempotencyKey = useRef<string | null>(null);

  useEffect(() => {
    if (state.error) {
      idempotencyKey.current = null;
    }
  }, [state]);

  // Se envía con onSubmit y no con <form action>: React reinicia los campos del formulario al terminar una
  // acción, y ante un error de la API el usuario perdería los goleadores que ya eligió.
  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const formData = new FormData(event.currentTarget);
    idempotencyKey.current ??= newIdempotencyKey();
    formData.set("idempotencyKey", idempotencyKey.current);
    startTransition(() => dispatch(formData));
  }

  return (
    <form onSubmit={submit}>
      {state.error && <ErrorMessage error={state.error} title={titleFor(state.error)} />}

      <fieldset>
        <legend>Marcador</legend>
        <div className="gol-row">
          <MarcadorInput name="golesLocal" label={local.nombre} value={golesLocal} onChange={setGolesLocal} />
          <MarcadorInput name="golesVisitante" label={visitante.nombre} value={golesVisitante} onChange={setGolesVisitante} />
        </div>
      </fieldset>

      <GolesEquipo equipo={local} cantidad={golesLocal} />
      <GolesEquipo equipo={visitante} cantidad={golesVisitante} />

      <button type="submit" className="primary" disabled={pending}>
        {pending ? "Registrando…" : "Registrar resultado"}
      </button>
    </form>
  );
}

function titleFor(error: ApiError): string | undefined {
  if (error.status === 400) return "Revise los datos del resultado.";
  if (error.status === 409) return "No se pudo registrar el resultado.";
  return undefined;
}

function MarcadorInput({ name, label, value, onChange }: { name: string; label: string; value: number; onChange: (value: number) => void }) {
  return (
    <label>
      {label}
      <input
        type="number"
        name={name}
        min={0}
        max={MAX_GOLES}
        required
        value={value}
        onChange={(event) => onChange(Math.min(MAX_GOLES, Math.max(0, Number(event.target.value) || 0)))}
      />
    </label>
  );
}

/** Una fila por gol del equipo: goleador (solo jugadores del equipo) y minuto. */
function GolesEquipo({ equipo, cantidad }: { equipo: EquipoFormulario; cantidad: number }) {
  if (cantidad === 0) {
    return null;
  }

  return (
    <fieldset>
      <legend>Goles de {equipo.nombre}</legend>
      {equipo.jugadores.length === 0 && <p className="field-error">El equipo no tiene jugadores activos registrados.</p>}
      {Array.from({ length: cantidad }, (_, index) => (
        <div className="gol-row" key={index}>
          <input type="hidden" name="golEquipoId" value={equipo.id} />
          <label>
            Goleador {index + 1}
            <select name="golJugadorId" required defaultValue="">
              <option value="" disabled>
                Seleccione
              </option>
              {equipo.jugadores.map((jugador) => (
                <option key={jugador.id} value={jugador.id}>
                  #{jugador.numeroCamiseta} {jugador.nombre}
                </option>
              ))}
            </select>
          </label>
          <label>
            Minuto
            <input type="number" name="golMinuto" min={1} max={130} required />
          </label>
        </div>
      ))}
    </fieldset>
  );
}
