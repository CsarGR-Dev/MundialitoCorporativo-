"use client";

import { useActionState, type FormEvent } from "react";

import { ErrorMessage } from "@/components/ErrorMessage";
import type { FormState } from "@/lib/form-data";

interface ConfirmActionButtonProps {
  action: (state: FormState, formData: FormData) => Promise<FormState>;
  label: string;
  pendingLabel: string;
  /** Pregunta que se confirma antes de enviar. */
  confirmMessage: string;
  /** Título del aviso si la API rechaza la operación (por ejemplo, 409 por partidos asociados). */
  errorTitle?: string;
  variant?: "danger" | "default";
}

/**
 * Botón para operaciones sin datos que capturar: eliminar, retirar, cancelar o cambiar de estado.
 * Pide confirmación, ejecuta la Server Action y, si la API la rechaza, muestra el motivo junto al botón.
 */
export function ConfirmActionButton({ action, label, pendingLabel, confirmMessage, errorTitle, variant = "default" }: ConfirmActionButtonProps) {
  const [state, dispatch, pending] = useActionState(action, {});

  function confirmSubmit(event: FormEvent<HTMLFormElement>) {
    if (!window.confirm(confirmMessage)) {
      event.preventDefault();
    }
  }

  return (
    <form action={dispatch} onSubmit={confirmSubmit} className="inline-action">
      <button type="submit" className={variant === "danger" ? "danger" : undefined} disabled={pending}>
        {pending ? pendingLabel : label}
      </button>
      {state.error && (
        <ErrorMessage error={state.error} title={state.error.status === 400 || state.error.status === 409 ? errorTitle : undefined} />
      )}
    </form>
  );
}
