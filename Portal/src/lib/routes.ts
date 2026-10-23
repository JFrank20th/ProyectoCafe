export const HOME_ROUTE = '/dashboard';
export const DASHBOARD_ROUTE = HOME_ROUTE;
export const LOGIN_ROUTE = '/login';
export const REGISTER_ROUTE = '/register';
export const FORBIDDEN_ROUTE = '/acceso-denegado';

export const PUBLIC_ROUTES = [LOGIN_ROUTE, REGISTER_ROUTE] as const;

export const RETURN_URL_PARAM = 'returnUrl';

/**
 * Normaliza una ruta para comparaciones estables:
 * - elimina query string y barras finales
 * - garantiza una barra inicial
 * - colapsa barras duplicadas
 */
export function normalizePathname(pathname: string | null | undefined): string {
  if (!pathname) return '/';

  const withoutQuery = pathname.split('?')[0] ?? '';
  const withLeadingSlash = withoutQuery.startsWith('/') ? withoutQuery : `/${withoutQuery}`;
  const collapsed = withLeadingSlash.replace(/\/{2,}/g, '/');
  const trimmed = collapsed.replace(/\/+$/, '');

  return trimmed || '/';
}

export function isPublicRoute(pathname: string | null | undefined): boolean {
  if (!pathname) return false;

  const normalized = normalizePathname(pathname);
  return PUBLIC_ROUTES.some((route) => normalized === route);
}

export function isApiRoute(pathname: string | null | undefined): boolean {
  if (!pathname) return false;

  const normalized = normalizePathname(pathname);
  return normalized === '/api' || normalized.startsWith('/api/');
}

/**
 * Construye la URL de login preservando la ruta actual como `returnUrl`.
 */
export function buildLoginRedirect(currentPath: string): string {
  const path = currentPath.startsWith('/') ? currentPath : `/${currentPath}`;
  return `${LOGIN_ROUTE}?${RETURN_URL_PARAM}=${encodeURIComponent(path)}`;
}

/**
 * Valida un `returnUrl` para evitar redirecciones abiertas y bucles de login.
 * Solo se aceptan rutas internas que no sean públicas.
 */
export function sanitizeReturnUrl(
  value: string | null | undefined,
  fallback: string = HOME_ROUTE,
): string {
  if (!value) return fallback;

  const candidate = value.trim();
  if (!candidate.startsWith('/') || candidate.startsWith('//')) return fallback;
  if (candidate.includes('\\') || /[\u0000-\u001F]/.test(candidate)) return fallback;

  try {
    const parsed = new URL(candidate, 'http://portal.local');
    if (parsed.origin !== 'http://portal.local') return fallback;
    if (isPublicRoute(parsed.pathname)) return fallback;

    return `${parsed.pathname}${parsed.search}${parsed.hash}`;
  } catch {
    return fallback;
  }
}
