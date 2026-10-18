import Link from "next/link";

export default function NotFound() {
  return (
    <>
      <div className="page-head">
        <div>
          <p className="eyebrow">Error 404</p>
          <h1>No encontrado</h1>
          <p className="page-sub">El recurso que busca no existe o fue eliminado.</p>
        </div>
      </div>
      <Link href="/" className="back-link">
        ← Volver al inicio
      </Link>
    </>
  );
}
