import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";

import { EmptyState } from "@/components/EmptyState";
import { ErrorMessage } from "@/components/ErrorMessage";
import { EstadoTorneoBadge } from "@/components/EstadoTorneoBadge";
import { ActionForm } from "@/components/forms/ActionForm";
import { ConfirmActionButton } from "@/components/forms/ConfirmActionButton";
import { getEquipos } from "@/lib/api/equipos.api";
import { getTorneo, getTorneoEquipos } from "@/lib/api/torneos.api";
import { formatFecha } from "@/lib/format";
import type { EstadoTorneo } from "@/lib/types/models";

import { cambiarEstadoTorneoAction, eliminarTorneoAction, inscribirEquipoAction, retirarEquipoAction } from "../actions";

export const metadata: Metadata = { title: "Torneo" };

/** Siguiente estado del ciclo de vida del torneo y el texto del botón que lo aplica. */
const SIGUIENTE_ESTADO: Partial<Record<EstadoTorneo, { estado: EstadoTorneo; label: string; confirm: string }>> = {
  Planificado: { estado: "EnCurso", label: "Iniciar torneo", confirm: "¿Iniciar el torneo? Pasará a estado En curso." },
  EnCurso: { estado: "Finalizado", label: "Finalizar torneo", confirm: "¿Finalizar el torneo? Un torneo finalizado ya no admite cambios." },
};

/** Datos del torneo, cambio de estado y equipos participantes (inscripción y retiro). */
export default async function TorneoPage({ params }: PageProps<"/torneos/[id]">) {
  const { id } = await params;
  const [torneo, inscritos, equipos] = await Promise.all([
    getTorneo(id),
    getTorneoEquipos(id, { pageSize: 100, sortBy: "equipo" }),
    getEquipos({ pageSize: 100, sortBy: "nombre" }),
  ]);

  if (!torneo.ok) {
    if (torneo.error.status === 404) {
      notFound();
    }
    return <ErrorMessage error={torneo.error} />;
  }

  const t = torneo.data;
  const editable = t.estado !== "Finalizado";
  const siguiente = SIGUIENTE_ESTADO[t.estado];
  const idsInscritos = new Set(inscritos.ok ? inscritos.data.data.map((equipo) => equipo.equipoId) : []);
  const disponibles = equipos.ok ? equipos.data.data.filter((equipo) => !idsInscritos.has(equipo.id)) : [];

  return (
    <>
      <Link href="/torneos" className="back-link">
        ← Torneos
      </Link>
      <div className="page-head">
        <div>
          <p className="eyebrow">Torneo</p>
          <h1>{t.nombre}</h1>
          <p className="meta-row">
            <EstadoTorneoBadge estado={t.estado} />
            <span>
              {formatFecha(t.fechaInicio)} – {formatFecha(t.fechaFin)}
            </span>
            <span>·</span>
            <span>
              {t.cantidadEquipos} equipos · {t.cantidadPartidos} partidos
            </span>
          </p>
          {t.descripcion && <p className="page-sub">{t.descripcion}</p>}
        </div>
        <div className="page-actions">
          {editable && (
            <Link href={`/torneos/${t.id}/editar`} className="btn-secondary">
              Editar
            </Link>
          )}
          {siguiente && (
            <ConfirmActionButton
              action={cambiarEstadoTorneoAction.bind(null, t.id, siguiente.estado)}
              label={siguiente.label}
              pendingLabel="Actualizando…"
              confirmMessage={siguiente.confirm}
              errorTitle="No se pudo cambiar el estado del torneo."
            />
          )}
          <ConfirmActionButton
            action={eliminarTorneoAction.bind(null, t.id)}
            label="Eliminar"
            pendingLabel="Eliminando…"
            confirmMessage={`¿Eliminar el torneo "${t.nombre}"?`}
            errorTitle="No se pudo eliminar el torneo."
            variant="danger"
          />
        </div>
      </div>

      <nav className="quick-links" aria-label="Secciones del torneo">
        <Link href={`/partidos?torneo=${t.id}`}>Partidos</Link>
        <Link href={`/tabla-posiciones?torneo=${t.id}`}>Tabla de posiciones</Link>
        <Link href={`/goleadores?torneo=${t.id}`}>Goleadores</Link>
      </nav>

      <h2>Equipos participantes</h2>

      {editable && (
        <ActionForm
          action={inscribirEquipoAction.bind(null, t.id)}
          submitLabel="Inscribir"
          pendingLabel="Inscribiendo…"
          errorTitle="No se pudo inscribir el equipo."
          idempotent
          className="filters"
        >
          {!equipos.ok ? (
            <ErrorMessage error={equipos.error} />
          ) : (
            <label>
              Equipo
              <select name="equipoId" defaultValue="" required disabled={disponibles.length === 0}>
                <option value="" disabled>
                  {disponibles.length === 0 ? "Todos los equipos están inscritos" : "Seleccione"}
                </option>
                {disponibles.map((equipo) => (
                  <option key={equipo.id} value={equipo.id}>
                    {equipo.nombre}
                  </option>
                ))}
              </select>
            </label>
          )}
        </ActionForm>
      )}

      {!inscritos.ok ? (
        <ErrorMessage error={inscritos.error} />
      ) : inscritos.data.data.length === 0 ? (
        <EmptyState>Todavía no hay equipos inscritos en este torneo.</EmptyState>
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Equipo</th>
                <th>Abreviatura</th>
                <th>Departamento</th>
                <th>Inscripción</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {inscritos.data.data.map((equipo) => (
                <tr key={equipo.equipoId}>
                  <td>
                    <Link href={`/equipos/${equipo.equipoId}`}>{equipo.equipo}</Link>
                  </td>
                  <td>{equipo.abreviatura}</td>
                  <td>{equipo.departamento ?? "—"}</td>
                  <td>{formatFecha(equipo.fechaInscripcion.slice(0, 10))}</td>
                  <td>
                    {editable && (
                      <ConfirmActionButton
                        action={retirarEquipoAction.bind(null, t.id, equipo.equipoId)}
                        label="Retirar"
                        pendingLabel="Retirando…"
                        confirmMessage={`¿Retirar a "${equipo.equipo}" del torneo?`}
                        errorTitle="No se pudo retirar el equipo."
                        variant="danger"
                      />
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </>
  );
}
