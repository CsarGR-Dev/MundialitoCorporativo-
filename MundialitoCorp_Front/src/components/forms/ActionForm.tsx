"use client";

import Link from "next/link";
import { startTransition, useActionState, useEffect, useRef, type FormEvent, type ReactNode } from "react";

import { ErrorMessage } from "@/components/ErrorMessage";
import type { FormState } from "@/lib/form-data";
import { newIdempotencyKey } from "@/lib/idempotency-key";

interface ActionFormProps {
  action: (state: FormState, formData: FormData) => Promise<FormState>;
  submitLabel: string;
  pendingLabel: string;
  /** Título del aviso cuando la API rechaza los datos (400 o 409). */
  errorTitle?: string;
  /** Formularios de creación (POST): envían una Idempotency-Key por intento. */
  idempotent?: boolean;
  cancelHref?: string;
  className?: string;
  children: ReactNode;
}

/**
 * Formulario de alta o edición. Los campos llegan como children (renderizados en el servidor) y el envío va a
 * una Server Action, que es la única que llama a la API.
 *
 * - Se envía con onSubmit y no con <form action>: React reinicia los campos al terminar una acción y,
 *   ante un error de la API, el usuario perdería lo que escribió.
 * - Con idempotent, la clave se crea en el primer envío y se conserva mientras ese intento está en curso
 *   (un doble clic no crea dos registros). Tras un error se usa una nueva: la API guarda también las
 *   respuestas de error, y otro cuerpo con la misma clave respondería 409.
 */
export function ActionForm({ action, submitLabel, pendingLabel, errorTitle, idempotent, cancelHref, className, children }: ActionFormProps) {
  const [state, dispatch, pending] = useActionState(action, {});
  const idempotencyKey = useRef<string | null>(null);

  useEffect(() => {
    if (state.error) {
      idempotencyKey.current = null;
    }
  }, [state]);

  function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const formData = new FormData(event.currentTarget);
    if (idempotent) {
      idempotencyKey.current ??= newIdempotencyKey();
      formData.set("idempotencyKey", idempotencyKey.current);
    }
    startTransition(() => dispatch(formData));
  }

  return (
    <form onSubmit={submit} className={className ?? "form-card"}>
      {state.error && <ErrorMessage error={state.error} title={titleFor(state.error.status, errorTitle)} />}
      {children}
      <div className="form-actions">
        <button type="submit" className="primary" disabled={pending}>
          {pending ? pendingLabel : submitLabel}
        </button>
        {cancelHref && (
          <Link href={cancelHref} className="btn-secondary">
            Cancelar
          </Link>
        )}
      </div>
    </form>
  );
}

function titleFor(status: number, errorTitle: string | undefined): string | undefined {
  return status === 400 || status === 409 ? errorTitle : undefined;
}
