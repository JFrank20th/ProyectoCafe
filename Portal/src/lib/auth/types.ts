/**
 * Role names are dynamic: the backend allows creating, renaming and deactivating
 * roles at runtime. Keep the alias for readability, but do not narrow it to a
 * closed union. Permission checks remain the source of truth for authorization.
 */
export type Role = string;

export type Permission = string;

export type SessionUser = {
  email: string;
  name: string;
  roles: Role[];
  permissions: Permission[];
};

export type Session = {
  user: SessionUser;
  accessToken: string | null;
};
