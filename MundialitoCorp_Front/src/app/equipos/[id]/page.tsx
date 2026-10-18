import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { Suspense } from "react";

import { EmptyState } from "@/components/EmptyState";
import { ErrorMessage } from "@/components/ErrorMessage";
import { FilterForm } from "@/components/FilterForm";
import { Pagination } from "@/components/Pagination";
import { SortableHeader } from "@/components/SortableHeader";
import { TableSkeleton } from "@/components/TableSkeleton";
import { getEquipo, getIntegrantes, getPosicionesJuego } from "@/lib/api/equipos.api";
import { pageQuery, param, searchKey, type SearchParams } from "@/lib/search-params";

export const metadata: Metadata = { title: "Integrantes del equipo" };

const SORT_FIELDS = ["apellidos", "nombres", "numeroCamiseta", "posicionJuego"] as const;

/** Datos del equipo e integrantes paginados, con filtro por posición de juego; alta, edición y baja de integrantes. */
export default async function EquipoPage({ params, searchParams }: PageProps<"/equipos/[id]">) {
  const [{ id }, query] = await Promise.all([params, searchParams]);
  const path = `/equipos/${id}`;
  const posicionJuegoId = param(query, "posicionJuegoId");

  // Los integrantes se piden sin await: viajan al bloque de resultados y se transmiten cuando llegan,
  // en paralelo con los datos del equipo y las posiciones del filtro.
  const integrantes = getIntegrantes(id, { ...pageQuery(query, SORT_FIELDS), posicionJuegoId });
  const [equipo, posiciones] = await Promise.all([getEquipo(id), getPosicionesJuego()]);

  if (!equipo.ok) {
    if (equipo.error.status === 404) {
      notFound();
    }
    return <ErrorMessage error={equipo.error} />;
  }

  const e = equipo.data;

  return (
    <>
      <Link href="/equipos" className="back-link">
        ← Equipos
      </Link>
      <div className="page-head">
        <div>
          <p className="eyebrow">Equipo</p>
          <h1>
            {e.nombre} <span className="muted">({e.abreviatura})</span>
          </h1>
          <p className="page-sub">
            Departamento: {e.departamento ?? "—"} · {e.cantidadJugadores} integrantes
          </p>
        </div>
      </div>

      <h2>Integrantes</h2>
      <FilterForm action={path} limpiar={path}>
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
      </FilterForm>

      <Suspense key={searchKey(query)} fallback={<TableSkeleton />}>
        <ListaIntegrantes integrantes={integrantes} params={query} path={path} />
      </Suspense>
    </>
  );
}

/** Solo este bloque se vuelve a pedir al filtrar, ordenar o cambiar de página. */
async function ListaIntegrantes({
  integrantes: consulta,
  params,
  path,
}: {
  integrantes: ReturnType<typeof getIntegrantes>;
  params: SearchParams;
  path: string;
}) {
  const integrantes = await consulta;

  if (!integrantes.ok) {
    return <ErrorMessage error={integrantes.error} />;
  }

  if (integrantes.data.data.length === 0) {
    return <EmptyState>El equipo no tiene integrantes para los filtros seleccionados.</EmptyState>;
  }

  return (
    <>
      <div className="table-wrap">
        <table>
          <thead>
            <tr>
              <SortableHeader label="#" field="numeroCamiseta" pathname={path} searchParams={params} defaultField="apellidos" className="num" />
              <SortableHeader label="Apellidos" field="apellidos" pathname={path} searchParams={params} defaultField="apellidos" />
              <SortableHeader label="Nombres" field="nombres" pathname={path} searchParams={params} defaultField="apellidos" />
              <SortableHeader label="Posición" field="posicionJuego" pathname={path} searchParams={params} defaultField="apellidos" />
              <th className="num">Goles</th>
              <th>Estado</th>
            </tr>
          </thead>
          <tbody>
            {integrantes.data.data.map((jugador) => (
              <tr key={jugador.id}>
                <td className="num">{jugador.numeroCamiseta}</td>
                <td>{jugador.apellidos}</td>
                <td>{jugador.nombres}</td>
                <td>{jugador.posicionJuego}</td>
                <td className="num">{jugador.goles}</td>
                <td>{jugador.activo ? "Activo" : "Inactivo"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <Pagination pathname={path} searchParams={params} page={integrantes.data} />
    </>
  );
}
