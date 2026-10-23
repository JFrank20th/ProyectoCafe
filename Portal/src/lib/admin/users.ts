import { normalizeRoleName } from '@/lib/auth/roles';

export type UsuarioEstado = 0 | 1 | 2 | 3 | 4;

export type UsuarioListItem = {
  usuarioId: number;
  email: string;
  name: string;
  estado: UsuarioEstado;
  creadoEn: string;
  ultimoLoginEn?: string | null;
  roles: string[];
};

export type RolEstado = 0 | 1 | 2;

export type RolListItem = {
  rolId: number;
  nombre: string;
  estado: RolEstado;
  descripcion?: string | null;
  permisosCount?: number;
};

export type ChipColor = 'default' | 'info' | 'success' | 'warning' | 'error';

export function estadoColor(estado: UsuarioEstado): ChipColor {
  switch (estado) {
    case 0:
    case 1:
      return 'info';
    case 2:
      return 'success';
    case 3:
      return 'error';
    default:
      return 'default';
  }
}

export function formatDateTime(locale: string, value?: string | null): string {
  if (!value) return '—';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return value;
  return new Intl.DateTimeFormat(locale, { dateStyle: 'short', timeStyle: 'short' }).format(date);
}

/**
 * Returns the names of the roles that can currently be assigned. The backend
 * supports arbitrary role names, so this list is always built from its response
 * instead of a hardcoded catalog.
 */
export function activeRoleNames(roles: readonly RolListItem[]): string[] {
  const seen = new Set<string>();
  const names: string[] = [];

  for (const role of roles) {
    if (role.estado !== 0) continue;

    const name = (role.nombre ?? '').trim();
    if (!name) continue;

    const key = normalizeRoleName(name);
    if (seen.has(key)) continue;

    seen.add(key);
    names.push(name);
  }

  return names.sort((a, b) => a.localeCompare(b));
}
