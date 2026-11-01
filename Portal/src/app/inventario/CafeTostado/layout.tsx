import type { ReactNode } from 'react';

import { requireServerPermission } from '@/lib/auth/server-auth';
import { INVENTARIO_CAFE_ROUTE } from '@/lib/routes';

export default async function InventarioCafeLayout({ children }: { children: ReactNode }) {
  await requireServerPermission('Inventario.Lotes', 'Ver', {
    returnUrl: INVENTARIO_CAFE_ROUTE,
  });

  return children;
}
