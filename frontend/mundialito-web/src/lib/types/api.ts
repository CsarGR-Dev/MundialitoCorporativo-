/**
 * Contratos de la API: respuesta paginada, ProblemDetails y resultado de cada llamada.
 */

/** Formato de todos los GET de colección (requerimiento ítem 10). */
export interface PagedResponse<T> {
  data: T[];
  pageNumber: number;
  pageSize: number;
  totalRecords: number;
  totalPages: number;
}

export type SortDirection = "asc" | "desc";

/** Parámetros comunes de paginación y orden; se envían con los mismos nombres que la API. */
export interface PageQuery {
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortDirection?: SortDirection;
}

/** Error de la API (ProblemDetails, RFC 9457) o de conexión. */
export interface ApiError {
  /** Código HTTP; 0 si no se pudo conectar con la API. */
  status: number;
  title: string;
  detail?: string;
  errorCode?: string;
  traceId?: string;
  correlationId?: string;
  /** Errores de validación por campo (400). */
  errors?: Record<string, string[]>;
}

/**
 * Igual que el Result Pattern del backend: los errores esperados (400, 404, 409) viajan como valor
 * y la página decide cómo mostrarlos; no se usan excepciones como control de flujo.
 */
export type ApiResult<T> = { ok: true; data: T } | { ok: false; error: ApiError };
