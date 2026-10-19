# Frontend Next.js del Mundialito (ítem 12). Contexto de build: esta carpeta (MundialitoCorp_Front).
# Multi-stage con output "standalone": la imagen final lleva solo el servidor y las dependencias que usa.

FROM node:22-alpine AS deps
WORKDIR /app
COPY package.json package-lock.json ./
RUN npm ci --no-audit --no-fund

FROM node:22-alpine AS build
WORKDIR /app
ENV NEXT_TELEMETRY_DISABLED=1
COPY --from=deps /app/node_modules ./node_modules
COPY . .
# public/ puede no existir en un clon (git no guarda carpetas vacías). El build no consulta la API.
RUN mkdir -p public && npm run build

FROM node:22-alpine AS runtime
WORKDIR /app
ENV NODE_ENV=production \
    NEXT_TELEMETRY_DISABLED=1 \
    PORT=3000 \
    HOSTNAME=0.0.0.0
COPY --from=build --chown=node:node /app/public ./public
COPY --from=build --chown=node:node /app/.next/standalone ./
COPY --from=build --chown=node:node /app/.next/static ./.next/static

# API_URL se lee al ejecutar (no en el build): en docker-compose apunta a http://api:8080.
USER node
EXPOSE 3000
CMD ["node", "server.js"]
