export type Role = 'Admin' | 'User';

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
