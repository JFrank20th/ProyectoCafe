# ProyectoCafe Portal

Portal en Next.js (App Router) alineado a la estructura y tooling del Portal de Next-RPS.

## Desarrollo

- `npm install`
- `npm run dev`

## Variables de entorno

Usa `.env.local` (no commitear). Ver `.env.example`.

## Imagen Docker

`Dockerfile` es multi-etapa y usa el contexto `Portal`:

- Build (`node:22-slim`): `npm ci` y `npm run build` con `output: 'standalone'`.
  El lint y el typecheck se ejecutan en el build (no se desactivan para Docker).
- Runner (`node:22-slim`): usuario no-root `nextjs` (UID 1001).
  Copia `.next/standalone`, `.next/static` y `public/`, escucha en
  `0.0.0.0:3000` (`PORT=3000`, `HOSTNAME=0.0.0.0`) y arranca con `node server.js`.
- `public/.gitkeep` mantiene el directorio `public/` en el repo: el `COPY` del
  runner falla si no existe.
- `.dockerignore` excluye `node_modules`, `.next`, `.env`, `.env.*`
  (excepto `.env.example`), `coverage` y `test-results`; `.env.local` nunca
  forma parte de la imagen.

### Build

```powershell
Set-Location -Path "c:\ProyectoCafe"
docker build -t pc-portal:local Portal
```

### Run

```powershell
docker run --rm -p 3000:3000 -e PROYECTOCAFE_API_URL=http://localhost:8080 pc-portal:local

curl.exe -i http://localhost:3000/login
# HTTP/1.1 200 OK
```

### Variables de runtime

Las variables `NEXT_PUBLIC_*` las quema Next.js en build time; por eso la URL de
la API se inyecta en runtime con `PROYECTOCAFE_API_URL`
(ver `src/lib/runtime-config.ts` y `src/app/layout.tsx`). La misma imagen sirve
para DEV, QA y PROD cambiando solo las variables del contenedor.

`PROYECTOCAFE_API_URL` debe ser alcanzable por **dos** consumidores:

1. El **navegador**: `app/layout.tsx` la publica en `window.__RUNTIME_ENV__` y
   el cliente llama directamente a la API.
2. El **servidor del Portal**: el middleware y los Server Components validan la
   sesión contra `/api/v1/auth/me`.

No uses un host interno de Docker (por ejemplo `http://api:8080`) si el
navegador no lo resuelve: ambos deben poder llegar a la misma URL.
