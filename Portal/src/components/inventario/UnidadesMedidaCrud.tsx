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

type UnidadMedida = {
  codigo: string;
  nombre: string;
  estado: boolean;
};

type UnidadMedidaCreateRequest = {
  codigo: string;
  nombre: string;
  estado: boolean;
};

type UnidadMedidaUpdateRequest = {
  nombre: string;
  estado: boolean;
};

type UnidadesMedidaCrudProps = {
  embedded?: boolean;
};

export function UnidadesMedidaCrud({ embedded = false }: UnidadesMedidaCrudProps) {
  const nav = useTranslations('navigation');
  const tc = useTranslations('common');
  const tf = useTranslations('fields');
  const tp = useTranslations('pages');
  const tcrud = useTranslations('crud');

  const { has } = useAuth();
  const canView = has('Inventario.UnidadesMedida', 'Ver');
  const canCreate = has('Inventario.UnidadesMedida', 'Crear');
  const canEdit = has('Inventario.UnidadesMedida', 'Editar');
  const canDelete = has('Inventario.UnidadesMedida', 'Eliminar');

  const [items, setItems] = useState<UnidadMedida[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [editOpen, setEditOpen] = useState(false);
  const [editError, setEditError] = useState<string | null>(null);
  const [editSaving, setEditSaving] = useState(false);
  const [editing, setEditing] = useState<UnidadMedida | null>(null);
  const [codigo, setCodigo] = useState('');
  const [nombre, setNombre] = useState('');
  const [estado, setEstado] = useState(true);

  const [deleteOpen, setDeleteOpen] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<UnidadMedida | null>(null);
  const [deleteSaving, setDeleteSaving] = useState(false);

  async function reload() {
    setLoading(true);
    setError(null);
    try {
      const list = await apiFetch<UnidadMedida[]>('/api/v1/unidades-medida');
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

  function openEdit(item: UnidadMedida) {
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
    try {
      if (editing) {
        const payload: UnidadMedidaUpdateRequest = { nombre: nombre.trim(), estado };
        await apiFetch<void>(`/api/v1/unidades-medida/${encodeURIComponent(editing.codigo)}`, {
          method: 'PUT',
          body: JSON.stringify(payload),
        });
      } else {
        const payload: UnidadMedidaCreateRequest = { codigo: codigo.trim(), nombre: nombre.trim(), estado };
        await apiFetch<{ codigo: string }>(`/api/v1/unidades-medida`, {
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

  function openDelete(item: UnidadMedida) {
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
    if (!canDelete) return;
    if (!deleteTarget) return;
    setDeleteSaving(true);
    setDeleteError(null);
    try {
      await apiFetch<void>(`/api/v1/unidades-medida/${encodeURIComponent(deleteTarget.codigo)}`, { method: 'DELETE' });
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
        <Typography variant="h4">{nav('unidadesMedida')}</Typography>
        <Typography variant="body2" color="text.secondary">
          {tp('noPermission')}
        </Typography>
      </Stack>
    );
  }

  return (
    <Stack spacing={embedded ? 1.5 : 2}>
      <Stack direction="row" alignItems="center" justifyContent={embedded ? 'flex-end' : 'space-between'} spacing={2}>
        {embedded ? null : <Typography variant="h4">{nav('unidadesMedida')}</Typography>}
        <Stack direction="row" spacing={1} sx={embedded ? { ml: 'auto' } : undefined}>
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
        {embedded ? null : <CardHeader title={tc('listTitle')} />}
        <CardContent sx={{ pt: embedded ? 2 : 0 }}>
          {loading ? (
            <Typography variant="body2" color="text.secondary">
              {tc('loading')}
            </Typography>
          ) : (
            <TableContainer>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>{tf('code')}</TableCell>
                    <TableCell>{tf('name')}</TableCell>
                    <TableCell>{tf('status')}</TableCell>
                    <TableCell align="right">{tf('actions')}</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {items.map((x) => (
                    <TableRow key={x.codigo} hover>
                      <TableCell>{x.codigo}</TableCell>
                      <TableCell>{x.nombre}</TableCell>
                      <TableCell>
                        <Chip
                          size="small"
                          color={x.estado ? 'success' : 'default'}
                          label={x.estado ? tf('active') : tf('inactive')}
                        />
                      </TableCell>
                      <TableCell align="right">
                        <Stack direction="row" spacing={1} justifyContent="flex-end">
                          <Button size="small" variant="outlined" onClick={() => openEdit(x)} disabled={!canEdit}>
                            {tc('edit')}
                          </Button>
                          <Button
                            size="small"
                            color="error"
                            variant="outlined"
                            onClick={() => openDelete(x)}
                            disabled={!canDelete}
                          >
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
        <DialogTitle>
          {editing ? `${tc('edit')} — ${nav('unidadesMedida')}` : `${tcrud('new')} — ${nav('unidadesMedida')}`}
        </DialogTitle>
        <DialogContent dividers>
          <Stack spacing={2} sx={{ pt: 1 }}>
            {editError ? <Alert severity="error">{editError}</Alert> : null}
            <TextField label={tf('name')} value={nombre} onChange={(e) => setNombre(e.target.value)} helperText={tf('unitNameHelper')} fullWidth autoFocus />
            <TextField
              label={tf('code')}
              value={codigo}
              onChange={(e) => setCodigo(e.target.value.toUpperCase())}
              helperText={editing ? tf('unitCodeLockedHelper') : tf('unitCodeHelper')}
              fullWidth
              disabled={Boolean(editing)}
              placeholder="UN"
            />
            <FormControlLabel
              control={<Checkbox checked={estado} onChange={(e) => setEstado(e.target.checked)} />}
              label={tf('active')}
            />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={closeEdit} disabled={editSaving}>
            {tc('cancel')}
          </Button>
          <Button
            onClick={() => void saveEdit()}
            variant="contained"
            disabled={editSaving || (editing ? !canEdit : !canCreate)}
          >
            {tc('save')}
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={deleteOpen} onClose={closeDelete} fullWidth maxWidth="xs">
        <DialogTitle>{tc('delete')}</DialogTitle>
        <DialogContent dividers>
          <Stack spacing={2} sx={{ pt: 1 }}>
            {deleteError ? <Alert severity="error">{deleteError}</Alert> : null}
            <Typography variant="body2">{tcrud('confirmDeleteUnitOfMeasure')}</Typography>
            {deleteTarget ? (
              <Typography variant="body2" color="text.secondary">
                {deleteTarget.codigo} — {deleteTarget.nombre}
              </Typography>
            ) : null}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={closeDelete} disabled={deleteSaving}>
            {tc('cancel')}
          </Button>
          <Button
            onClick={() => void confirmDelete()}
            color="error"
            variant="contained"
            disabled={deleteSaving || !deleteTarget || !canDelete}
          >
            {tc('delete')}
          </Button>
        </DialogActions>
      </Dialog>
    </Stack>
  );
}
