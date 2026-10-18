import { TableSkeleton } from "@/components/TableSkeleton";

/** Se muestra al entrar a una ruta mientras llega su contenido; dentro de cada lista, el esqueleto queda solo en la tabla. */
export default function Loading() {
  return (
    <div className="loading" aria-busy="true" aria-label="Cargando">
      <div className="skeleton skeleton-title" />
      <div className="skeleton skeleton-bar" />
      <TableSkeleton />
    </div>
  );
}
