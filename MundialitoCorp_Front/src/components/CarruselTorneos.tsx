"use client";

import Link, { useLinkStatus } from "next/link";

/**
 * Flecha del carrusel de torneos del inicio. Es un enlace normal (se puede abrir en otra pestaña y el
 * torneo queda en la URL), que se atenúa mientras llega el resumen del torneo elegido.
 */
export function CarruselFlecha({ href, etiqueta, direccion }: { href?: string; etiqueta: string; direccion: "anterior" | "siguiente" }) {
  if (!href) {
    return (
      <span className="carrusel-flecha" aria-disabled="true" aria-label={etiqueta} role="link">
        <Chevron direccion={direccion} />
      </span>
    );
  }

  return (
    <Link href={href} className="carrusel-flecha" aria-label={etiqueta} title={etiqueta} scroll={false}>
      <Icono direccion={direccion} />
    </Link>
  );
}

/** useLinkStatus solo funciona dentro de un <Link>: marca la flecha mientras dura la navegación. */
function Icono({ direccion }: { direccion: "anterior" | "siguiente" }) {
  const { pending } = useLinkStatus();

  return (
    <span className="carrusel-icono" data-pendiente={pending ? "" : undefined}>
      <Chevron direccion={direccion} />
    </span>
  );
}

function Chevron({ direccion }: { direccion: "anterior" | "siguiente" }) {
  return (
    <svg viewBox="0 0 24 24" width="16" height="16" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
      <path d={direccion === "anterior" ? "m14.5 5-6 7 6 7" : "m9.5 5 6 7-6 7"} />
    </svg>
  );
}
