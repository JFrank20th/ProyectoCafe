import type { Permission, Role } from '@/lib/auth/types';
import { normalizePermission } from '@/lib/auth/permissions';
import { hasAnyRole } from '@/lib/auth/roles';
import { DASHBOARD_ROUTE } from '@/lib/routes';

export type NavItem = {
  labelKey: string;
  href?: string;
  items?: NavItem[];
  roles?: Role[];
  permissions?: Permission[];
};

export type NavSection = {
  labelKey: string;
  items: NavItem[];
};

export const NAV: NavSection[] = [
  {
    labelKey: 'main',
    items: [{ labelKey: 'dashboard', href: DASHBOARD_ROUTE }],
  },
  {
    labelKey: 'administration',
    items: [
      {
        labelKey: 'seguridad',
        items: [
          { labelKey: 'usuarios', href: '/administracion/usuarios', permissions: ['perm:Administracion.Usuarios:Ver'] },
          { labelKey: 'roles', href: '/administracion/roles', permissions: ['perm:Administracion.Roles:Ver'] },
        ],
      },
    ],
  },
];

export function canSeeItem(
  userRoles: readonly Role[],
  permissionSet: ReadonlySet<string>,
  item: NavItem,
) {
  if (item.roles && item.roles.length > 0) {
    // Role names are dynamic and compared case-insensitively, matching the backend.
    if (!hasAnyRole(userRoles, item.roles)) return false;
  }

  if (item.permissions && item.permissions.length > 0) {
    const ok = item.permissions.some((p) => permissionSet.has(normalizePermission(p)));
    if (!ok) return false;
  }

  return true;
}

export function filterVisibleNavItems(
  userRoles: readonly Role[],
  userPermissions: readonly Permission[],
  items: NavItem[],
): NavItem[] {
  const permissionSet = new Set((userPermissions ?? []).map(normalizePermission));

  const walk = (list: NavItem[]): NavItem[] => {
    return list
      .map((item) => {
        if (item.items && item.items.length > 0) {
          const visibleChildren = walk(item.items);
          if (visibleChildren.length === 0) return null;
          if (!canSeeItem(userRoles, permissionSet, item)) return null;
          return { ...item, items: visibleChildren };
        }

        if (!item.href) return null;
        if (!canSeeItem(userRoles, permissionSet, item)) return null;
        return item;
      })
      .filter((x): x is NavItem => Boolean(x));
  };

  return walk(items);
}
