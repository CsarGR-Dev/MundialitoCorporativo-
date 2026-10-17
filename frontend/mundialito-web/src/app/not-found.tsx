import Link from "next/link";

export default function NotFound() {
  return (
    <>
      <h1>No encontrado</h1>
      <p>El recurso que busca no existe o fue eliminado.</p>
      <Link href="/">Volver al inicio</Link>
    </>
  );
}
