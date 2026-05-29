'use client';

import { useEffect, useMemo, useState } from 'react';

import { useTranslations } from '@/hooks/useTranslations';
import { useAuth } from '@/lib/auth/AuthProvider';
import { apiFetch } from '@/lib/api/client';

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
import FormControlLabel from '@mui/material/FormControlLabel';
import FormGroup from '@mui/material/FormGroup';
import InputLabel from '@mui/material/InputLabel';
import MenuItem from '@mui/material/MenuItem';
import Select, { type SelectChangeEvent } from '@mui/material/Select';
import Stack from '@mui/material/Stack';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import TextField from '@mui/material/TextField';
import Typography from '@mui/material/Typography';

type RolEstado = 0 | 1 | 2;

type RoleDto = {
  rolId: number;
  nombre: string;
  descripcion?: string | null;
  estado: RolEstado;
  permisosCount: number;
};

type PermissionDto = {
  modulo: string;
  accion: string;
};

type PermissionCatalogItem = {
  modulo: string;
  acciones: string[];
};

function normalizePermissionKey(modulo: string, accion: string) {
  return `${modulo}:${accion}`.toLowerCase();
}

function roleEstadoLabel(estado: RolEstado, ta: (key: string) => string) {
  switch (estado) {
    case 0:
      return ta('roleStateActive');
    case 1:
      return ta('roleStateInactive');
    default:
      return ta('roleStateDeleted');
  }
}

function roleEstadoColor(estado: RolEstado): 'default' | 'success' | 'warning' | 'error' {
  switch (estado) {
    case 0:
      return 'success';
    case 1:
      return 'warning';
    default:
      return 'error';
  }
}

