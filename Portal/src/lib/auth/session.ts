import { readLocalStorageJson, writeLocalStorageJson } from '@/lib/storage';
import type { Session } from '@/lib/auth/types';

const SESSION_KEY = 'pc_session_v1';

export function readSession(): Session | null {
  return readLocalStorageJson<Session>(SESSION_KEY);
}

export function writeSession(session: Session | null) {
  writeLocalStorageJson(SESSION_KEY, session);
}
