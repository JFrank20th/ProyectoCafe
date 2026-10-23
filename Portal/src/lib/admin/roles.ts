import type { RolEstado, RolListItem } from '@/lib/admin/users';

export type PermissionDto = {
  modulo: string;
  accion: string;
};

export type PermissionCatalogItem = {
  modulo: string;
  acciones: string[];
};

/**
 * Backend limits for role fields. Keep the UI validations aligned with
 * `AdminEndpoints` so custom role names fail locally with a clear message
 * instead of reaching the API and returning a generic validation error.
 */
export const ROLE_NAME_MAX_LENGTH = 50;
export const ROLE_DESCRIPTION_MAX_LENGTH = 250;

export type RoleValidationError = 'required' | 'maxLength';

export type RoleValidationErrors = {
  nombre?: RoleValidationError;
  descripcion?: RoleValidationError;
};

export function validateRoleInput(input: {
  nombre: string;
  descripcion: string;
}): RoleValidationErrors {
  const errors: RoleValidationErrors = {};

  const nombre = (input.nombre ?? '').trim();
  if (!nombre) {
    errors.nombre = 'required';
  } else if (nombre.length > ROLE_NAME_MAX_LENGTH) {
    errors.nombre = 'maxLength';
  }

  const descripcion = (input.descripcion ?? '').trim();
  if (descripcion.length > ROLE_DESCRIPTION_MAX_LENGTH) {
    errors.descripcion = 'maxLength';
  }

  return errors;
}

export function hasRoleValidationErrors(errors: RoleValidationErrors): boolean {
  return Boolean(errors.nombre || errors.descripcion);
}

export function normalizePermissionKey(modulo: string, accion: string): string {
  return `${modulo.trim()}:${accion.trim()}`.toLowerCase();
}

export function toPermissionMap(permissions: readonly PermissionDto[] | null | undefined): Record<string, PermissionDto> {
  const selected: Record<string, PermissionDto> = {};

  for (const permission of permissions ?? []) {
    const modulo = (permission.modulo ?? '').trim();
    const accion = (permission.accion ?? '').trim();
    if (!modulo || !accion) continue;

    const key = normalizePermissionKey(modulo, accion);
    if (!selected[key]) {
      selected[key] = { modulo, accion };
    }
  }

  return selected;
}

export function toPermissionPayload(selected: Record<string, PermissionDto>): PermissionDto[] {
  return Object.values(selected);
}

export function toggleSelectedPermission(
  selected: Record<string, PermissionDto>,
  modulo: string,
  accion: string,
): Record<string, PermissionDto> {
  const key = normalizePermissionKey(modulo, accion);
  if (selected[key]) {
    const next = { ...selected };
    delete next[key];
    return next;
  }

  return { ...selected, [key]: { modulo, accion } };
}

export function roleEstadoColor(estado: RolEstado): 'default' | 'success' | 'warning' | 'error' {
  switch (estado) {
    case 0:
      return 'success';
    case 1:
      return 'warning';
    default:
      return 'error';
  }
}

export function roleEstadoKind(estado: RolEstado): 'active' | 'inactive' | 'deleted' {
  switch (estado) {
    case 0:
      return 'active';
    case 1:
      return 'inactive';
    default:
      return 'deleted';
  }
}

export function buildRolePayload(input: {
  nombre: string;
  descripcion: string;
  estado: RolEstado;
}): { nombre: string; descripcion: string | null; estado: RolEstado } {
  const descripcion = input.descripcion.trim();
  return {
    nombre: input.nombre.trim(),
    descripcion: descripcion ? descripcion : null,
    estado: input.estado,
  };
}

export function isRoleVisible(role: RolListItem): boolean {
  return role.estado !== 2;
}
