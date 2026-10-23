export const HOME_ROUTE = '/dashboard';
export const DASHBOARD_ROUTE = HOME_ROUTE;
export const LOGIN_ROUTE = '/login';
export const REGISTER_ROUTE = '/register';

export const PUBLIC_ROUTES = [LOGIN_ROUTE, REGISTER_ROUTE] as const;

export function isPublicRoute(pathname: string | null | undefined): boolean {
  if (!pathname) return false;
  const normalized = pathname.split('?')[0]?.replace(/\/+$/, '') || '/';
  return PUBLIC_ROUTES.some((route) => normalized === route);
}

/**
 * Construye la URL de login preservando la ruta actual como `returnUrl`.
 */
export function buildLoginRedirect(currentPath: string): string {
  const path = currentPath.startsWith('/') ? currentPath : `/${currentPath}`;
  return `${LOGIN_ROUTE}?returnUrl=${encodeURIComponent(path)}`;
}
