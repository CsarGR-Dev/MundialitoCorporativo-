import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  cacheComponents: true,
  partialPrefetching: true,
  // Imagen de Docker liviana: solo el servidor y las dependencias que usa (ítem 12).
  output: "standalone",
};

export default nextConfig;
