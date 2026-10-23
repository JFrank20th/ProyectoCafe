import { NextResponse, type NextRequest } from 'next/server';

import { createPermissionSet, hasModuleAction } from '@/lib/auth/permissions';
import { AUTH_COOKIE_NAME, fetchSessionFromCookie, type ServerSession } from '@/lib/auth/server-session';

type ApiGuardSuccess = {
  session: ServerSession;
  response: null;
};

type ApiGuardFailure = {
  session: null;
  response: NextResponse;
};

export type ApiGuardResult = ApiGuardSuccess | ApiGuardFailure;

function unauthorized(): ApiGuardFailure {
  return {
    session: null,
    response: NextResponse.json(
      { error: { code: 'UNAUTHORIZED', message: 'No autorizado.' } },
      { status: 401, headers: { 'cache-control': 'no-store' } },
    ),
  };
}

function forbidden(): ApiGuardFailure {
  return {
    session: null,
    response: NextResponse.json(
      { error: { code: 'FORBIDDEN', message: 'No tienes permisos.' } },
      { status: 403, headers: { 'cache-control': 'no-store' } },
    ),
  };
}

/**
 * Guard reutilizable para Route Handlers y operaciones de API del Portal.
 * Valida la cookie HttpOnly contra la API antes de ejecutar la operación.
 */
export async function requireApiSession(request: NextRequest): Promise<ApiGuardResult> {
  const token = request.cookies.get(AUTH_COOKIE_NAME)?.value;
  if (!token) return unauthorized();

  const session = await fetchSessionFromCookie(token);
  if (!session) return unauthorized();

  return { session, response: null };
}

/**
 * Variante con autorización por rol/permiso. El backend sigue siendo la
 * autoridad final; este guard evita invocar operaciones no permitidas.
 */
export async function requireApiPermission(
  request: NextRequest,
  modulo: string,
  accion: string,
): Promise<ApiGuardResult> {
  const result = await requireApiSession(request);
  if (result.response) return result;

  const permissionSet = createPermissionSet(result.session.user.permissions);
  if (!hasModuleAction(permissionSet, modulo, accion)) {
    return forbidden();
  }

  return result;
}
