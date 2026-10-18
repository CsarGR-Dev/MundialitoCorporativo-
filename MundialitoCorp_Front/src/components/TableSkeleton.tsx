/** Ocupa el lugar de la tabla mientras llega el resultado de la API. */
export function TableSkeleton({ filas = 5 }: { filas?: number }) {
  return (
    <div className="table-skeleton" aria-busy="true" aria-label="Cargando resultados">
      {Array.from({ length: filas + 1 }, (_, indice) => (
        <div key={indice} className="skeleton" />
      ))}
    </div>
  );
}
