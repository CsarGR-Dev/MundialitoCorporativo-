"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useTransition, type FormEvent, type ReactNode } from "react";

/**
 * Barra de filtros de una vista. Sigue siendo un formulario GET y los filtros viven en la URL, con los
 * mismos nombres que acepta la API, pero el envío navega del lado del cliente: la página no se recarga
 * y solo se vuelve a pedir el bloque de resultados, que está bajo su propio <Suspense>.
 */
export function FilterForm({ action, limpiar, children }: { action: string; limpiar?: string; children: ReactNode }) {
  const router = useRouter();
  const [pendiente, iniciar] = useTransition();

  function enviar(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const query = new URLSearchParams();
    for (const [nombre, valor] of new FormData(event.currentTarget).entries()) {
      const texto = typeof valor === "string" ? valor.trim() : "";
      if (texto) {
        query.set(nombre, texto);
      }
    }

    // Al filtrar se vuelve a la primera página: pageNumber no es un campo del formulario.
    const texto = query.toString();
    iniciar(() => router.push(texto ? `${action}?${texto}` : action, { scroll: false }));
  }

  return (
    <form method="get" action={action} className="filters" onSubmit={enviar}>
      {children}
      <div className="form-actions">
        <button type="submit" disabled={pendiente}>
          {pendiente ? "Filtrando…" : "Filtrar"}
        </button>
        {limpiar && <Link href={limpiar}>Limpiar</Link>}
      </div>
    </form>
  );
}
