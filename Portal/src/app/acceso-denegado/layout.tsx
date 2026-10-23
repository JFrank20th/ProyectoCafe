import type { ReactNode } from 'react';

import { requireServerSession } from '@/lib/auth/server-auth';
import { FORBIDDEN_ROUTE } from '@/lib/routes';

export default async function ForbiddenLayout({ children }: { children: ReactNode }) {
  await requireServerSession(FORBIDDEN_ROUTE);
  return children;
}
