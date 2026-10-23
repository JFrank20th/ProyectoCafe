'use client';

import { useEffect, useMemo, useState } from 'react';

import { useLocale, useTranslations } from '@/hooks/useTranslations';
import { useAuth } from '@/lib/auth/AuthProvider';
import { normalizeRoleNames } from '@/lib/auth/roles';
import { apiFetch } from '@/lib/api/client';
import {
  activeRoleNames,
  estadoColor,
  formatDateTime,
  type RolListItem,
  type UsuarioListItem,
} from '@/lib/admin/users';

import Alert from '@mui/material/Alert';
import Button from '@mui/material/Button';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import CardHeader from '@mui/material/CardHeader';
import Checkbox from '@mui/material/Checkbox';
import Chip from '@mui/material/Chip';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogTitle from '@mui/material/DialogTitle';
import FormControl from '@mui/material/FormControl';
import InputLabel from '@mui/material/InputLabel';
import ListItemText from '@mui/material/ListItemText';
import MenuItem from '@mui/material/MenuItem';
import Select, { type SelectChangeEvent } from '@mui/material/Select';
import Stack from '@mui/material/Stack';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import Typography from '@mui/material/Typography';

type UserDto = UsuarioListItem;

type RoleDto = RolListItem;

export default function UsuariosPage() {
  const nav = useTranslations('navigation');
  const tc = useTranslations('common');
  const tf = useTranslations('fields');
  const tp = useTranslations('pages');
  const ta = useTranslations('admin');
  const locale = useLocale();

  const { has } = useAuth();
  const canViewUsers = has('Administracion.Usuarios', 'Ver');
  const canEditUsers = has('Administracion.Usuarios', 'Editar');
  const canDeleteUsers = has('Administracion.Usuarios', 'Eliminar');

  const [items, setItems] = useState<UserDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [busyId, setBusyId] = useState<number | null>(null);

  const [rolesOpen, setRolesOpen] = useState(false);
  const [rolesError, setRolesError] = useState<string | null>(null);
  const [rolesSaving, setRolesSaving] = useState(false);
  const [rolesCatalog, setRolesCatalog] = useState<string[]>([]);
  const [selectedUser, setSelectedUser] = useState<UserDto | null>(null);
  const [selectedRoles, setSelectedRoles] = useState<string[]>([]);

  const [deleteOpen, setDeleteOpen] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<UserDto | null>(null);

  const estadoLabelByValue = useMemo(() => {
    return {
      0: ta('userStatePendingActivation'),
      1: ta('userStatePendingActivation'),
      2: ta('userStateActive'),
      3: ta('userStateInactive'),
      4: ta('userStateDeleted'),
    } as const;
  }, [ta]);

  async function loadUsers() {
    if (!canViewUsers) return;

    setLoading(true);
    setError(null);
    try {
      const list = await apiFetch<UserDto[]>('/api/v1/admin/users');
      setItems(list);
    } catch (e) {
      setError(e instanceof Error ? e.message : tp('errorLoadingUsers'));
    } finally {
      setLoading(false);
    }
  }

  async function loadRolesCatalog() {
    // Roles are dynamic: reload the catalog each time the dialog opens so newly
    // created or deactivated roles are reflected without reloading the page.
    const list = await apiFetch<RoleDto[]>('/api/v1/admin/roles');
    setRolesCatalog(activeRoleNames(list));
  }

  useEffect(() => {
    void loadUsers();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [canViewUsers]);

  async function onActivate(user: UserDto) {
    if (!canEditUsers) return;
    setBusyId(user.usuarioId);
    setError(null);
    try {
      await apiFetch<void>(`/api/v1/admin/users/${user.usuarioId}/activate`, { method: 'POST' });
      await loadUsers();
    } catch (e) {
      setError(e instanceof Error ? e.message : tp('errorUpdatingUser'));
    } finally {
      setBusyId(null);
    }
  }

  async function onDeactivate(user: UserDto) {
    if (!canEditUsers) return;
    setBusyId(user.usuarioId);
    setError(null);
    try {
      await apiFetch<void>(`/api/v1/admin/users/${user.usuarioId}/deactivate`, { method: 'POST' });
      await loadUsers();
    } catch (e) {
      setError(e instanceof Error ? e.message : tp('errorUpdatingUser'));
    } finally {
      setBusyId(null);
    }
  }

  function openRolesDialog(user: UserDto) {
    if (!canEditUsers) return;
    setSelectedUser(user);
    setSelectedRoles(normalizeRoleNames(user.roles));
    setRolesError(null);
    setRolesOpen(true);
    void loadRolesCatalog().catch((e) => {
      setRolesError(e instanceof Error ? e.message : tp('errorLoadingRoles'));
    });
  }

  function closeRolesDialog() {
    if (rolesSaving) return;
    setRolesOpen(false);
    setSelectedUser(null);
    setSelectedRoles([]);
    setRolesError(null);
  }

  async function onSaveRoles() {
    if (!canEditUsers) return;
    if (!selectedUser) return;

    setRolesSaving(true);
    setRolesError(null);
    try {
      await apiFetch<void>(`/api/v1/admin/users/${selectedUser.usuarioId}/roles`, {
        method: 'PUT',
        body: JSON.stringify({ roles: normalizeRoleNames(selectedRoles) }),
      });
      setRolesOpen(false);
      setSelectedUser(null);
      setSelectedRoles([]);
      await loadUsers();
    } catch (e) {
      setRolesError(e instanceof Error ? e.message : tp('errorSavingRoles'));
    } finally {
      setRolesSaving(false);
    }
  }

  function openDeleteDialog(user: UserDto) {
    if (!canDeleteUsers) return;
    setDeleteTarget(user);
    setDeleteError(null);
    setDeleteOpen(true);
  }

  function closeDeleteDialog() {
    if (busyId) return;
    setDeleteOpen(false);
    setDeleteTarget(null);
    setDeleteError(null);
  }

  async function onConfirmDelete() {
    if (!canDeleteUsers) return;
    if (!deleteTarget) return;
    setBusyId(deleteTarget.usuarioId);
    setDeleteError(null);
    try {
      await apiFetch<void>(`/api/v1/admin/users/${deleteTarget.usuarioId}`, { method: 'DELETE' });
      setDeleteOpen(false);
      setDeleteTarget(null);
      await loadUsers();
    } catch (e) {
      setDeleteError(e instanceof Error ? e.message : tp('errorDeletingUser'));
    } finally {
      setBusyId(null);
    }
  }

  if (!canViewUsers) {
    return (
      <Stack spacing={1}>
        <Typography variant="h4">{nav('usuarios')}</Typography>
        <Typography variant="body2" color="text.secondary">
          {tp('noPermission')}
        </Typography>
      </Stack>
    );
  }

  return (
    <Stack spacing={2}>
      <Stack direction="row" alignItems="center" justifyContent="space-between" spacing={2}>
        <Typography variant="h4">{nav('usuarios')}</Typography>
        <Button variant="outlined" onClick={() => void loadUsers()} disabled={loading}>
          {tc('refresh')}
        </Button>
      </Stack>

      {error ? <Alert severity="error">{error}</Alert> : null}

      <Card variant="outlined">
        <CardHeader title={tc('listTitle')} />
        <CardContent sx={{ pt: 0 }}>
          {loading ? (
            <Typography variant="body2" color="text.secondary">
              {tc('loading')}
            </Typography>
          ) : (
            <TableContainer>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>{tf('email')}</TableCell>
                    <TableCell>{tf('name')}</TableCell>
                    <TableCell>{tf('status')}</TableCell>
                    <TableCell>{tf('createdAt')}</TableCell>
                    <TableCell>{tf('lastLogin')}</TableCell>
                    <TableCell>{tf('roles')}</TableCell>
                    <TableCell align="right">{tf('actions')}</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {items.map((u) => {
                    const isBusy = busyId === u.usuarioId;
                    const canActivate = u.estado !== 4;

                    return (
                      <TableRow key={u.usuarioId} hover>
                        <TableCell>{u.email}</TableCell>
                        <TableCell>{u.name}</TableCell>
                        <TableCell>
                          <Chip
                            size="small"
                            color={estadoColor(u.estado)}
                            label={estadoLabelByValue[u.estado] ?? String(u.estado)}
                          />
                        </TableCell>
                        <TableCell>{formatDateTime(locale, u.creadoEn)}</TableCell>
                        <TableCell>{formatDateTime(locale, u.ultimoLoginEn)}</TableCell>
                        <TableCell>
                          <Stack direction="row" spacing={0.5} useFlexGap flexWrap="wrap">
                            {(u.roles ?? []).length > 0 ? (
                              u.roles.map((r) => <Chip key={r} label={r} size="small" variant="outlined" />)
                            ) : (
                              <Typography variant="body2" color="text.secondary">
                                —
                              </Typography>
                            )}
                          </Stack>
                        </TableCell>
                        <TableCell align="right">
                          <Stack direction="row" spacing={1} justifyContent="flex-end">
                            <Button
                              size="small"
                              variant="outlined"
                              onClick={() => openRolesDialog(u)}
                              disabled={isBusy || !canEditUsers}
                            >
                              {ta('editRoles')}
                            </Button>

                            {u.estado === 2 ? (
                              <Button
                                size="small"
                                color="warning"
                                variant="contained"
                                onClick={() => void onDeactivate(u)}
                                disabled={isBusy || !canEditUsers}
                              >
                                {ta('deactivate')}
                              </Button>
                            ) : (
                              <Button
                                size="small"
                                color="success"
                                variant="contained"
                                onClick={() => void onActivate(u)}
                                disabled={isBusy || !canActivate || !canEditUsers}
                              >
                                {ta('activate')}
                              </Button>
                            )}

                            <Button
                              size="small"
                              color="error"
                              variant="outlined"
                              onClick={() => openDeleteDialog(u)}
                              disabled={isBusy || !canDeleteUsers}
                            >
                              {tc('delete')}
                            </Button>
                          </Stack>
                        </TableCell>
                      </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
            </TableContainer>
          )}
        </CardContent>
      </Card>

      <Dialog open={rolesOpen} onClose={closeRolesDialog} fullWidth maxWidth="sm">
        <DialogTitle>
          {selectedUser ? ta('rolesDialogTitle', { email: selectedUser.email }) : ta('rolesDialogTitleEmpty')}
        </DialogTitle>
        <DialogContent dividers>
          <Stack spacing={2} sx={{ pt: 1 }}>
            {rolesError ? <Alert severity="error">{rolesError}</Alert> : null}

            <FormControl fullWidth>
              <InputLabel id="roles-label">{tf('roles')}</InputLabel>
              <Select<string[]>
                labelId="roles-label"
                label={tf('roles')}
                multiple
                value={selectedRoles}
                disabled={!canEditUsers}
                onChange={(e: SelectChangeEvent<string[]>) => {
                  const value = e.target.value;
                  setSelectedRoles(typeof value === 'string' ? value.split(',') : value);
                }}
                renderValue={(selected) => (selected.length ? selected.join(', ') : '—')}
              >
                {rolesCatalog.map((roleName) => (
                  <MenuItem key={roleName} value={roleName}>
                    <Checkbox checked={selectedRoles.includes(roleName)} />
                    <ListItemText primary={roleName} />
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={closeRolesDialog} disabled={rolesSaving}>
            {tc('cancel')}
          </Button>
          <Button
            onClick={() => void onSaveRoles()}
            variant="contained"
            disabled={rolesSaving || !selectedUser || !canEditUsers}
          >
            {tc('save')}
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={deleteOpen} onClose={closeDeleteDialog} fullWidth maxWidth="xs">
        <DialogTitle>{ta('confirmDeleteUserTitle')}</DialogTitle>
        <DialogContent dividers>
          <Stack spacing={2} sx={{ pt: 1 }}>
            {deleteError ? <Alert severity="error">{deleteError}</Alert> : null}
            <Typography variant="body2">
              {deleteTarget ? ta('confirmDeleteUserBody', { email: deleteTarget.email }) : null}
            </Typography>
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={closeDeleteDialog} disabled={busyId !== null}>
            {tc('cancel')}
          </Button>
          <Button
            onClick={() => void onConfirmDelete()}
            variant="contained"
            color="error"
            disabled={busyId !== null || !deleteTarget || !canDeleteUsers}
          >
            {tc('delete')}
          </Button>
        </DialogActions>
      </Dialog>
    </Stack>
  );
}
