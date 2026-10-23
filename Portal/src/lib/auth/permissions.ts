export const PERMISSION_PREFIX = 'perm:';

/**
 * Normaliza una clave de permiso para comparaciones case-insensitive.
 * El backend expone los permisos como `perm:Modulo:Accion`.
 */
export function normalizePermission(value: string): string {
  return value.trim().toLowerCase();
}

export function buildPermission(modulo: string, accion: string): string {
  return `${PERMISSION_PREFIX}${modulo.trim()}:${accion.trim()}`;
}

export function createPermissionSet(permissions: readonly string[] | null | undefined): ReadonlySet<string> {
  return new Set((permissions ?? []).map(normalizePermission));
}

export function hasPermission(
  permissionSet: ReadonlySet<string>,
  permission: string,
): boolean {
  return permissionSet.has(normalizePermission(permission));
}

export function hasModuleAction(
  permissionSet: ReadonlySet<string>,
  modulo: string,
  accion: string,
): boolean {
  return hasPermission(permissionSet, buildPermission(modulo, accion));
}