export default function RolesPage() {
  const nav = useTranslations('navigation');
  const tc = useTranslations('common');
  const tp = useTranslations('pages');
  const tf = useTranslations('fields');
  const ta = useTranslations('admin');

  const { has } = useAuth();
  const canViewRoles = has('Administracion.Roles', 'Ver');
  const canCreateRole = has('Administracion.Roles', 'Crear');
  const canEditRole = has('Administracion.Roles', 'Editar');
  const canDeleteRole = has('Administracion.Roles', 'Eliminar');
  const canViewPermissions = has('Administracion.Permisos', 'Ver');
  const canEditPermissions = has('Administracion.Permisos', 'Editar');

  const [items, setItems] = useState<RoleDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [busyRoleId, setBusyRoleId] = useState<number | null>(null);

  const [roleDialogOpen, setRoleDialogOpen] = useState(false);
  const [roleDialogError, setRoleDialogError] = useState<string | null>(null);
  const [roleDialogSaving, setRoleDialogSaving] = useState(false);
  const [editingRole, setEditingRole] = useState<RoleDto | null>(null);
  const [roleNombre, setRoleNombre] = useState('');
  const [roleDescripcion, setRoleDescripcion] = useState('');
  const [roleEstado, setRoleEstado] = useState<RolEstado>(0);

  const [deleteOpen, setDeleteOpen] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<RoleDto | null>(null);

  const [permsOpen, setPermsOpen] = useState(false);
  const [permsLoading, setPermsLoading] = useState(false);
  const [permsError, setPermsError] = useState<string | null>(null);
  const [permsSaving, setPermsSaving] = useState(false);
  const [permsTarget, setPermsTarget] = useState<RoleDto | null>(null);
  const [catalog, setCatalog] = useState<PermissionCatalogItem[]>([]);
  const [selected, setSelected] = useState<Record<string, PermissionDto>>({});

  const selectedCount = useMemo(() => Object.keys(selected).length, [selected]);

  async function loadRoles() {
    if (!canViewRoles) return;
    setLoading(true);
    setError(null);
    try {
      const list = await apiFetch<RoleDto[]>('/api/v1/admin/roles');
      setItems(list);
    } catch (e) {
      setError(e instanceof Error ? e.message : tp('errorLoadingRoles'));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void loadRoles();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [canViewRoles]);

  function openCreateRole() {
    if (!canCreateRole) return;
    setEditingRole(null);
    setRoleNombre('');
    setRoleDescripcion('');
    setRoleEstado(0);
    setRoleDialogError(null);
    setRoleDialogOpen(true);
  }

  function openEditRole(role: RoleDto) {
    if (!canEditRole) return;
    setEditingRole(role);
    setRoleNombre(role.nombre ?? '');
    setRoleDescripcion(role.descripcion ?? '');
    setRoleEstado(role.estado ?? 0);
    setRoleDialogError(null);
    setRoleDialogOpen(true);
  }

  function closeRoleDialog() {
    if (roleDialogSaving) return;
    setRoleDialogOpen(false);
    setEditingRole(null);
    setRoleDialogError(null);
  }

  async function saveRole() {
    setRoleDialogSaving(true);
    setRoleDialogError(null);

    const payload = {
      nombre: roleNombre.trim(),
      descripcion: roleDescripcion.trim() ? roleDescripcion.trim() : null,
      estado: roleEstado,
    };

    try {
      if (editingRole) {
        await apiFetch<void>(`/api/v1/admin/roles/${editingRole.rolId}`, {
          method: 'PUT',
          body: JSON.stringify(payload),
        });
      } else {
        await apiFetch<{ rolId: number }>('/api/v1/admin/roles', {
          method: 'POST',
          body: JSON.stringify(payload),
        });
      }

      setRoleDialogOpen(false);
      setEditingRole(null);
      await loadRoles();
    } catch (e) {
      setRoleDialogError(e instanceof Error ? e.message : tp('errorSavingRole'));
    } finally {
      setRoleDialogSaving(false);
    }
  }

  function openDeleteDialog(role: RoleDto) {
    if (!canDeleteRole) return;
    setDeleteTarget(role);
    setDeleteError(null);
    setDeleteOpen(true);
  }

  function closeDeleteDialog() {
    if (busyRoleId !== null) return;
    setDeleteOpen(false);
    setDeleteTarget(null);
    setDeleteError(null);
  }

  async function confirmDeleteRole() {
    if (!canDeleteRole) return;
    if (!deleteTarget) return;
    setBusyRoleId(deleteTarget.rolId);
    setDeleteError(null);
    try {
      await apiFetch<void>(`/api/v1/admin/roles/${deleteTarget.rolId}`, { method: 'DELETE' });
      setDeleteOpen(false);
      setDeleteTarget(null);
      await loadRoles();
    } catch (e) {
      setDeleteError(e instanceof Error ? e.message : tp('errorDeletingRole'));
    } finally {
      setBusyRoleId(null);
    }
  }

  async function ensureCatalog() {
    if (catalog.length > 0) return;
    const data = await apiFetch<PermissionCatalogItem[]>('/api/v1/admin/permissions/catalog');
    setCatalog(data);
  }

  function openPermissions(role: RoleDto) {
    if (!canViewPermissions) return;
    setPermsTarget(role);
    setPermsOpen(true);
    setPermsError(null);
    setSelected({});

    void (async () => {
      setPermsLoading(true);
      try {
        await ensureCatalog();
        const perms = await apiFetch<PermissionDto[]>(`/api/v1/admin/roles/${role.rolId}/permissions`);
        const next: Record<string, PermissionDto> = {};
        for (const p of perms) {
          const modulo = (p.modulo ?? '').trim();
          const accion = (p.accion ?? '').trim();
          if (!modulo || !accion) continue;
          next[normalizePermissionKey(modulo, accion)] = { modulo, accion };
        }
        setSelected(next);
      } catch (e) {
        setPermsError(e instanceof Error ? e.message : tp('errorLoadingPermissions'));
      } finally {
        setPermsLoading(false);
      }
    })();
  }

  function closePermissions() {
    if (permsSaving) return;
    setPermsOpen(false);
    setPermsTarget(null);
    setPermsError(null);
    setSelected({});
  }

  function togglePermission(modulo: string, accion: string) {
    const key = normalizePermissionKey(modulo, accion);
    setSelected((prev) => {
      if (prev[key]) {
        const next = { ...prev };
        delete next[key];
        return next;
      }
      return { ...prev, [key]: { modulo, accion } };
    });
  }

  async function savePermissions() {
    if (!canEditPermissions) return;
    if (!permsTarget) return;

    setPermsSaving(true);
    setPermsError(null);
    try {
      const payload = Object.values(selected);

      await apiFetch<void>(`/api/v1/admin/roles/${permsTarget.rolId}/permissions`, {
        method: 'PUT',
        body: JSON.stringify({ permissions: payload }),
      });

      setPermsOpen(false);
      setPermsTarget(null);
      setSelected({});
      await loadRoles();
    } catch (e) {
      setPermsError(e instanceof Error ? e.message : tp('errorSavingPermissions'));
    } finally {
      setPermsSaving(false);
    }
  }

  if (!canViewRoles) {
    return (
      <Stack spacing={1}>
        <Typography variant="h4">{nav('roles')}</Typography>
        <Typography variant="body2" color="text.secondary">
          {tp('noPermission')}
        </Typography>
      </Stack>
    );
  }

  return (
    <Stack spacing={2}>
      <Stack direction="row" alignItems="center" justifyContent="space-between" spacing={2}>
        <Typography variant="h4">{nav('roles')}</Typography>
        <Stack direction="row" spacing={1}>
          <Button variant="outlined" onClick={() => void loadRoles()} disabled={loading}>
            {tc('refresh')}
          </Button>
          <Button variant="contained" onClick={openCreateRole} disabled={!canCreateRole}>
            {ta('newRole')}
          </Button>
        </Stack>
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
                    <TableCell>{tf('name')}</TableCell>
                    <TableCell>{tf('description')}</TableCell>
                    <TableCell>{tf('status')}</TableCell>
                    <TableCell>{tf('permissions')}</TableCell>
                    <TableCell align="right">{tf('actions')}</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {items.map((r) => {
                    const isBusy = busyRoleId === r.rolId;
                    return (
                      <TableRow key={r.rolId} hover>
                        <TableCell>{r.nombre}</TableCell>
                        <TableCell>{r.descripcion || '—'}</TableCell>
                        <TableCell>
                          <Chip size="small" color={roleEstadoColor(r.estado)} label={roleEstadoLabel(r.estado, ta)} />
                        </TableCell>
                        <TableCell>{r.permisosCount}</TableCell>
                        <TableCell align="right">
                          <Stack direction="row" spacing={1} justifyContent="flex-end">
                            <Button
                              size="small"
                              variant="outlined"
                              onClick={() => openEditRole(r)}
                              disabled={isBusy || !canEditRole}
                            >
                              {tc('edit')}
                            </Button>
                            <Button
                              size="small"
                              variant="outlined"
                              onClick={() => openPermissions(r)}
                              disabled={isBusy || !canViewPermissions}
                            >
                              {ta('managePermissions')}
                            </Button>
                            <Button
                              size="small"
                              color="error"
                              variant="outlined"
                              onClick={() => openDeleteDialog(r)}
                              disabled={isBusy || !canDeleteRole}
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

      <Dialog open={roleDialogOpen} onClose={closeRoleDialog} fullWidth maxWidth="sm">
        <DialogTitle>
          {editingRole ? ta('editRoleTitle', { name: editingRole.nombre }) : ta('newRoleTitle')}
        </DialogTitle>
        <DialogContent dividers>
          <Stack spacing={2} sx={{ pt: 1 }}>
            {roleDialogError ? <Alert severity="error">{roleDialogError}</Alert> : null}

            <TextField
              label={ta('roleNameLabel')}
              value={roleNombre}
              onChange={(e) => setRoleNombre(e.target.value)}
              fullWidth
              autoFocus
            />

            <TextField
              label={ta('roleDescriptionLabel')}
              value={roleDescripcion}
              onChange={(e) => setRoleDescripcion(e.target.value)}
              fullWidth
              multiline
              minRows={2}
            />

            <FormControl fullWidth>
              <InputLabel id="estado-role">{tf('status')}</InputLabel>
              <Select
                labelId="estado-role"
                label={tf('status')}
                value={String(roleEstado)}
                onChange={(e: SelectChangeEvent) => setRoleEstado(Number(e.target.value) as RolEstado)}
              >
                <MenuItem value="0">{ta('roleStateActive')}</MenuItem>
                <MenuItem value="1">{ta('roleStateInactive')}</MenuItem>
              </Select>
            </FormControl>
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={closeRoleDialog} disabled={roleDialogSaving}>
            {tc('cancel')}
          </Button>
          <Button
            onClick={() => void saveRole()}
            variant="contained"
            disabled={roleDialogSaving || (editingRole ? !canEditRole : !canCreateRole)}
          >
            {tc('save')}
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={deleteOpen} onClose={closeDeleteDialog} fullWidth maxWidth="xs">
        <DialogTitle>{ta('confirmDeleteRoleTitle')}</DialogTitle>
        <DialogContent dividers>
          <Stack spacing={2} sx={{ pt: 1 }}>
            {deleteError ? <Alert severity="error">{deleteError}</Alert> : null}
            <Typography variant="body2">
              {deleteTarget ? ta('confirmDeleteRoleBody', { name: deleteTarget.nombre }) : null}
            </Typography>
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={closeDeleteDialog} disabled={busyRoleId !== null}>
            {tc('cancel')}
          </Button>
          <Button
            onClick={() => void confirmDeleteRole()}
            color="error"
            variant="contained"
            disabled={busyRoleId !== null || !deleteTarget || !canDeleteRole}
          >
            {tc('delete')}
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={permsOpen} onClose={closePermissions} fullWidth maxWidth="md">
        <DialogTitle>
          {permsTarget ? ta('permissionsTitle', { name: permsTarget.nombre }) : ta('permissionsTitleEmpty')}
        </DialogTitle>
        <DialogContent dividers>
          <Stack spacing={2} sx={{ pt: 1 }}>
            {permsError ? <Alert severity="error">{permsError}</Alert> : null}
            <Typography variant="body2" color="text.secondary">
              {ta('selectedPermissionsCount', { count: selectedCount })}
            </Typography>

            {permsLoading ? (
              <Typography variant="body2" color="text.secondary">
                {tc('loading')}
              </Typography>
            ) : (
              <Stack spacing={2}>
                {catalog.map((m) => (
                  <Card key={m.modulo} variant="outlined">
                    <CardHeader title={m.modulo} sx={{ pb: 0 }} />
                    <CardContent>
                      <FormGroup row>
                        {m.acciones.map((accion) => {
                          const key = normalizePermissionKey(m.modulo, accion);
                          return (
                            <FormControlLabel
                              key={accion}
                              control={
                                <Checkbox
                                  checked={Boolean(selected[key])}
                                  onChange={() => togglePermission(m.modulo, accion)}
                                  disabled={!canEditPermissions}
                                />
                              }
                              label={accion}
                            />
                          );
                        })}
                      </FormGroup>
                    </CardContent>
                  </Card>
                ))}
              </Stack>
            )}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={closePermissions} disabled={permsSaving}>
            {tc('cancel')}
          </Button>
          <Button
            onClick={() => void savePermissions()}
            variant="contained"
            disabled={permsSaving || permsLoading || !permsTarget || !canEditPermissions}
          >
            {tc('save')}
          </Button>
        </DialogActions>
      </Dialog>
    </Stack>
  );
}
