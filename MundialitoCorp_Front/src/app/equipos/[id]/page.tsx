import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { Suspense } from "react";

import { EmptyState } from "@/components/EmptyState";
import { ErrorMessage } from "@/components/ErrorMessage";
import { FilterForm } from "@/components/FilterForm";
import { ConfirmActionButton } from "@/components/forms/ConfirmActionButton";
import { Pagination } from "@/components/Pagination";
import { SortableHeader } from "@/components/SortableHeader";
import { TableSkeleton } from "@/components/TableSkeleton";
import { getEquipo, getIntegrantes, getPosicionesJuego } from "@/lib/api/equipos.api";
import { pageQuery, param, searchKey, type SearchParams } from "@/lib/search-params";

import { eliminarEquipoAction, eliminarIntegranteAction } from "../actions";

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
        <div className="page-actions">
          <Link href={`${path}/editar`} className="btn-secondary">
            Editar
          </Link>
          <ConfirmActionButton
            action={eliminarEquipoAction.bind(null, e.id)}
            label="Eliminar"
            pendingLabel="Eliminando…"
            confirmMessage={`¿Eliminar el equipo "${e.nombre}"?`}
            errorTitle="No se pudo eliminar el equipo."
            variant="danger"
          />
        </div>
      </div>

      <div className="section-head">
        <h2>Integrantes</h2>
        <Link href={`${path}/jugadores/nuevo`} className="btn-link">
          Registrar integrante
        </Link>
      </div>
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
        <ListaIntegrantes integrantes={integrantes} params={query} path={path} equipoId={e.id} />
      </Suspense>
    </>
  );
}

/** Solo este bloque se vuelve a pedir al filtrar, ordenar o cambiar de página. */
async function ListaIntegrantes({
  integrantes: consulta,
  params,
  path,
  equipoId,
}: {
  integrantes: ReturnType<typeof getIntegrantes>;
  params: SearchParams;
  path: string;
  equipoId: string;
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
              <th />
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
                <td>
                  <div className="row-actions">
                    <Link href={`${path}/jugadores/${jugador.id}/editar`}>Editar</Link>
                    <ConfirmActionButton
                      action={eliminarIntegranteAction.bind(null, equipoId, jugador.id)}
                      label="Eliminar"
                      pendingLabel="Eliminando…"
                      confirmMessage={`¿Eliminar a ${jugador.nombres} ${jugador.apellidos} del equipo?`}
                      errorTitle="No se pudo eliminar el integrante."
                      variant="danger"
                    />
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <Pagination pathname={path} searchParams={params} page={integrantes.data} />
    </>
  );
}
