import { redirect } from 'next/navigation';

import { HOME_ROUTE } from '@/lib/routes';

export default function HomePage() {
  redirect(HOME_ROUTE);
}
