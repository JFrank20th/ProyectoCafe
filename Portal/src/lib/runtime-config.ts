/**
 * Runtime Configuration
 *
 * Lee variables de entorno en RUNTIME (no en build time).
 * - Cliente: lee de window.__RUNTIME_ENV__ (inyectado por app/layout.tsx)
 * - Servidor: lee de process.env
 */

declare global {
  interface Window {
    __RUNTIME_ENV__?: {
      API_URL: string;
      APP_NAME: string;
      APP_VERSION: string;
    };
  }
}

export type RuntimeConfig = {
  API_URL: string;
  APP_NAME: string;
  APP_VERSION: string;
};

export function getRuntimeConfig(): RuntimeConfig {
  if (typeof window !== 'undefined' && window.__RUNTIME_ENV__) {
    return window.__RUNTIME_ENV__;
  }

  return {
    API_URL:
      process.env['PROYECTOCAFE_API_URL'] ||
      process.env['NEXT_PUBLIC_PROYECTOCAFE_API_URL'] ||
      'http://localhost:5000',
    APP_NAME:
      process.env['APP_NAME'] || process.env['NEXT_PUBLIC_APP_NAME'] || 'ProyectoCafe',
    APP_VERSION:
      process.env['NEXT_PUBLIC_APP_VERSION'] || process.env['APP_VERSION'] || '0.1.0',
  };
}
