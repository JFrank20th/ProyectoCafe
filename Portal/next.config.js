/** @type {import('next').NextConfig} */
const nextConfig = {
  reactStrictMode: true,

  experimental: {
    // Work around a known Next 15 dev-only RSC manifest issue on Windows.
    devtoolSegmentExplorer: false,
  },

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

  // Acelerar builds en CI/Docker (el chequeo se hace en local/CI por separado)
  eslint:     { ignoreDuringBuilds: true },
  typescript: { ignoreBuildErrors:  true },

  // ID de build único para evitar colisiones de caché entre deploys
  generateBuildId: async () => `${Date.now()}-${Math.random().toString(36).substring(7)}`,

  images: {
    remotePatterns: [
      {
        protocol: 'https',
        hostname: '**',
      },
      {
        protocol: 'http',
        hostname: 'localhost',
      },
    ],
  },

  // Allow external images
  async headers() {
    return [
      {
        source: '/api/:path*',
        headers: [
          { key: 'Access-Control-Allow-Credentials', value: 'true' },
          { key: 'Access-Control-Allow-Origin', value: '*' },
          { key: 'Access-Control-Allow-Methods', value: 'GET,DELETE,PATCH,POST,PUT' },
          { key: 'Access-Control-Allow-Headers', value: 'X-CSRF-Token, X-Requested-With, Accept, Accept-Version, Content-Length, Content-MD5, Content-Type, Date, X-Api-Version' },
        ]
      }
    ]
  }
}

module.exports = nextConfig
