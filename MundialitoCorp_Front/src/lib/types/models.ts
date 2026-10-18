/**
 * Modelos que devuelve la API, con los mismos nombres de propiedades (español, como el dominio).
 */

export type EstadoTorneo = "Planificado" | "EnCurso" | "Finalizado";
export type EstadoPartido = "Programado" | "Jugado" | "Cancelado";

export const ESTADOS_PARTIDO: EstadoPartido[] = ["Programado", "Jugado", "Cancelado"];
export const ESTADOS_TORNEO: EstadoTorneo[] = ["Planificado", "EnCurso", "Finalizado"];

export interface Torneo {
  id: string;
  nombre: string;
  descripcion: string | null;
  fechaInicio: string;
  fechaFin: string | null;
  estado: EstadoTorneo;
  cantidadEquipos: number;
  cantidadPartidos: number;
}

export interface TorneoEquipo {
  equipoId: string;
  equipo: string;
  abreviatura: string;
  departamento: string | null;
  fechaInscripcion: string;
}

export interface Equipo {
  id: string;
  nombre: string;
  abreviatura: string;
  departamento: string | null;
  cantidadJugadores: number;
  fechaCreacion: string;
}

export interface Jugador {
  id: string;
  equipoId: string;
  equipo: string;
  posicionJuegoId: number;
  posicionJuego: string;
  nombres: string;
  apellidos: string;
  numeroCamiseta: number;
  activo: boolean;
  goles: number;
}

export interface PosicionJuego {
  id: number;
  nombre: string;
  abreviatura: string;
  orden: number;
  activo: boolean;
}

export interface Partido {
  id: string;
  torneoId: string;
  torneo: string;
  equipoLocalId: string;
  equipoLocal: string;
  equipoVisitanteId: string;
  equipoVisitante: string;
  fechaProgramada: string;
  sede: string | null;
  estado: EstadoPartido;
  golesLocal: number | null;
  golesVisitante: number | null;
  fechaJugado: string | null;
}

export interface Gol {
  id: string;
  jugadorId: string;
  jugador: string;
  equipoId: string;
  equipo: string;
  minuto: number;
}

export interface PartidoDetalle extends Partido {
  goles: Gol[];
}

export interface FilaTablaPosiciones {
  posicion: number;
  equipoId: string;
  equipo: string;
  abreviatura: string;
  partidosJugados: number;
  ganados: number;
  empatados: number;
  perdidos: number;
  golesFavor: number;
  golesContra: number;
  diferenciaGol: number;
  puntos: number;
}

export interface Goleador {
  posicion: number;
  jugadorId: string;
  jugador: string;
  numeroCamiseta: number;
  posicionJuego: string;
  equipoId: string;
  equipo: string;
  goles: number;
  partidosConGol: number;
}

/** Cuerpo de POST /api/partidos/{id}/resultado. */
export interface RegistrarResultado {
  golesLocal: number;
  golesVisitante: number;
  goles: { jugadorId: string; equipoId: string; minuto: number }[];
}

/** Cuerpo de POST y PUT de /api/torneos. Fechas sin hora ("2026-10-01"). */
export interface TorneoInput {
  nombre: string;
  descripcion: string | null;
  fechaInicio: string;
  fechaFin: string | null;
}

/** Cuerpo de POST y PUT de /api/equipos. */
export interface EquipoInput {
  nombre: string;
  abreviatura: string;
  departamento: string | null;
}

/** Cuerpo de POST /api/equipos/{id}/jugadores; PUT /api/jugadores/{id} agrega activo. */
export interface JugadorInput {
  posicionJuegoId: number;
  nombres: string;
  apellidos: string;
  numeroCamiseta: number;
}

export interface JugadorUpdate extends JugadorInput {
  activo: boolean;
}

/** Cuerpo de POST /api/partidos; PUT no cambia el torneo. Fecha en UTC (ISO 8601). */
export interface PartidoInput {
  torneoId: string;
  equipoLocalId: string;
  equipoVisitanteId: string;
  fechaProgramada: string;
  sede: string | null;
}
