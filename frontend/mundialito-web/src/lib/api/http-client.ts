import "server-only";

import type { ApiError, ApiResult } from "@/lib/types/api";

/**
 * Única pieza que conoce la URL y el protocolo HTTP de la API. Corre solo en el servidor (Server Components
 * y Server Actions): el navegador nunca llama a la API directamente, así no hace falta CORS y la URL interna
 * (en Docker, http://api:8080) no se expone.
 */
const API_URL = (process.env.API_URL ?? "http://localhost:5000").replace(/\/$/, "");

const TIMEOUT_MS = 10_000;

export type QueryValue = string | number | boolean | null | undefined;

interface RequestOptions {
  query?: Record<string, QueryValue>;
  body?: unknown;
  /** Obligatoria en todo POST (ítem 6). */
  idempotencyKey?: string;
}

export function apiGet<T>(path: string, query?: Record<string, QueryValue>): Promise<ApiResult<T>> {
  return send<T>("GET", path, { query });
}

export function apiPost<T>(path: string, body: unknown, idempotencyKey: string): Promise<ApiResult<T>> {
  return send<T>("POST", path, { body, idempotencyKey });
}

async function send<T>(method: string, path: string, options: RequestOptions): Promise<ApiResult<T>> {
  const url = buildUrl(path, options.query);
  // Cada llamada lleva su propio CorrelationId: en los logs de la API se distingue lo que pidió el frontend.
  const correlationId = `web-${crypto.randomUUID()}`;

  const headers: Record<string, string> = { Accept: "application/json", "X-Correlation-Id": correlationId };
  if (options.body !== undefined) {
    headers["Content-Type"] = "application/json";
  }
  if (options.idempotencyKey) {
    headers["Idempotency-Key"] = options.idempotencyKey;
  }

  let response: Response;
  try {
    response = await fetch(url, {
      method,
      headers,
      body: options.body === undefined ? undefined : JSON.stringify(options.body),
      cache: "no-store",
      signal: AbortSignal.timeout(TIMEOUT_MS),
    });
  } catch {
    return {
      ok: false,
      error: {
        status: 0,
        title: "No se pudo conectar con la API",
        detail: "Verifique que la API esté en ejecución e intente de nuevo.",
        correlationId,
      },
    };
  }

  if (response.ok) {
    const data = response.status === 204 ? undefined : await response.json();
    return { ok: true, data: data as T };
  }

  return { ok: false, error: await readProblem(response, correlationId) };
}

function buildUrl(path: string, query?: Record<string, QueryValue>): string {
  const url = new URL(API_URL + path);
  for (const [key, value] of Object.entries(query ?? {})) {
    if (value !== undefined && value !== null && value !== "") {
      url.searchParams.set(key, String(value));
    }
  }
  return url.toString();
}

/** Interpreta ProblemDetails; si la respuesta no lo es (por ejemplo, un proxy), arma un error genérico. */
async function readProblem(response: Response, correlationId: string): Promise<ApiError> {
  const traceId = response.headers.get("X-Trace-Id") ?? undefined;
  try {
    const problem = await response.json();
    return {
      status: response.status,
      title: problem.title ?? response.statusText,
      detail: problem.detail,
      errorCode: problem.errorCode,
      traceId: problem.traceId ?? traceId,
      correlationId: problem.correlationId ?? correlationId,
      errors: problem.errors,
    };
  } catch {
    return { status: response.status, title: response.statusText || "Error de la API", traceId, correlationId };
  }
}
