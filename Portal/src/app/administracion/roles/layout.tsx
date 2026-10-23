import type { ReactNode } from 'react';

import { requireServerPermission } from '@/lib/auth/server-auth';

export default async function RolesLayout({ children }: { children: ReactNode }) {
  await requireServerPermission('Administracion.Roles', 'Ver', {
    returnUrl: '/administracion/roles',
  });

  return children;
}
