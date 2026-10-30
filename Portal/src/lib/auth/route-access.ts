import { buildPermission, createPermissionSet, hasPermission } from '@/lib/auth/permissions';
import { INVENTARIO_CAFE_ROUTE, isPublicRoute, normalizePathname } from '@/lib/routes';

export type RouteAccess =
  | { access: 'public' }
  | { access: 'protected'; permission?: string; permissions?: string[] };

type PermissionRoute = {
  path: string;
  permission: string;
  permissions?: string[];
};

/**
 * Rutas de página que además de requerir sesión exigen un permiso concreto.
 * El backend vuelve a validar el permiso en cada operación: esto solo evita
 * renderizar pantallas que el usuario no puede usar.
 */
export const PERMISSION_ROUTES: readonly PermissionRoute[] = [
  {
    path: '/administracion/usuarios',
    permission: buildPermission('Administracion.Usuarios', 'Ver'),
  },
  {
    path: '/administracion/roles',
    permission: buildPermission('Administracion.Roles', 'Ver'),
  },
  {
    path: INVENTARIO_CAFE_ROUTE,
    permission: buildPermission('Inventario.Lotes', 'Ver'),
    permissions: [
      buildPermission('Inventario.Lotes', 'Ver'),
      buildPermission('Inventario.Presentaciones', 'Ver'),
      buildPermission('Inventario.TiposCafe', 'Ver'),
    ],
  },
];

function matchesRoute(pathname: string, route: string): boolean {
  return pathname === route || pathname.startsWith(`${route}/`);
}

/**
 * Política de acceso del servidor (deny-by-default):
 * - login y registro son públicos
 * - el resto de páginas requiere sesión
 * - las pantallas de administración requieren además el permiso de lectura
 */
export function getRouteAccess(pathname: string | null | undefined): RouteAccess {
  const normalized = normalizePathname(pathname);

  if (isPublicRoute(normalized)) {
    return { access: 'public' };
  }

  const permissionRoute = PERMISSION_ROUTES.find((route) => matchesRoute(normalized, route.path));
  if (permissionRoute) {
    return permissionRoute.permissions
      ? {
          access: 'protected',
          permission: permissionRoute.permission,
          permissions: permissionRoute.permissions,
        }
      : { access: 'protected', permission: permissionRoute.permission };
  }

  return { access: 'protected' };
}

export function hasRoutePermission(
  permissionSet: ReadonlySet<string>,
  access: RouteAccess,
): boolean {
  if (access.access === 'public') return true;
  if (access.permissions && access.permissions.length > 0) {
    return access.permissions.some((permission) => hasPermission(permissionSet, permission));
  }
  if (!access.permission) return true;

  return hasPermission(permissionSet, access.permission);
}

export function canAccessRoute(
  permissions: readonly string[] | null | undefined,
  pathname: string,
): boolean {
  return hasRoutePermission(createPermissionSet(permissions), getRouteAccess(pathname));
}
