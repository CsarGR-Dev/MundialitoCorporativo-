import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";

import { EmptyState } from "@/components/EmptyState";
import { ErrorMessage } from "@/components/ErrorMessage";
import { Pagination } from "@/components/Pagination";
import { SortableHeader } from "@/components/SortableHeader";
import { getEquipo, getIntegrantes, getPosicionesJuego } from "@/lib/api/equipos.api";
import { pageQuery, param } from "@/lib/search-params";

export const metadata: Metadata = { title: "Integrantes del equipo" };

const SORT_FIELDS = ["apellidos", "nombres", "numeroCamiseta", "posicionJuego"] as const;

/** Datos del equipo e integrantes paginados, con filtro por posición de juego. */
export default async function EquipoPage({ params, searchParams }: PageProps<"/equipos/[id]">) {
  const [{ id }, query] = await Promise.all([params, searchParams]);
  const path = `/equipos/${id}`;
  const posicionJuegoId = param(query, "posicionJuegoId");

  const [equipo, integrantes, posiciones] = await Promise.all([
    getEquipo(id),
    getIntegrantes(id, { ...pageQuery(query, SORT_FIELDS), posicionJuegoId }),
    getPosicionesJuego(),
  ]);

  if (!equipo.ok) {
    if (equipo.error.status === 404) {
      notFound();
    }
    return <ErrorMessage error={equipo.error} />;
  }

  const e = equipo.data;

  return (
    <>
      <p>
        <Link href="/equipos">← Equipos</Link>
      </p>
      <h1>
        {e.nombre} <span className="muted">({e.abreviatura})</span>
      </h1>
      <p>Departamento: {e.departamento ?? "—"} · {e.cantidadJugadores} integrantes</p>

      <h2>Integrantes</h2>
      <form method="get" action={path} className="filters">
        <label>
          Posición
          <select name="posicionJuegoId" defaultValue={posicionJuegoId ?? ""}>
            <option value="">Todas</option>
            {posiciones.ok &&
              posiciones.data.data.map((posicion) => (
                <option key={posicion.id} value={posicion.id}>
                  {posicion.nombre}
                </option>
              ))}
          </select>
        </label>
        <button type="submit">Filtrar</button>
      </form>

      {!integrantes.ok ? (
        <ErrorMessage error={integrantes.error} />
      ) : integrantes.data.data.length === 0 ? (
        <EmptyState>El equipo no tiene integrantes para los filtros seleccionados.</EmptyState>
      ) : (
        <>
          <table>
            <thead>
              <tr>
                <SortableHeader label="#" field="numeroCamiseta" pathname={path} searchParams={query} defaultField="apellidos" />
                <SortableHeader label="Apellidos" field="apellidos" pathname={path} searchParams={query} defaultField="apellidos" />
                <SortableHeader label="Nombres" field="nombres" pathname={path} searchParams={query} defaultField="apellidos" />
                <SortableHeader label="Posición" field="posicionJuego" pathname={path} searchParams={query} defaultField="apellidos" />
                <th className="num">Goles</th>
                <th>Estado</th>
              </tr>
            </thead>
            <tbody>
              {integrantes.data.data.map((jugador) => (
                <tr key={jugador.id}>
                  <td>{jugador.numeroCamiseta}</td>
                  <td>{jugador.apellidos}</td>
                  <td>{jugador.nombres}</td>
                  <td>{jugador.posicionJuego}</td>
                  <td className="num">{jugador.goles}</td>
                  <td>{jugador.activo ? "Activo" : "Inactivo"}</td>
                </tr>
              ))}
            </tbody>
          </table>
          <Pagination pathname={path} searchParams={query} page={integrantes.data} />
        </>
      )}
    </>
  );
}
