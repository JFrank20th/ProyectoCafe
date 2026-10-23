/**
 * Helpers for dynamic roles.
 *
 * Roles are defined in the backend and may be renamed or created at runtime,
 * so the frontend must never assume a closed list (for example `Admin`/`User`).
 * Authorization always relies on permissions; roles are only used to filter
 * optional UI affordances.
 */
export function normalizeRoleName(value: string): string {
  return value.trim().toLowerCase();
}

/**
 * Trims role names, drops empty values and removes case-insensitive duplicates
 * while preserving the original casing and order of the first occurrence.
 */
export function normalizeRoleNames(values: readonly string[] | null | undefined): string[] {
  const seen = new Set<string>();
  const roles: string[] = [];

  for (const value of values ?? []) {
    if (typeof value !== 'string') continue;

    const role = value.trim();
    if (!role) continue;

    const key = normalizeRoleName(role);
    if (seen.has(key)) continue;

    seen.add(key);
    roles.push(role);
  }

  return roles;
}

export function hasRole(userRoles: readonly string[] | null | undefined, role: string): boolean {
  const key = normalizeRoleName(role);
  if (!key) return false;

  return (userRoles ?? []).some((candidate) => normalizeRoleName(candidate) === key);
}

export function hasAnyRole(
  userRoles: readonly string[] | null | undefined,
  requiredRoles: readonly string[] | null | undefined,
): boolean {
  return (requiredRoles ?? []).some((role) => hasRole(userRoles, role));
}
