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

type LedgerItem = {
  id: number;
  nombre: string;
  monto: number;
  fecha?: string | null;
  estado: boolean;
};

type LedgerCrudProps = {
  title: string;
  apiBasePath: string;
  idKey: 'costoId' | 'gastoId' | 'pasivoId';
  permModule: string;
};

type CostoApiItem = {
  costoId: number;
  nombre: string;
  monto: number;
  fecha?: string | null;
  estado: boolean;
};

type GastoApiItem = {
  gastoId: number;
  nombre: string;
  monto: number;
  fecha?: string | null;
  estado: boolean;
};

type PasivoApiItem = {
  pasivoId: number;
  nombre: string;
  monto: number;
  fecha?: string | null;
  estado: boolean;
};

type LedgerApiItem = CostoApiItem | GastoApiItem | PasivoApiItem;

type LedgerRequest = {
  nombre: string;
  monto: number;
  estado: boolean;
  fecha?: string;
};

function getId(item: LedgerApiItem, idKey: LedgerCrudProps['idKey']) {
  switch (idKey) {
    case 'costoId':
      return (item as CostoApiItem).costoId;
    case 'gastoId':
      return (item as GastoApiItem).gastoId;
    case 'pasivoId':
      return (item as PasivoApiItem).pasivoId;
  }
}

function toInputDate(value?: string | null) {
  if (!value) return '';
  return value.slice(0, 10);
}

export function LedgerCrud({ title, apiBasePath, idKey, permModule }: LedgerCrudProps) {
  const tc = useTranslations('common');
  const tf = useTranslations('fields');
  const tp = useTranslations('pages');
  const tcrud = useTranslations('crud');

  const { has } = useAuth();
  const canView = has(permModule, 'Ver');
  const canCreate = has(permModule, 'Crear');
  const canEdit = has(permModule, 'Editar');
  const canDelete = has(permModule, 'Eliminar');

  const [items, setItems] = useState<LedgerItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [editOpen, setEditOpen] = useState(false);
  const [editError, setEditError] = useState<string | null>(null);
  const [editSaving, setEditSaving] = useState(false);
  const [editing, setEditing] = useState<LedgerItem | null>(null);

  const [nombre, setNombre] = useState('');
  const [monto, setMonto] = useState<string>('0');
  const [fecha, setFecha] = useState<string>('');
  const [estado, setEstado] = useState(true);

  const [deleteOpen, setDeleteOpen] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<LedgerItem | null>(null);
  const [deleteSaving, setDeleteSaving] = useState(false);

  async function reload() {
    setLoading(true);
    setError(null);
    try {
      const raw = await apiFetch<LedgerApiItem[]>(`${apiBasePath}`);
      const mapped: LedgerItem[] = raw.map((x) => ({
        id: getId(x, idKey),
        nombre: x.nombre,
        monto: Number(x.monto ?? 0),
        fecha: x.fecha ?? null,
        estado: Boolean(x.estado),
      }));
      setItems(mapped);
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
    setNombre('');
    setMonto('0');
    setFecha('');
    setEstado(true);
    setEditError(null);
    setEditOpen(true);
  }

  function openEdit(item: LedgerItem) {
    if (!canEdit) return;
    setEditing(item);
    setNombre(item.nombre);
    setMonto(String(item.monto ?? 0));
    setFecha(toInputDate(item.fecha));
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

    const payload: LedgerRequest = {
      nombre: nombre.trim(),
      monto: Number(monto),
      estado,
      ...(fecha ? { fecha: new Date(fecha).toISOString() } : {}),
    };

    try {
      if (editing) {
        await apiFetch<void>(`${apiBasePath}/${editing.id}`, {
          method: 'PUT',
          body: JSON.stringify(payload),
        });
      } else {
        await apiFetch<{ id: number }>(`${apiBasePath}`, {
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

  function openDelete(item: LedgerItem) {
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
      await apiFetch<void>(`${apiBasePath}/${deleteTarget.id}`, { method: 'DELETE' });
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
        <Typography variant="h4">{title}</Typography>
        <Typography variant="body2" color="text.secondary">
          {tp('noPermission')}
        </Typography>
      </Stack>
    );
  }

  return (
    <Stack spacing={2}>
      <Stack direction="row" alignItems="center" justifyContent="space-between" spacing={2}>
        <Typography variant="h4">{title}</Typography>
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
                    <TableCell>{tf('amount')}</TableCell>
                    <TableCell>{tf('date')}</TableCell>
                    <TableCell>{tf('status')}</TableCell>
                    <TableCell align="right">{tf('actions')}</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {items.map((x) => (
                    <TableRow key={x.id} hover>
                      <TableCell>{x.id}</TableCell>
                      <TableCell>{x.nombre}</TableCell>
                      <TableCell>{x.monto}</TableCell>
                      <TableCell>{toInputDate(x.fecha) || '—'}</TableCell>
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
          {editing ? `${tc('edit')} — ${title}` : `${tcrud('new')} — ${title}`}
        </DialogTitle>
        <DialogContent dividers>
          <Stack spacing={2} sx={{ pt: 1 }}>
            {editError ? <Alert severity="error">{editError}</Alert> : null}
            <TextField label={tf('name')} value={nombre} onChange={(e) => setNombre(e.target.value)} fullWidth autoFocus />
            <TextField
              label={tf('amount')}
              value={monto}
              onChange={(e) => setMonto(e.target.value)}
              type="number"
              inputProps={{ step: 0.01 }}
              fullWidth
            />
            <TextField
              label={tf('date')}
              value={fecha}
              onChange={(e) => setFecha(e.target.value)}
              type="date"
              fullWidth
              InputLabelProps={{ shrink: true }}
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
            <Typography variant="body2">{tcrud('confirmDeleteRecord')}</Typography>
            {deleteTarget ? (
              <Typography variant="body2" color="text.secondary">
                {deleteTarget.nombre}
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
