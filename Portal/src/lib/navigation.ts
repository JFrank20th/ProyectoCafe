import type { Permission, Role } from '@/lib/auth/types';

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
    items: [{ labelKey: 'dashboard', href: '/dashboard' }],
  },
  {
    labelKey: 'sales',
    items: [
      { labelKey: 'clientes', href: '/ventas/clientes', permissions: ['perm:Ventas.Clientes:Ver'] },
      { labelKey: 'tiposVenta', href: '/ventas/tipos-venta', permissions: ['perm:Ventas.TiposVenta:Ver'] },
      { labelKey: 'ventas', href: '/ventas', permissions: ['perm:Ventas.Ventas:Ver'] },
    ],
  },
  {
    labelKey: 'production',
    items: [
      {
        labelKey: 'campo',
        items: [
          { labelKey: 'fincas', href: '/produccion/fincas', permissions: ['perm:Produccion.Fincas:Ver'] },
          { labelKey: 'parcelas', href: '/produccion/cultivos', permissions: ['perm:Produccion.Cultivos:Ver'] },
          { labelKey: 'operacionesProduccion', href: '/produccion/operaciones-agricolas', permissions: ['perm:Produccion.OperacionesAgricolas:Ver'] },
        ],
      },
      { labelKey: 'insumos', href: '/produccion/insumos', permissions: ['perm:Inventario.Insumos:Ver'] },
    ],
  },
  {
    labelKey: 'accounting',
    items: [
      { labelKey: 'balance', href: '/contabilidad/balance' },
      { labelKey: 'costos', href: '/contabilidad/costos', permissions: ['perm:Contabilidad.Costos:Ver'] },
      { labelKey: 'gastos', href: '/contabilidad/gastos', permissions: ['perm:Contabilidad.Gastos:Ver'] },
      { labelKey: 'pasivos', href: '/contabilidad/pasivos', permissions: ['perm:Contabilidad.Pasivos:Ver'] },
    ],
  },
  {
    labelKey: 'inventory',
    items: [
      { labelKey: 'procesosInventario', href: '/inventario/procesos', permissions: ['perm:Inventario.Procesos:Ver'] },
      { labelKey: 'lotes', href: '/inventario/lotes', permissions: ['perm:Inventario.Lotes:Ver'] },
      { labelKey: 'productos', href: '/inventario/productos', permissions: ['perm:Inventario.Productos:Ver'] },
      {
        labelKey: 'unidadesMedida',
        href: '/inventario/unidades-medida',
        permissions: ['perm:Inventario.UnidadesMedida:Ver'],
      },
    ],
  },
  {
    labelKey: 'purchases',
    items: [
      { labelKey: 'proveedores', href: '/compras/proveedores', permissions: ['perm:Compras.Proveedores:Ver'] },
      { labelKey: 'recepcionesCafe', href: '/compras/recepciones-cafe', permissions: ['perm:Compras.RecepcionesCafe:Ver'] },
    ],
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

function normalizePermission(value: string) {
  return value.trim().toLowerCase();
}

export function canSeeItem(userRoles: Role[], permissionSet: ReadonlySet<string>, item: NavItem) {
  if (item.roles && item.roles.length > 0) {
    const ok = item.roles.some((role) => userRoles.includes(role));
    if (!ok) return false;
  }

  if (item.permissions && item.permissions.length > 0) {
    const ok = item.permissions.some((p) => permissionSet.has(normalizePermission(p)));
    if (!ok) return false;
  }

  return true;
}

export function filterVisibleNavItems(userRoles: Role[], userPermissions: Permission[], items: NavItem[]): NavItem[] {
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
