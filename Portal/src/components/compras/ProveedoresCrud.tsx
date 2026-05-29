'use client';

import { useEffect, useState } from 'react';

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
import FormControlLabel from '@mui/material/FormControlLabel';
import Stack from '@mui/material/Stack';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import TextField from '@mui/material/TextField';
import Typography from '@mui/material/Typography';

import { apiFetch } from '@/lib/api/client';
import { useAuth } from '@/lib/auth/AuthProvider';
import { useTranslations } from '@/hooks/useTranslations';

type Proveedor = {
  proveedorId: number;
  nombre: string;
  documento?: string | null;
  telefono?: string | null;
  email?: string | null;
  direccion?: string | null;
  estado: boolean;
};

type ProveedorRequest = {
  nombre: string;
  documento?: string;
  telefono?: string;
  email?: string;
  direccion?: string;
  estado: boolean;
};

export function ProveedoresCrud() {
  const nav = useTranslations('navigation');
  const tc = useTranslations('common');
  const tf = useTranslations('fields');
  const tp = useTranslations('pages');
  const tcrud = useTranslations('crud');

  const { has } = useAuth();
  const canView = has('Compras.Proveedores', 'Ver');
  const canCreate = has('Compras.Proveedores', 'Crear');
  const canEdit = has('Compras.Proveedores', 'Editar');
  const canDelete = has('Compras.Proveedores', 'Eliminar');

  const [items, setItems] = useState<Proveedor[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [editOpen, setEditOpen] = useState(false);
  const [editError, setEditError] = useState<string | null>(null);
  const [editSaving, setEditSaving] = useState(false);
  const [editing, setEditing] = useState<Proveedor | null>(null);

  const [nombre, setNombre] = useState('');
  const [documento, setDocumento] = useState('');
  const [telefono, setTelefono] = useState('');
  const [email, setEmail] = useState('');
  const [direccion, setDireccion] = useState('');
  const [estado, setEstado] = useState(true);

  const [deleteOpen, setDeleteOpen] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);
  const [deleteSaving, setDeleteSaving] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState<Proveedor | null>(null);

  async function reload() {
    setLoading(true);
    setError(null);
    try {
      const data = await apiFetch<Proveedor[]>('/api/v1/compras/proveedores');
      setItems(data);
    } catch (e) {
      setError(e instanceof Error ? e.message : tcrud('errorLoadingData'));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void reload();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  function resetForm() {
    setNombre('');
    setDocumento('');
    setTelefono('');
    setEmail('');
    setDireccion('');
    setEstado(true);
  }

  function openCreate() {
    if (!canCreate) return;
    setEditing(null);
    resetForm();
    setEditError(null);
    setEditOpen(true);
  }

  function openEdit(item: Proveedor) {
    if (!canEdit) return;
    setEditing(item);
    setNombre(item.nombre ?? '');
    setDocumento(item.documento ?? '');
    setTelefono(item.telefono ?? '');
    setEmail(item.email ?? '');
    setDireccion(item.direccion ?? '');
    setEstado(Boolean(item.estado));
    setEditError(null);
    setEditOpen(true);
  }

  function closeEdit() {
    if (editSaving) return;
    setEditOpen(false);
    setEditing(null);
    setEditError(null);
  }

  async function saveEdit() {
    setEditSaving(true);
    setEditError(null);

    const payload: ProveedorRequest = {
      nombre: nombre.trim(),
      estado,
      ...(documento.trim() ? { documento: documento.trim() } : {}),
      ...(telefono.trim() ? { telefono: telefono.trim() } : {}),
      ...(email.trim() ? { email: email.trim() } : {}),
      ...(direccion.trim() ? { direccion: direccion.trim() } : {}),
    };

    try {
      if (editing) {
        await apiFetch<void>(`/api/v1/compras/proveedores/${editing.proveedorId}`, {
          method: 'PUT',
          body: JSON.stringify(payload),
        });
      } else {
        await apiFetch<{ id: number }>('/api/v1/compras/proveedores', {
          method: 'POST',
          body: JSON.stringify(payload),
        });
      }

      setEditOpen(false);
      setEditing(null);
      await reload();
    } catch (e) {
      setEditError(e instanceof Error ? e.message : tcrud('errorSaving'));
    } finally {
      setEditSaving(false);
    }
  }

  function openDelete(item: Proveedor) {
    if (!canDelete) return;
    setDeleteTarget(item);
    setDeleteError(null);
    setDeleteOpen(true);
  }

  function closeDelete() {
    if (deleteSaving) return;
    setDeleteOpen(false);
    setDeleteTarget(null);
    setDeleteError(null);
  }

  async function confirmDelete() {
    if (!canDelete || !deleteTarget) return;
    setDeleteSaving(true);
    setDeleteError(null);
    try {
      await apiFetch<void>(`/api/v1/compras/proveedores/${deleteTarget.proveedorId}`, { method: 'DELETE' });
      setDeleteOpen(false);
      setDeleteTarget(null);
      await reload();
    } catch (e) {
      setDeleteError(e instanceof Error ? e.message : tcrud('errorDeleting'));
    } finally {
      setDeleteSaving(false);
    }
  }

  if (!canView) {
    return (
      <Stack spacing={1}>
        <Typography variant="h4">{nav('proveedores')}</Typography>
        <Typography variant="body2" color="text.secondary">
          {tp('noPermission')}
        </Typography>
      </Stack>
    );
  }

  return (
    <Stack spacing={2}>
      <Stack direction="row" alignItems="center" justifyContent="space-between" spacing={2}>
        <Typography variant="h4">{nav('proveedores')}</Typography>
        <Stack direction="row" spacing={1}>
          <Button variant="outlined" onClick={() => void reload()} disabled={loading}>
            {tc('refresh')}
          </Button>
          <Button variant="contained" onClick={openCreate} disabled={!canCreate}>
            {tcrud('new')}
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
                    <TableCell>{tf('id')}</TableCell>
                    <TableCell>{tf('name')}</TableCell>
                    <TableCell>{tf('document')}</TableCell>
                    <TableCell>{tf('phone')}</TableCell>
                    <TableCell>{tf('email')}</TableCell>
                    <TableCell>{tf('status')}</TableCell>
                    <TableCell align="right">{tf('actions')}</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {items.map((item) => (
                    <TableRow key={item.proveedorId} hover>
                      <TableCell>{item.proveedorId}</TableCell>
                      <TableCell>{item.nombre}</TableCell>
                      <TableCell>{item.documento || '-'}</TableCell>
                      <TableCell>{item.telefono || '-'}</TableCell>
                      <TableCell>{item.email || '-'}</TableCell>
                      <TableCell>
                        <Chip
                          size="small"
                          color={item.estado ? 'success' : 'default'}
                          label={item.estado ? tf('active') : tf('inactive')}
                        />
                      </TableCell>
                      <TableCell align="right">
                        <Stack direction="row" spacing={1} justifyContent="flex-end">
                          <Button size="small" variant="outlined" onClick={() => openEdit(item)} disabled={!canEdit}>
                            {tc('edit')}
                          </Button>
                          <Button size="small" color="error" variant="outlined" onClick={() => openDelete(item)} disabled={!canDelete}>
                            {tc('delete')}
                          </Button>
                        </Stack>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          )}
        </CardContent>
      </Card>

      <Dialog open={editOpen} onClose={closeEdit} fullWidth maxWidth="sm">
        <DialogTitle>{editing ? tc('edit') : tcrud('create')}</DialogTitle>
        <DialogContent sx={{ display: 'grid', gap: 2, pt: '12px !important' }}>
          {editError ? <Alert severity="error">{editError}</Alert> : null}
          <TextField label={tf('name')} value={nombre} onChange={(e) => setNombre(e.target.value)} required autoFocus />
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
            <TextField fullWidth label={tf('document')} value={documento} onChange={(e) => setDocumento(e.target.value)} />
            <TextField fullWidth label={tf('phone')} value={telefono} onChange={(e) => setTelefono(e.target.value)} />
          </Stack>
          <TextField label={tf('email')} value={email} onChange={(e) => setEmail(e.target.value)} />
          <TextField label={tf('address')} value={direccion} onChange={(e) => setDireccion(e.target.value)} multiline minRows={2} />
          <FormControlLabel control={<Checkbox checked={estado} onChange={(e) => setEstado(e.target.checked)} />} label={tf('active')} />
        </DialogContent>
        <DialogActions>
          <Button onClick={closeEdit} disabled={editSaving}>
            {tc('cancel')}
          </Button>
          <Button onClick={() => void saveEdit()} disabled={editSaving} variant="contained">
            {editing ? tcrud('saveChanges') : tcrud('create')}
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={deleteOpen} onClose={closeDelete} fullWidth maxWidth="xs">
        <DialogTitle>{tc('delete')}</DialogTitle>
        <DialogContent sx={{ display: 'grid', gap: 2, pt: '12px !important' }}>
          {deleteError ? <Alert severity="error">{deleteError}</Alert> : null}
          <Typography variant="body2">{tcrud('confirmDeleteRecord')}</Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={closeDelete} disabled={deleteSaving}>
            {tc('cancel')}
          </Button>
          <Button onClick={() => void confirmDelete()} disabled={deleteSaving} color="error" variant="contained">
            {tc('delete')}
          </Button>
        </DialogActions>
      </Dialog>
    </Stack>
  );
}