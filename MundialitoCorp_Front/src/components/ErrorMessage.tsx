import type { ApiError } from "@/lib/types/api";

const MENSAJES_POR_CODIGO: Record<number, string> = {
  0: "La API no está disponible.",
  400: "La solicitud no es válida.",
  404: "No se encontró lo que busca.",
  409: "La operación entra en conflicto con el estado actual.",
};

/**
 * Muestra un error de la API con un mensaje útil. Para 5xx o API caída incluye el traceId,
 * que permite encontrar el request en los logs de la API.
 */
export function ErrorMessage({ error, title }: { error: ApiError; title?: string }) {
  const resumen = title ?? MENSAJES_POR_CODIGO[error.status] ?? "Ocurrió un error inesperado.";
  const fieldErrors = Object.entries(error.errors ?? {});

  return (
    <div className="alert alert-error" role="alert">
      <strong>{resumen}</strong>
      {error.detail && <p>{error.detail}</p>}
      {fieldErrors.length > 0 && (
        <ul>
          {fieldErrors.flatMap(([field, messages]) => messages.map((message) => <li key={`${field}-${message}`}>{message}</li>))}
        </ul>
      )}
      {(error.traceId || error.errorCode) && (
        <p className="muted small">
          {error.errorCode && <>Código: {error.errorCode}. </>}
          {error.traceId && <>Referencia para soporte (traceId): <code>{error.traceId}</code></>}
        </p>
      )}
    </div>
  );
}
