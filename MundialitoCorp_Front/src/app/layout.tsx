import type { Metadata } from "next";
import { Inter } from "next/font/google";
import Link from "next/link";
import { Suspense } from "react";

import { MainNav, MainNavFallback } from "@/components/MainNav";
import { TEMA_INICIAL_SCRIPT, ThemeToggle } from "@/components/ThemeToggle";

import "./globals.css";

const inter = Inter({ subsets: ["latin"], variable: "--font-inter", display: "swap" });

export const metadata: Metadata = {
  title: {
    default: "Mundialito Corporativo",
    template: "%s · Mundialito Corporativo",
  },
  description: "Gestión del torneo de fútbol corporativo: equipos, partidos, resultados, tabla de posiciones y goleadores.",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html lang="es" className={inter.variable} suppressHydrationWarning>
      <head>
        <script dangerouslySetInnerHTML={{ __html: TEMA_INICIAL_SCRIPT }} />
      </head>
      <body>
        <header className="site-header">
          <div className="header-inner">
            <Link href="/" className="brand">
              <span className="brand-mark" aria-hidden="true">
                <svg viewBox="0 0 24 24" width="19" height="19" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round">
                  <circle cx="12" cy="12" r="9" />
                  <path d="m12 7.3 3.7 2.7-1.4 4.4H9.7L8.3 10z" />
                  <path d="M12 3v4.3M3.3 10.3l5 -.3M20.7 10.3l-5-.3M6.7 19.1 9.7 14.4M17.3 19.1l-3-4.7" />
                </svg>
              </span>
              Mundialito <span>Corporativo</span>
            </Link>
            <nav aria-label="Principal">
              <Suspense fallback={<MainNavFallback />}>
                <MainNav />
              </Suspense>
            </nav>
            <ThemeToggle />
          </div>
        </header>
        <main className="content">{children}</main>
        <footer className="site-footer">
          <div>
            <span>Mundialito de Fútbol Corporativo</span>
            <span>Torneo interno · Datos de la API del torneo</span>
          </div>
        </footer>
      </body>
    </html>
  );
}
