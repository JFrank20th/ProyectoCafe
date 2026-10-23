/** @type {import('next').NextConfig} */
const nextConfig = {
  reactStrictMode: true,

  experimental: {
    // Work around a known Next 15 dev-only RSC manifest issue on Windows.
    devtoolSegmentExplorer: false,
  },

  // ============================================================================
  // CORS
  // ============================================================================
  // No configurar cabeceras CORS en este archivo (ni en middleware/route handlers).
  // CORS se centraliza en la API (.NET) mediante una allowlist explícita de
  // orígenes. Emitir aquí cabeceras CORS duplicaría o contradiría la respuesta de
  // la API; además, un origen comodín es incompatible con credenciales (cookies).
  // El Portal solo consume la API con credentials: 'include'.
  // ============================================================================

  // ============================================================================
  // ⚠️ RUNTIME CONFIGURATION
  // ============================================================================
  // Las variables de entorno (URLs, auth, etc.) NO se hornean en build time.
  // Se inyectan en runtime desde app/layout.tsx → window.__RUNTIME_ENV__
  // Esto permite reutilizar la misma imagen Docker en DEV, QA y PROD sin rebuild.
  // ============================================================================

  // Requerido para el Dockerfile multi-etapa (standalone bundle)
  output: 'standalone',

  // Fijar el root de tracing al directorio del portal para evitar el warning
  // de múltiples lockfiles cuando hay un package-lock.json en directorios padre.
  outputFileTracingRoot: require('path').join(__dirname),

  // ID de build único para evitar colisiones de caché entre deploys
  generateBuildId: async () => `${Date.now()}-${Math.random().toString(36).substring(7)}`,
}

module.exports = nextConfig
