import Link from "next/link";

const SECCIONES = [
  { href: "/tabla-posiciones", titulo: "Tabla de posiciones", texto: "Puntos, diferencia de gol y goles a favor del torneo." },
  { href: "/goleadores", titulo: "Goleadores", texto: "Ranking de goleadores, con filtro por equipo." },
  { href: "/partidos", titulo: "Partidos", texto: "Calendario con filtros por fecha, equipo y estado; registro de resultados." },
  { href: "/equipos", titulo: "Equipos", texto: "Equipos participantes y sus integrantes." },
] as const;

export default function HomePage() {
  return (
    <>
      <h1>Mundialito de Fútbol Corporativo</h1>
      <p className="muted">Control del torneo: equipos, integrantes, calendario, resultados, tabla de posiciones y goleadores.</p>
      <div className="cards">
        {SECCIONES.map((seccion) => (
          <Link key={seccion.href} href={seccion.href} className="card">
            <strong>{seccion.titulo}</strong>
            <p>{seccion.texto}</p>
          </Link>
        ))}
      </div>
    </>
  );
}
