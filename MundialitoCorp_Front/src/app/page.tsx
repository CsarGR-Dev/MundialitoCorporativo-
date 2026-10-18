import Link from "next/link";
import { Suspense, type ReactNode } from "react";

import { CarruselFlecha } from "@/components/CarruselTorneos";
import { EmptyState } from "@/components/EmptyState";
import { ErrorMessage } from "@/components/ErrorMessage";
import { EstadoTorneoBadge } from "@/components/EstadoTorneoBadge";
import { getPartidos } from "@/lib/api/partidos.api";
import { getGoleadores, getTablaPosiciones } from "@/lib/api/torneos.api";
import { formatFechaHora } from "@/lib/format";
import { param } from "@/lib/search-params";
import { resolveTorneo } from "@/lib/torneo-actual";
import type { Torneo } from "@/lib/types/models";

const SECCIONES = [
  {
    href: "/tabla-posiciones",
    titulo: "Tabla de posiciones",
    texto: "Puntos, diferencia de gol y goles a favor del torneo.",
    icono: (
      <Icono>
        <path d="M3 20h18" />
        <rect x="5" y="11" width="4" height="9" rx="1" />
        <rect x="10.5" y="6" width="4" height="14" rx="1" />
        <rect x="16" y="14" width="4" height="6" rx="1" />
      </Icono>
    ),
  },
  {
    href: "/goleadores",
    titulo: "Goleadores",
    texto: "Ranking de goleadores, con filtro por equipo.",
    icono: (
      <Icono>
        <circle cx="12" cy="12" r="9" />
        <circle cx="12" cy="12" r="4.5" />
        <circle cx="12" cy="12" r="1" />
      </Icono>
    ),
  },
  {
    href: "/partidos",
    titulo: "Partidos",
    texto: "Calendario con filtros por fecha, equipo y estado; registro de resultados.",
    icono: (
      <Icono>
        <rect x="3" y="5" width="18" height="16" rx="2" />
        <path d="M8 3v4M16 3v4M3 10h18" />
      </Icono>
    ),
  },
  {
    href: "/equipos",
    titulo: "Equipos",
    texto: "Equipos participantes, sus integrantes y departamentos.",
    icono: (
      <Icono>
        <circle cx="9" cy="7.5" r="3.5" />
        <path d="M15.5 19v-1.5a4 4 0 0 0-4-4h-5a4 4 0 0 0-4 4V19" />
        <path d="M16.5 5.2a3.5 3.5 0 0 1 0 6.6M21 19v-1.2a3.6 3.6 0 0 0-2.7-3.5" />
      </Icono>
    ),
  },
] as const;

/**
 * Inicio: presentación del torneo y un resumen de lo que está pasando (tabla, próximos partidos y
 * goleadores). El resumen va bajo <Suspense>: la portada se ve de inmediato y los datos llegan después.
 * searchParams se pasa sin await para no volver dinámica la portada; se lee dentro del resumen.
 */
export default function HomePage({ searchParams }: PageProps<"/">) {
  return (
    <>
      <section className="hero">
        <p className="eyebrow">Torneo corporativo</p>
        <h1>Mundialito de Fútbol Corporativo</h1>
        <p>
          Equipos por departamento, calendario de partidos, registro de resultados y la tabla de posiciones y el ranking de goleadores
          calculados a partir de los goles registrados.
        </p>
        <div className="hero-actions">
          <Link href="/tabla-posiciones">Ver tabla de posiciones</Link>
          <Link href="/partidos">Ir al calendario</Link>
        </div>
      </section>

      <Suspense fallback={<ResumenCargando />}>
        <ResumenTorneo searchParams={searchParams} />
      </Suspense>

      <h2>Secciones</h2>
      <div className="cards">
        {SECCIONES.map((seccion) => (
          <Link key={seccion.href} href={seccion.href} className="card">
            <span className="card-icon" aria-hidden="true">
              {seccion.icono}
            </span>
            <strong>{seccion.titulo}</strong>
            <p>{seccion.texto}</p>
            <span className="card-go">Entrar →</span>
          </Link>
        ))}
      </div>
    </>
  );
}

