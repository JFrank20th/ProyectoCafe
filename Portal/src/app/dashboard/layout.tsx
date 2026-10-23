import type { ReactNode } from 'react';

import { requireServerSession } from '@/lib/auth/server-auth';
import { DASHBOARD_ROUTE } from '@/lib/routes';

export default async function DashboardLayout({ children }: { children: ReactNode }) {
  await requireServerSession(DASHBOARD_ROUTE);
  return children;
}
