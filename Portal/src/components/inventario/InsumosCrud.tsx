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
import MenuItem from '@mui/material/MenuItem';
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

type Insumo = {
  insumoId: number;
  nombre: string;
  descripcion?: string | null;
  cantidad: number;
  costoUnitario: number;
  unidadMedida: string;
  estado: boolean;
};

type InsumoRequest = {
  nombre: string;
  descripcion?: string;
  cantidad?: number;
  costoUnitario?: number;
  unidadMedida?: string;
  estado: boolean;
};

export function InsumosCrud() {
  const nav = useTranslations('navigation');
  const tc = useTranslations('common');
  const tf = useTranslations('fields');
  const tp = useTranslations('pages');
  const tcrud = useTranslations('crud');

  const { has } = useAuth();
  const canView = has('Inventario.Insumos', 'Ver');
  const canCreate = has('Inventario.Insumos', 'Crear');
  const canEdit = has('Inventario.Insumos', 'Editar');
  const canDelete = has('Inventario.Insumos', 'Eliminar');

  const [items, setItems] = useState<Insumo[]>([]);
  const [unidades, setUnidades] = useState<UnidadMedida[]>([]);

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [editOpen, setEditOpen] = useState(false);
  const [editError, setEditError] = useState<string | null>(null);
  const [editSaving, setEditSaving] = useState(false);
  const [editing, setEditing] = useState<Insumo | null>(null);

  const [nombre, setNombre] = useState('');
  const [descripcion, setDescripcion] = useState('');
  const [cantidad, setCantidad] = useState<string>('');
  const [costoUnitario, setCostoUnitario] = useState<string>('');
  const [unidadMedida, setUnidadMedida] = useState<string>('');
  const [estado, setEstado] = useState(true);

  const [deleteOpen, setDeleteOpen] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<Insumo | null>(null);
  const [deleteSaving, setDeleteSaving] = useState(false);

  async function reload() {
    setLoading(true);
    setError(null);
    try {
      const [insumosRes, unidadesRes] = await Promise.allSettled([
        apiFetch<Insumo[]>('/api/v1/insumos'),
        apiFetch<UnidadMedida[]>('/api/v1/unidades-medida'),
      ]);

      if (insumosRes.status === 'fulfilled') {
        setItems(insumosRes.value);
      } else {
        setItems([]);
        setError(insumosRes.reason instanceof Error ? insumosRes.reason.message : tcrud('errorLoadingData'));
      }

      if (unidadesRes.status === 'fulfilled') {
        const activeUnits = unidadesRes.value.filter((u) => u.estado);
        setUnidades(activeUnits);

        if (!unidadMedida && activeUnits.length > 0) {
          setUnidadMedida(activeUnits.find((u) => u.codigo === 'UN')?.codigo ?? activeUnits[0]!.codigo);
        }
      } else {
        setUnidades([]);
      }
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
    setDescripcion('');
    setCantidad('');
    setCostoUnitario('');
    setEstado(true);
    if (unidades.length > 0) {
      setUnidadMedida(unidades.find((u) => u.codigo === 'UN')?.codigo ?? unidades[0]!.codigo);
    } else {
      setUnidadMedida('');
    }

    setEditError(null);
    setEditOpen(true);
  }

  function openEdit(item: Insumo) {
    if (!canEdit) return;
    setEditing(item);
    setNombre(item.nombre);
    setDescripcion(item.descripcion ?? '');
    setCantidad(item.cantidad === 0 ? '' : String(item.cantidad));
    setCostoUnitario(item.costoUnitario === 0 ? '' : String(item.costoUnitario));
    setUnidadMedida(item.unidadMedida ?? '');
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

    const payload: InsumoRequest = {
      nombre: nombre.trim(),
      estado,
      ...(descripcion.trim() ? { descripcion: descripcion.trim() } : {}),
      ...(cantidad.trim() ? { cantidad: Number(cantidad) } : {}),
      ...(costoUnitario.trim() ? { costoUnitario: Number(costoUnitario) } : {}),
      ...(unidadMedida.trim() ? { unidadMedida: unidadMedida.trim() } : {}),
    };

    try {
      if (editing) {
        await apiFetch<void>(`/api/v1/insumos/${editing.insumoId}`, {
          method: 'PUT',
          body: JSON.stringify(payload),
        });
      } else {
        await apiFetch<{ id: number }>(`/api/v1/insumos`, {
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

  function openDelete(item: Insumo) {
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
      await apiFetch<void>(`/api/v1/insumos/${deleteTarget.insumoId}`, { method: 'DELETE' });
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
        <Typography variant="h4">{nav('insumos')}</Typography>
        <Typography variant="body2" color="text.secondary">
          {tp('noPermission')}
        </Typography>
      </Stack>
    );
  }

  return (
    <Stack spacing={2}>
      <Stack direction="row" alignItems="center" justifyContent="space-between" spacing={2}>
        <Typography variant="h4">{nav('insumos')}</Typography>
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
                    <TableCell>{tf('quantity')}</TableCell>
                    <TableCell>{tf('unitCost')}</TableCell>
                    <TableCell>{tf('uomAbbr')}</TableCell>
                    <TableCell>{tf('status')}</TableCell>
                    <TableCell align="right">{tf('actions')}</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {items.map((x) => (
                    <TableRow key={x.insumoId} hover>
                      <TableCell>{x.insumoId}</TableCell>
                      <TableCell>{x.nombre}</TableCell>
                      <TableCell>{x.cantidad}</TableCell>
                      <TableCell>{x.costoUnitario}</TableCell>
                      <TableCell>{x.unidadMedida}</TableCell>
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

      <Dialog open={editOpen} onClose={closeEdit} fullWidth maxWidth="md">
        <DialogTitle>
          {editing ? `${tc('edit')} — ${nav('insumos')}` : `${tcrud('new')} — ${nav('insumos')}`}
        </DialogTitle>
        <DialogContent dividers>
          <Stack spacing={2} sx={{ pt: 1 }}>
            {editError ? <Alert severity="error">{editError}</Alert> : null}
            <TextField label={tf('name')} value={nombre} onChange={(e) => setNombre(e.target.value)} fullWidth autoFocus />
            <TextField
              label={tf('unitOfMeasure')}
              value={unidadMedida}
              onChange={(e) => setUnidadMedida(e.target.value)}
              select
              fullWidth
            >
              {unidades.map((u) => (
                <MenuItem key={u.codigo} value={u.codigo}>
                  {u.codigo} — {u.nombre}
                </MenuItem>
              ))}
            </TextField>
            <TextField
              label={tf('description')}
              value={descripcion}
              onChange={(e) => setDescripcion(e.target.value)}
              fullWidth
            />
            <TextField
              label={tf('quantityOptional')}
              value={cantidad}
              onChange={(e) => setCantidad(e.target.value)}
              type="number"
              inputProps={{ step: 0.001 }}
              fullWidth
            />
            <TextField
              label={tf('unitCost')}
              value={costoUnitario}
              onChange={(e) => setCostoUnitario(e.target.value)}
              type="number"
              inputProps={{ step: 0.0001, min: 0 }}
              fullWidth
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
            <Typography variant="body2">{tcrud('confirmDeleteSupply')}</Typography>
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
