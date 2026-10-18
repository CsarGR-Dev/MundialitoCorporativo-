"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

const SECCIONES = [
  { href: "/", label: "Inicio" },
  { href: "/tabla-posiciones", label: "Tabla de posiciones" },
  { href: "/goleadores", label: "Goleadores" },
  { href: "/partidos", label: "Partidos" },
  { href: "/equipos", label: "Equipos" },
  { href: "/torneos", label: "Torneos" },
] as const;

/**
 * Navegación principal con la sección visible marcada. Lee la ruta en el navegador, así que el layout
 * la envuelve en <Suspense> y entrega primero MainNavFallback, con los mismos enlaces sin marcar.
 */
export function MainNav() {
  const pathname = usePathname();

  return (
    <>
      {SECCIONES.map((seccion) => {
        const actual = seccion.href === "/" ? pathname === "/" : pathname === seccion.href || pathname.startsWith(`${seccion.href}/`);
        return (
          <Link key={seccion.href} href={seccion.href} className="nav-link" aria-current={actual ? "page" : undefined}>
            {seccion.label}
          </Link>
        );
      })}
    </>
  );
}

export function MainNavFallback() {
  return (
    <>
      {SECCIONES.map((seccion) => (
        <Link key={seccion.href} href={seccion.href} className="nav-link">
          {seccion.label}
        </Link>
      ))}
    </>
  );
}