/**
 * Resumen del torneo elegido en la URL (?torneo=) y, si no viene, del que está en curso. Las flechas
 * recorren los torneos en orden de fecha, como un carrusel.
 */
async function ResumenTorneo({ searchParams }: { searchParams: PageProps<"/">["searchParams"] }) {
  const params = await searchParams;
  const seleccion = await resolveTorneo(param(params, "torneo"));
  if (!seleccion.ok) {
    return <ErrorMessage error={seleccion.error} />;
  }

  const torneo = seleccion.data.actual;
  if (!torneo) {
    return <EmptyState>Todavía no hay torneos registrados.</EmptyState>;
  }

  // Del más antiguo al más reciente: la flecha izquierda va al torneo anterior en el tiempo.
  const torneos = [...seleccion.data.torneos].sort((a, b) => a.fechaInicio.localeCompare(b.fechaInicio));
  const indice = torneos.findIndex((candidato) => candidato.id === torneo.id);
  const anterior = indice > 0 ? torneos[indice - 1] : undefined;
  const siguiente = indice >= 0 && indice < torneos.length - 1 ? torneos[indice + 1] : undefined;

  const [tabla, goleadores, proximos, jugados] = await Promise.all([
    getTablaPosiciones(torneo.id, { pageSize: 5 }),
    getGoleadores(torneo.id, { pageSize: 5 }),
    getPartidos({ torneoId: torneo.id, estado: "Programado", pageSize: 5, sortBy: "fechaProgramada", sortDirection: "asc" }),
    getPartidos({ torneoId: torneo.id, estado: "Jugado", pageSize: 1 }),
  ]);

  return (
    <>
      <div className="section-head carrusel">
        <div className="carrusel-control">
          <CarruselFlecha href={hrefTorneo(anterior)} etiqueta={etiquetaFlecha(anterior, "anterior")} direccion="anterior" />
          <h2>
            {torneo.nombre} <EstadoTorneoBadge estado={torneo.estado} />
          </h2>
          <CarruselFlecha href={hrefTorneo(siguiente)} etiqueta={etiquetaFlecha(siguiente, "siguiente")} direccion="siguiente" />
        </div>
        <div className="carrusel-meta">
          {torneos.length > 1 && (
            <PuntosCarrusel torneos={torneos} actual={torneo} indice={indice} />
          )}
        </div>
      </div>

      <div className="stats">
        <Estadistica valor={torneo.cantidadEquipos} etiqueta="Equipos inscritos" />
        <Estadistica valor={jugados.ok ? jugados.data.totalRecords : "—"} etiqueta="Partidos jugados" />
        <Estadistica valor={proximos.ok ? proximos.data.totalRecords : "—"} etiqueta="Partidos por jugar" />
        <Estadistica valor={torneo.cantidadPartidos} etiqueta="Partidos del calendario" />
      </div>

      <div className="panels">
        <Panel titulo="Tabla de posiciones" href={`/tabla-posiciones?torneo=${torneo.id}`}>
          {!tabla.ok ? (
            <ErrorMessage error={tabla.error} />
          ) : tabla.data.data.length === 0 ? (
            <EmptyState>Sin partidos jugados.</EmptyState>
          ) : (
            <ul className="lista">
              {tabla.data.data.map((fila) => (
                <li key={fila.equipoId}>
                  <span className="puesto">{fila.posicion}</span>
                  <span>
                    <span className="principal">{fila.equipo}</span>
                    <span className="secundario">
                      {fila.partidosJugados} PJ · {fila.golesFavor}:{fila.golesContra}
                    </span>
                  </span>
                  <span className="valor">
                    {fila.puntos} {fila.puntos === 1 ? "pt" : "pts"}
                  </span>
                </li>
              ))}
            </ul>
          )}
        </Panel>

        <Panel titulo="Próximos partidos" href={`/partidos?torneo=${torneo.id}&estado=Programado`}>
          {!proximos.ok ? (
            <ErrorMessage error={proximos.error} />
          ) : proximos.data.data.length === 0 ? (
            <EmptyState>No hay partidos programados.</EmptyState>
          ) : (
            <ul className="lista">
              {proximos.data.data.map((partido) => (
                <li key={partido.id}>
                  <span>
                    <span className="principal">
                      {partido.equipoLocal} vs {partido.equipoVisitante}
                    </span>
                    <span className="secundario">
                      {formatFechaHora(partido.fechaProgramada)} · {partido.sede ?? "Sin sede"}
                    </span>
                  </span>
                  <Link href={`/partidos/${partido.id}`} className="valor">
                    Ver
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </Panel>

        <Panel titulo="Goleadores" href={`/goleadores?torneo=${torneo.id}`}>
          {!goleadores.ok ? (
            <ErrorMessage error={goleadores.error} />
          ) : goleadores.data.data.length === 0 ? (
            <EmptyState>Todavía no hay goles registrados.</EmptyState>
          ) : (
            <ul className="lista">
              {goleadores.data.data.map((goleador) => (
                <li key={goleador.jugadorId}>
                  <span className="puesto">{goleador.posicion}</span>
                  <span>
                    <span className="principal">{goleador.jugador}</span>
                    <span className="secundario">{goleador.equipo}</span>
                  </span>
                  <span className="valor">
                    {goleador.goles} {goleador.goles === 1 ? "gol" : "goles"}
                  </span>
                </li>
              ))}
            </ul>
          )}
        </Panel>
      </div>
    </>
  );
}

function hrefTorneo(torneo: Torneo | undefined) {
  return torneo ? `/?torneo=${torneo.id}` : undefined;
}

function etiquetaFlecha(torneo: Torneo | undefined, direccion: "anterior" | "siguiente") {
  const titulo = direccion === "anterior" ? "Torneo anterior" : "Torneo siguiente";
  return torneo ? `${titulo}: ${torneo.nombre}` : `${titulo} (no hay)`;
}

/** Con pocos torneos, un punto por torneo; con muchos, solo la posición. */
function PuntosCarrusel({ torneos, actual, indice }: { torneos: Torneo[]; actual: Torneo; indice: number }) {
  if (torneos.length > 8) {
    return (
      <span className="carrusel-pos">
        {indice + 1} de {torneos.length}
      </span>
    );
  }

  return (
    <span className="carrusel-puntos">
      {torneos.map((torneo) => (
        <Link
          key={torneo.id}
          href={`/?torneo=${torneo.id}`}
          className="carrusel-punto"
          aria-current={torneo.id === actual.id ? "true" : undefined}
          aria-label={torneo.nombre}
          title={torneo.nombre}
          scroll={false}
        />
      ))}
    </span>
  );
}

function ResumenCargando() {
  return (
    <div className="panels" aria-busy="true" aria-label="Cargando el resumen del torneo">
      {[0, 1, 2].map((indice) => (
        <div key={indice} className="skeleton skeleton-block" />
      ))}
    </div>
  );
}

function Estadistica({ valor, etiqueta }: { valor: number | string; etiqueta: string }) {
  return (
    <div className="stat">
      <span className="stat-value">{valor}</span>
      <span className="stat-label">{etiqueta}</span>
    </div>
  );
}

function Panel({ titulo, href, children }: { titulo: string; href: string; children: ReactNode }) {
  return (
    <section className="panel">
      <div className="panel-head">
        <h2>{titulo}</h2>
        <Link href={href}>Ver todo →</Link>
      </div>
      <div className="panel-body">{children}</div>
    </section>
  );
}

function Icono({ children }: { children: ReactNode }) {
  return (
    <svg viewBox="0 0 24 24" width="20" height="20" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round">
      {children}
    </svg>
  );
}
