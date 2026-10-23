import { NextResponse, type NextRequest } from 'next/server';

import { createPermissionSet } from '@/lib/auth/permissions';
import { getRouteAccess, hasRoutePermission } from '@/lib/auth/route-access';
import { AUTH_COOKIE_NAME, fetchSessionFromCookie } from '@/lib/auth/server-session';
import { buildLoginRedirect, FORBIDDEN_ROUTE, isApiRoute } from '@/lib/routes';

function loginRedirect(request: NextRequest, clearCookie: boolean): NextResponse {
  const current = `${request.nextUrl.pathname}${request.nextUrl.search}`;
  const response = NextResponse.redirect(new URL(buildLoginRedirect(current), request.url));

  if (clearCookie) {
    response.cookies.delete(AUTH_COOKIE_NAME);
  }

  return response;
}

function apiError(
  status: number,
  code: string,
  message: string,
  clearCookie = false,
): NextResponse {
  const response = NextResponse.json(
    { error: { code, message } },
    { status, headers: { 'cache-control': 'no-store' } },
  );

  if (clearCookie) {
    response.cookies.delete(AUTH_COOKIE_NAME);
  }

  return response;
}

/**
 * Protección de rutas en el servidor (Next.js 15).
 *
 * - Páginas públicas: login y registro.
 * - Resto de páginas: requieren una sesión válida contra la API.
 * - Administración: requieren además el permiso de lectura correspondiente.
 * - API del Portal: responden 401/403 en JSON (nunca redirigen al login).
 *
 * El guard del cliente (AppShell/SessionGuard) se conserva como complemento.
 */
export async function middleware(request: NextRequest) {
  const access = getRouteAccess(request.nextUrl.pathname);

  if (access.access === 'public') {
    return NextResponse.next();
  }

  const isApi = isApiRoute(request.nextUrl.pathname);
  const token = request.cookies.get(AUTH_COOKIE_NAME)?.value;

  if (!token) {
    return isApi
      ? apiError(401, 'UNAUTHORIZED', 'No autorizado.')
      : loginRedirect(request, false);
  }

  const session = await fetchSessionFromCookie(token);

  if (!session) {
    return isApi
      ? apiError(401, 'UNAUTHORIZED', 'No autorizado.', true)
      : loginRedirect(request, true);
  }

  if (!hasRoutePermission(createPermissionSet(session.user.permissions), access)) {
    return isApi
      ? apiError(403, 'FORBIDDEN', 'No tienes permisos.')
      : NextResponse.redirect(new URL(FORBIDDEN_ROUTE, request.url));
  }

  return NextResponse.next();
}

export const config = {
  // Node.js permite leer variables de entorno en runtime (Docker/QA/PROD).
  runtime: 'nodejs',
  matcher: [
    '/((?!_next/|favicon.ico|robots.txt|sitemap.xml|.*\\.(?:svg|png|jpg|jpeg|gif|webp|avif|ico|css|js|map|woff|woff2|ttf|otf|eot)$).*)',
  ],
};
