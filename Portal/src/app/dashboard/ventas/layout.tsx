import type { ReactNode } from 'react';

import { requireServerPermission } from '@/lib/auth/server-auth';
import { VENTAS_ROUTE } from '@/lib/routes';

export default async function VentasLayout({ children }: { children: ReactNode }) {
  await requireServerPermission('Ventas.Ventas', 'Ver', {
    returnUrl: VENTAS_ROUTE,
  });

  return children;
}