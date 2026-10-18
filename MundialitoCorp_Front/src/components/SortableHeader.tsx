import Link from "next/link";

import { hrefWith, param, type SearchParams } from "@/lib/search-params";

/**
 * Encabezado de columna que ordena por ese campo (sortBy) y alterna la dirección (sortDirection).
 */
export function SortableHeader({
  label,
  field,
  pathname,
  searchParams,
  defaultField,
  className,
}: {
  label: string;
  field: string;
  pathname: string;
  searchParams: SearchParams;
  /** Campo por el que ordena la API cuando no se indica sortBy. */
  defaultField?: string;
  /** "num" para las columnas numéricas, alineadas a la derecha igual que sus celdas. */
  className?: string;
}) {
  const currentField = param(searchParams, "sortBy") ?? defaultField;
  const currentDirection = param(searchParams, "sortDirection") === "desc" ? "desc" : "asc";
  const isActive = currentField === field;
  const nextDirection = isActive && currentDirection === "asc" ? "desc" : "asc";
  const indicator = isActive ? (currentDirection === "asc" ? "▲" : "▼") : null;

  return (
    <th className={className} aria-sort={isActive ? (currentDirection === "asc" ? "ascending" : "descending") : undefined}>
      <Link href={hrefWith(pathname, searchParams, { sortBy: field, sortDirection: nextDirection })} scroll={false}>
        {label}
        {indicator && <span className="sort-indicator">{indicator}</span>}
      </Link>
    </th>
  );
}
