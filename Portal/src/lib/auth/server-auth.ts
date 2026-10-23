import { cookies } from 'next/headers';
import { redirect } from 'next/navigation';

import { createPermissionSet, hasModuleAction } from '@/lib/auth/permissions';
import { AUTH_COOKIE_NAME, fetchSessionFromCookie, type ServerSession } from '@/lib/auth/server-session';
import { buildLoginRedirect, FORBIDDEN_ROUTE, HOME_ROUTE } from '@/lib/routes';

/**
 * Recupera la sesión validada por la API. Uso exclusivo en Server Components,
 * Server Actions y Route Handlers de Node.js.
 */
export async function getServerSession(): Promise<ServerSession | null> {
  const cookieStore = await cookies();
  const token = cookieStore.get(AUTH_COOKIE_NAME)?.value;

  return fetchSessionFromCookie(token);
}

export async function requireServerSession(returnUrl: string = HOME_ROUTE): Promise<ServerSession> {
  const session = await getServerSession();

  if (!session) {
    redirect(buildLoginRedirect(returnUrl));
  }

  return session;
}

type ServerPermissionOptions = {
  returnUrl?: string;
  forbiddenUrl?: string;
};

/**
 * Guard para páginas y operaciones de servidor que exige sesión + permiso.
 * No sustituye la autorización de la API: es una capa adicional para no
 * renderizar ni ejecutar acciones no autorizadas.
 */
export async function requireServerPermission(
  modulo: string,
  accion: string,
  options: ServerPermissionOptions = {},
): Promise<ServerSession> {
  const session = await requireServerSession(options.returnUrl ?? HOME_ROUTE);
  const permissionSet = createPermissionSet(session.user.permissions);

  if (!hasModuleAction(permissionSet, modulo, accion)) {
    redirect(options.forbiddenUrl ?? FORBIDDEN_ROUTE);
  }

  return session;
}
