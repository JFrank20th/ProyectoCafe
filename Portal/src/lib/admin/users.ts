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

export function activeRoleNames(roles: readonly RolListItem[]): string[] {
  return roles
    .filter((role) => role.estado === 0)
    .map((role) => role.nombre)
    .sort((a, b) => a.localeCompare(b));
}
