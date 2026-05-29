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

import { useTranslations } from '@/hooks/useTranslations';
import { apiFetch } from '@/lib/api/client';
import { useAuth } from '@/lib/auth/AuthProvider';

type TipoVenta = {
  tipoVentaId: number;
  codigo: string;
  nombre: string;
  estado: boolean;
};

type TipoVentaRequest = {
  codigo: string;
  nombre: string;
  estado: boolean;
};

export function TiposVentaCrud() {
  const nav = useTranslations('navigation');
  const tc = useTranslations('common');
  const tf = useTranslations('fields');
  const tp = useTranslations('pages');
  const tcrud = useTranslations('crud');

  const { has } = useAuth();
  const canView = has('Ventas.TiposVenta', 'Ver');
  const canCreate = has('Ventas.TiposVenta', 'Crear');
  const canEdit = has('Ventas.TiposVenta', 'Editar');
  const canDelete = has('Ventas.TiposVenta', 'Eliminar');

  const [items, setItems] = useState<TipoVenta[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [editOpen, setEditOpen] = useState(false);
  const [editError, setEditError] = useState<string | null>(null);
  const [editSaving, setEditSaving] = useState(false);
  const [editing, setEditing] = useState<TipoVenta | null>(null);
  const [codigo, setCodigo] = useState('');
  const [nombre, setNombre] = useState('');
  const [estado, setEstado] = useState(true);

  const [deleteOpen, setDeleteOpen] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);
  const [deleteSaving, setDeleteSaving] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState<TipoVenta | null>(null);

  async function reload() {
    setLoading(true);
    setError(null);
    try {
      const list = await apiFetch<TipoVenta[]>('/api/v1/tipos-venta');
      setItems(list);
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

  function openCreate() {
    if (!canCreate) return;
    setEditing(null);
    setCodigo('');
    setNombre('');
    setEstado(true);
    setEditError(null);
    setEditOpen(true);
  }

  function openEdit(item: TipoVenta) {
    if (!canEdit) return;
    setEditing(item);
    setCodigo(item.codigo);
    setNombre(item.nombre);
    setEstado(item.estado);
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

    const payload: TipoVentaRequest = {
      codigo: codigo.trim(),
      nombre: nombre.trim(),
      estado,
    };

    try {
      if (editing) {
        await apiFetch<void>(`/api/v1/tipos-venta/${editing.tipoVentaId}`, {
          method: 'PUT',
          body: JSON.stringify(payload),
        });
      } else {
        await apiFetch<{ id: number }>('/api/v1/tipos-venta', {
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

  function openDelete(item: TipoVenta) {
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
      await apiFetch<void>(`/api/v1/tipos-venta/${deleteTarget.tipoVentaId}`, { method: 'DELETE' });
      setDeleteOpen(false);
      setDeleteTarget(null);
      await reload();
    } catch (e) {
      setDeleteError(e instanceof Error ? e.message : tcrud('errorDeleting'));
    } finally {
      setDeleteSaving(false);
    }
  }

  const canSave = Boolean(codigo.trim()) && Boolean(nombre.trim());

  if (!canView) {
    return (
      <Stack spacing={1}>
        <Typography variant="h4">{nav('tiposVenta')}</Typography>
        <Typography variant="body2" color="text.secondary">
          {tp('noPermission')}
        </Typography>
      </Stack>
    );
  }

  return (
    <Stack spacing={2}>
      <Stack direction="row" alignItems="center" justifyContent="space-between" spacing={2}>
        <Typography variant="h4">{nav('tiposVenta')}</Typography>
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
                    <TableCell>{tf('code')}</TableCell>
                    <TableCell>{tf('name')}</TableCell>
                    <TableCell>{tf('status')}</TableCell>
                    <TableCell align="right">{tf('actions')}</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {items.map((item) => (
                    <TableRow key={item.tipoVentaId} hover>
                      <TableCell>{item.tipoVentaId}</TableCell>
                      <TableCell>{item.codigo}</TableCell>
                      <TableCell>{item.nombre}</TableCell>
                      <TableCell>
                        <Chip size="small" color={item.estado ? 'success' : 'default'} label={item.estado ? tf('active') : tf('inactive')} />
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
        <DialogTitle>{editing ? `${tc('edit')} — ${nav('tiposVenta')}` : `${tcrud('new')} — ${nav('tiposVenta')}`}</DialogTitle>
        <DialogContent dividers>
          <Stack spacing={2} sx={{ pt: 1 }}>
            {editError ? <Alert severity="error">{editError}</Alert> : null}
            <TextField label={tf('code')} value={codigo} onChange={(e) => setCodigo(e.target.value)} autoFocus fullWidth />
            <TextField label={tf('name')} value={nombre} onChange={(e) => setNombre(e.target.value)} fullWidth />
            <FormControlLabel control={<Checkbox checked={estado} onChange={(e) => setEstado(e.target.checked)} />} label={tf('active')} />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={closeEdit} disabled={editSaving}>
            {tc('cancel')}
          </Button>
          <Button onClick={() => void saveEdit()} variant="contained" disabled={editSaving || !canSave || (editing ? !canEdit : !canCreate)}>
            {tc('save')}
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={deleteOpen} onClose={closeDelete} fullWidth maxWidth="xs">
        <DialogTitle>{tc('delete')}</DialogTitle>
        <DialogContent dividers>
          <Stack spacing={2} sx={{ pt: 1 }}>
            {deleteError ? <Alert severity="error">{deleteError}</Alert> : null}
            <Typography variant="body2">{tcrud('confirmDeleteRecord')}</Typography>
            {deleteTarget ? <Typography variant="body2" color="text.secondary">{deleteTarget.nombre}</Typography> : null}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={closeDelete} disabled={deleteSaving}>
            {tc('cancel')}
          </Button>
          <Button onClick={() => void confirmDelete()} color="error" variant="contained" disabled={deleteSaving || !deleteTarget || !canDelete}>
            {tc('delete')}
          </Button>
        </DialogActions>
      </Dialog>
    </Stack>
  );
}