import type { ApiError } from "@/lib/types/api";

/**
 * Estado que devuelve una Server Action a su formulario: vacío si todo salió bien (la acción redirige)
 * o el error de la API para mostrarlo sin perder lo que el usuario escribió.
 */
export interface FormState {
  error?: ApiError;
}

/**
 * Lectura de campos en las Server Actions. Solo convierten tipos: las reglas (largos, rangos, duplicados)
 * las valida la API, que responde 400 o 409 con el detalle de cada campo.
 */
export function text(formData: FormData, name: string): string {
  const value = formData.get(name);
  return typeof value === "string" ? value.trim() : "";
}

/** Campo opcional: vacío se envía como null. */
export function optionalText(formData: FormData, name: string): string | null {
  return text(formData, name) || null;
}

/** Un valor vacío o no numérico queda como NaN, que JSON envía como null y la API rechaza con 400. */
export function integer(formData: FormData, name: string): number {
  const value = text(formData, name);
  return value === "" ? Number.NaN : Number(value);
}

export function checkbox(formData: FormData, name: string): boolean {
  return formData.get(name) === "on";
}
