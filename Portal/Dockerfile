# =============================================================================
# ProyectoCafe Portal — Dockerfile multi-etapa
# =============================================================================
# PATRÓN RUNTIME CONFIG: Las variables de entorno NO se hornean en build time.
# Se inyectan en runtime desde:
#   - app/layout.tsx  →  window.__RUNTIME_ENV__  (server-side)
#   - lib/runtime-config.ts  →  getRuntimeConfig()  (client-side)
#
# Esto permite que la MISMA imagen Docker funcione en DEV, QA y PROD
# sin necesidad de rebuild. Solo se pasan como env vars al contenedor.
# =============================================================================

# ── Etapa 1: Dependencias ────────────────────────────────────────────────────
FROM node:22-slim AS deps
WORKDIR /app

COPY package.json package-lock.json* ./
RUN npm ci

# ── Etapa 2: Builder ─────────────────────────────────────────────────────────
FROM node:22-slim AS builder
WORKDIR /app

COPY --from=deps /app/node_modules ./node_modules
COPY . .

ENV NEXT_TELEMETRY_DISABLED=1
ENV NODE_ENV=production

# Lint y typecheck se ejecutan SIEMPRE en el build (igual que en CI).
# No se desactivan: si se necesitara hacerlo puntualmente, las opciones
# oficiales de Next.js son next.config.js → eslint.ignoreDuringBuilds y
# typescript.ignoreBuildErrors (no existen variables NEXT_IGNORE_*).

# ── SOLO metadata estática que no cambia entre ambientes ──────────────────────
# Las URLs (API) se inyectan en RUNTIME, no aquí.
# 🔧 Build arguments para metadata de versión (se queman en el build)
ARG NEXT_PUBLIC_APP_VERSION=1.0.0
ARG NEXT_PUBLIC_BUILD_VERSION=1.0.0
ARG BUILD_TIME
ENV NEXT_PUBLIC_APP_VERSION=${NEXT_PUBLIC_APP_VERSION} \
    NEXT_PUBLIC_BUILD_VERSION=${NEXT_PUBLIC_BUILD_VERSION} \
    BUILD_TIME=${BUILD_TIME}

# Placeholder vacío para satisfacer referencias NEXT_PUBLIC_* en tiempo de build
# (Next.js las reemplaza en runtime vía window.__RUNTIME_ENV__)
# ⚠️ NOTA: Las variables NEXT_PUBLIC_* son siempre bakeadas en build time por Next.js.
#    En runtime, app/layout.tsx lee primero vars SIN prefijo (PROYECTOCAFE_API_URL)
#    que sí se leen en runtime en Server Components con force-dynamic.
#    En Docker/Azure: configurar PROYECTOCAFE_API_URL.
#    En local (.env.local): usar NEXT_PUBLIC_PROYECTOCAFE_API_URL.
ENV NEXT_PUBLIC_PROYECTOCAFE_API_URL=""
ENV NEXT_PUBLIC_APP_NAME="ProyectoCafe"

# Vars runtime (sin NEXT_PUBLIC_) — leídas por app/layout.tsx en Server Component
ENV PROYECTOCAFE_API_URL=""

RUN npm run build

# ── Etapa 3: Runner ──────────────────────────────────────────────────────────
FROM node:22-slim AS runner
WORKDIR /app

ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1

# Usuario no-root
RUN groupadd --system --gid 1001 nodejs \
 && useradd  --system --uid 1001 -g nodejs nextjs

# Copiar artefactos del build standalone
COPY --from=builder /app/public              ./public
COPY --from=builder /app/.next/standalone    ./
COPY --from=builder /app/.next/static        ./.next/static

RUN chown -R nextjs:nodejs /app

USER nextjs

EXPOSE 3000

# =============================================================================
# Variables de runtime — DEBEN pasarse al contenedor (Azure Container Apps,
# docker-compose, etc.). Ver .env.example para la lista completa.
#
# PROYECTOCAFE_API_URL (o NEXT_PUBLIC_PROYECTOCAFE_API_URL en local) debe ser
# alcanzable por DOS consumidores distintos:
#   1. El navegador del usuario: app/layout.tsx la inyecta en
#      window.__RUNTIME_ENV__ y el cliente llama directamente a la API.
#   2. El servidor del Portal: middleware y Server Components validan la
#      sesión contra /api/v1/auth/me usando esa misma URL.
# Por eso no puede ser un host interno de Docker (p. ej. http://api:8080)
# si el navegador no resuelve ese nombre: usa una URL pública, o configura
# DNS/red de forma que ambos la alcancen.
# =============================================================================
ENV PORT=3000
ENV HOSTNAME="0.0.0.0"

CMD ["node", "server.js"]
