import type { Metadata } from "next";
import Link from "next/link";

import "./globals.css";

export const metadata: Metadata = {
  title: {
    default: "Mundialito Corporativo",
    template: "%s · Mundialito Corporativo",
  },
  description: "Gestión del torneo de fútbol corporativo: equipos, partidos, resultados, tabla de posiciones y goleadores.",
};

const NAVEGACION = [
  { href: "/tabla-posiciones", label: "Tabla de posiciones" },
  { href: "/goleadores", label: "Goleadores" },
  { href: "/partidos", label: "Partidos" },
  { href: "/equipos", label: "Equipos" },
] as const;

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html lang="es">
      <body>
        <header className="site-header">
          <Link href="/" className="brand">
            Mundialito Corporativo
          </Link>
          <nav aria-label="Principal">
            {NAVEGACION.map((item) => (
              <Link key={item.href} href={item.href}>
                {item.label}
              </Link>
            ))}
          </nav>
        </header>
        <main className="content">{children}</main>
      </body>
    </html>
  );
}
