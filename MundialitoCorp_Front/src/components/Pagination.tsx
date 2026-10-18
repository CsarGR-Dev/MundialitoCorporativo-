import Link from "next/link";

import { hrefWith, type SearchParams } from "@/lib/search-params";
import type { PagedResponse } from "@/lib/types/api";

/**
 * Navegación entre páginas con enlaces: la página queda en la URL (pageNumber) junto con los filtros.
 */
export function Pagination({ pathname, searchParams, page }: { pathname: string; searchParams: SearchParams; page: PagedResponse<unknown> }) {
  const { pageNumber, totalPages, totalRecords } = page;
  const hasPrevious = pageNumber > 1;
  const hasNext = pageNumber < totalPages;

  return (
    <nav className="pagination" aria-label="Paginación">
      {hasPrevious ? (
        <Link href={hrefWith(pathname, searchParams, { pageNumber: pageNumber - 1 })} scroll={false}>
          ← Anterior
        </Link>
      ) : (
        <span className="disabled">← Anterior</span>
      )}
      <span className="page-info">
        Página {totalPages === 0 ? 0 : pageNumber} de {totalPages} · {totalRecords} {totalRecords === 1 ? "registro" : "registros"}
      </span>
      {hasNext ? (
        <Link href={hrefWith(pathname, searchParams, { pageNumber: pageNumber + 1 })} scroll={false}>
          Siguiente →
        </Link>
      ) : (
        <span className="disabled">Siguiente →</span>
      )}
    </nav>
  );
}
