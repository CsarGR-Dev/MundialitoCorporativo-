"use client";

import { useSyncExternalStore } from "react";

export type Tema = "light" | "dark";

const CLAVE = "mundialito-tema";
const CONSULTA_OSCURO = "(prefers-color-scheme: dark)";

/**
 * Script que corre antes de pintar: aplica el tema guardado para que no se vea el modo claro
 * un instante antes de cambiar al oscuro. Sin tema guardado manda la preferencia del sistema.
 */
export const TEMA_INICIAL_SCRIPT = `try{var t=localStorage.getItem(${JSON.stringify(CLAVE)});if(t==="light"||t==="dark")document.documentElement.dataset.theme=t}catch(e){}`;

const oyentes = new Set<() => void>();

/** El tema vive en el DOM (data-theme) y en la preferencia del sistema, no en el estado de React. */
function suscribir(avisar: () => void) {
  oyentes.add(avisar);
  const consulta = window.matchMedia(CONSULTA_OSCURO);
  consulta.addEventListener("change", avisar);

  return () => {
    oyentes.delete(avisar);
    consulta.removeEventListener("change", avisar);
  };
}

function temaActual(): Tema {
  const fijado = document.documentElement.dataset.theme;
  if (fijado === "light" || fijado === "dark") {
    return fijado;
  }
  return window.matchMedia(CONSULTA_OSCURO).matches ? "dark" : "light";
}

function fijarTema(tema: Tema) {
  document.documentElement.dataset.theme = tema;
  try {
    localStorage.setItem(CLAVE, tema);
  } catch {
    // Navegador sin almacenamiento: el tema dura lo que dure la página.
  }
  for (const avisar of oyentes) {
    avisar();
  }
}

/** Alterna entre claro y oscuro y recuerda la elección en el navegador. */
export function ThemeToggle() {
  // En el servidor no se conoce el tema (null); React vuelve a pintar con el real al hidratar.
  const tema = useSyncExternalStore(suscribir, temaActual, () => null);
  const oscuro = tema === "dark";

  return (
    <button
      type="button"
      className="theme-toggle"
      onClick={() => fijarTema(oscuro ? "light" : "dark")}
      aria-pressed={oscuro}
      aria-label={oscuro ? "Cambiar a modo claro" : "Cambiar a modo oscuro"}
      title={oscuro ? "Modo claro" : "Modo oscuro"}
    >
      <svg viewBox="0 0 24 24" width="17" height="17" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" aria-hidden="true">
        {oscuro ? (
          <>
            <circle cx="12" cy="12" r="4.2" />
            <path d="M12 3v2M12 19v2M3 12h2M19 12h2M5.6 5.6 7 7M17 17l1.4 1.4M18.4 5.6 17 7M7 17l-1.4 1.4" />
          </>
        ) : (
          <path d="M20 13.4A8.4 8.4 0 1 1 10.6 4a6.8 6.8 0 0 0 9.4 9.4z" />
        )}
      </svg>
    </button>
  );
}
