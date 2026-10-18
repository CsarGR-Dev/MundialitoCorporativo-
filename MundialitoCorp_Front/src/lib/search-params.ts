import type { PageQuery, SortDirection } from "@/lib/types/api";

/**
 * Los filtros, la página y el orden viven en la URL: se pueden compartir, recargar y usar con "atrás".
 * Los nombres de los parámetros son los mismos que acepta la API (pageNumber, pageSize, sortBy, sortDirection).
 */
export type SearchParams = Record<string, string | string[] | undefined>;

export const PAGE_SIZES = [5, 10, 20, 50] as const;
const DEFAULT_PAGE_SIZE = 10;

/** Primer valor del parámetro, sin espacios; vacío se trata como ausente. */
export function param(searchParams: SearchParams, key: string): string | undefined {
  const raw = searchParams[key];
  const value = (Array.isArray(raw) ? raw[0] : raw)?.trim();
  return value ? value : undefined;
}

/**
 * Paginación y orden desde la URL. Solo se envía a la API un sortBy de la lista permitida y un tamaño de
 * página conocido; así una URL editada a mano no termina en un error 400.
 */
export function pageQuery(searchParams: SearchParams, sortFields: readonly string[] = []): PageQuery {
  const pageNumber = Number.parseInt(param(searchParams, "pageNumber") ?? "", 10);
  const pageSize = Number.parseInt(param(searchParams, "pageSize") ?? "", 10);
  const sortBy = param(searchParams, "sortBy");
  const sortDirection = param(searchParams, "sortDirection");

  return {
    pageNumber: pageNumber > 0 ? pageNumber : 1,
    pageSize: (PAGE_SIZES as readonly number[]).includes(pageSize) ? pageSize : DEFAULT_PAGE_SIZE,
    sortBy: sortBy && sortFields.includes(sortBy) ? sortBy : undefined,
    sortDirection: sortDirection === "desc" || sortDirection === "asc" ? (sortDirection as SortDirection) : undefined,
  };
}

/**
 * Clave con los parámetros actuales, para la frontera de <Suspense> del bloque de resultados:
 * al cambiar filtros, orden o página se monta una nueva y se ve el esqueleto solo en la tabla.
 */
export function searchKey(searchParams: SearchParams): string {
  return Object.entries(searchParams)
    .map(([key, raw]) => `${key}=${Array.isArray(raw) ? raw.join(",") : (raw ?? "")}`)
    .sort()
    .join("&");
}

/**
 * URL de la misma página con algunos parámetros cambiados. Un valor null o vacío quita el parámetro.
 * Cambiar cualquier parámetro que no sea la página vuelve a la página 1.
 */
export function hrefWith(pathname: string, searchParams: SearchParams, changes: Record<string, string | number | null | undefined>): string {
  const query = new URLSearchParams();
  for (const [key, raw] of Object.entries(searchParams)) {
    const value = Array.isArray(raw) ? raw[0] : raw;
    if (value) {
      query.set(key, value);
    }
  }

  if (!("pageNumber" in changes)) {
    query.delete("pageNumber");
  }

  for (const [key, value] of Object.entries(changes)) {
    if (value === null || value === undefined || value === "") {
      query.delete(key);
    } else {
      query.set(key, String(value));
    }
  }

  const text = query.toString();
  return text ? `${pathname}?${text}` : pathname;
}
