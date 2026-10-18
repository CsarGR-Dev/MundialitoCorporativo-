"use client";

import { AlertIcon } from "@/components/ErrorMessage";

/**
 * Último recurso para errores inesperados al mostrar una página. Los errores de la API (400, 404, 409, API caída)
 * no llegan aquí: cada página los recibe como valor y los muestra con su mensaje y traceId.
 */
export default function ErrorPage({ error, retry }: { error: unknown; retry: () => void }) {
  const digest = typeof error === "object" && error !== null && "digest" in error ? String(error.digest) : undefined;

  return (
    <div className="alert alert-error" role="alert">
      <AlertIcon />
      <div className="alert-body">
        <strong>Ocurrió un error inesperado al mostrar la página.</strong>
        <p>Intente de nuevo. Si el problema continúa, comparta esta referencia con soporte.</p>
        {digest && <p className="small">Referencia: <code>{digest}</code></p>}
        <button type="button" onClick={() => retry()}>
          Reintentar
        </button>
      </div>
    </div>
  );
}
