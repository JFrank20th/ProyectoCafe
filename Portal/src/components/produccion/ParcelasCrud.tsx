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

type Finca = {
  fincaId: number;
  nombre: string;
  estado: boolean;
};

type Producto = {
  productoId: number;
  nombre: string;
  unidadMedida: string;
  estado: boolean;
};

type Parcela = {
  parcelaId: number;
  fincaId: number;
  productoCosechaId?: number | null;
  productoCosechaNombre?: string | null;
  fincaNombre: string;
  nombre: string;
  descripcion?: string | null;
  cultivo?: string | null;
  areaHectareas?: number | null;
  estado: boolean;
};

type ParcelaRequest = {
  fincaId: number;
  productoCosechaId: number;
  nombre: string;
  descripcion?: string;
  cultivo?: string;
  areaHectareas?: number;
  estado: boolean;
};

function parseOptionalNumber(value: string) {
  const trimmed = value.trim();
  if (!trimmed) return undefined;
  return Number(trimmed);
}

export function ParcelasCrud() {
  const nav = useTranslations('navigation');
  const tc = useTranslations('common');
  const tf = useTranslations('fields');
  const tp = useTranslations('pages');
  const tcrud = useTranslations('crud');
  const tproduction = useTranslations('production');

  const { has } = useAuth();
  const canView = has('Produccion.Parcelas', 'Ver');
  const canCreate = has('Produccion.Parcelas', 'Crear');
  const canEdit = has('Produccion.Parcelas', 'Editar');
  const canDelete = has('Produccion.Parcelas', 'Eliminar');

  const [items, setItems] = useState<Parcela[]>([]);
  const [fincas, setFincas] = useState<Finca[]>([]);
  const [productos, setProductos] = useState<Producto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [editOpen, setEditOpen] = useState(false);
  const [editError, setEditError] = useState<string | null>(null);
  const [editSaving, setEditSaving] = useState(false);
  const [editing, setEditing] = useState<Parcela | null>(null);

  const [fincaId, setFincaId] = useState('');
  const [productoCosechaId, setProductoCosechaId] = useState('');
  const [nombre, setNombre] = useState('');
  const [descripcion, setDescripcion] = useState('');
  const [cultivo, setCultivo] = useState('');
  const [areaHectareas, setAreaHectareas] = useState('');
  const [estado, setEstado] = useState(true);

  const [deleteOpen, setDeleteOpen] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);
  const [deleteSaving, setDeleteSaving] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState<Parcela | null>(null);

  const activeFincas = fincas.filter((item) => item.estado);
  const activeProductos = productos.filter((item) => item.estado);

  async function reload() {
    setLoading(true);
    setError(null);
    try {
      const [parcelasRes, fincasRes, productosRes] = await Promise.allSettled([
        apiFetch<Parcela[]>('/api/v1/produccion/parcelas'),
        apiFetch<Finca[]>('/api/v1/produccion/fincas'),
        apiFetch<Producto[]>('/api/v1/productos'),
      ]);

      if (parcelasRes.status === 'fulfilled') {
        setItems(parcelasRes.value);
      } else {
        setItems([]);
        setError(parcelasRes.reason instanceof Error ? parcelasRes.reason.message : tcrud('errorLoadingData'));
      }

      const fincasData = fincasRes.status === 'fulfilled' ? fincasRes.value : [];
      const productosData = productosRes.status === 'fulfilled' ? productosRes.value : [];
      setFincas(fincasData);
      setProductos(productosData);

      if (!fincaId && fincasData.some((item) => item.estado)) {
        setFincaId(String(fincasData.find((item) => item.estado)?.fincaId ?? ''));
      }

      if (!productoCosechaId && productosData.some((item) => item.estado)) {
        setProductoCosechaId(String(productosData.find((item) => item.estado)?.productoId ?? ''));
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

  function resetForm() {
    setFincaId(String(activeFincas[0]?.fincaId ?? ''));
    setProductoCosechaId(String(activeProductos[0]?.productoId ?? ''));
    setNombre('');
    setDescripcion('');
    setCultivo('');
    setAreaHectareas('');
    setEstado(true);
  }

  function openCreate() {
    if (!canCreate || activeFincas.length === 0) return;
    setEditing(null);
    resetForm();
    setEditError(null);
    setEditOpen(true);
  }

  function openEdit(item: Parcela) {
    if (!canEdit) return;
    setEditing(item);
    setFincaId(String(item.fincaId));
    setProductoCosechaId(item.productoCosechaId != null ? String(item.productoCosechaId) : '');
    setNombre(item.nombre ?? '');
    setDescripcion(item.descripcion ?? '');
    setCultivo(item.cultivo ?? '');
    setAreaHectareas(item.areaHectareas != null ? String(item.areaHectareas) : '');
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
    if (!fincaId) {
      setEditError(tproduction('missingActiveFarmsForPlots'));
      return;
    }

    if (!productoCosechaId) {
      setEditError(tproduction('plotHarvestProductRequired'));
      return;
    }

    setEditSaving(true);
    setEditError(null);

    const area = parseOptionalNumber(areaHectareas);
    const payload: ParcelaRequest = {
      fincaId: Number(fincaId),
      productoCosechaId: Number(productoCosechaId),
      nombre: nombre.trim(),
      estado,
      ...(descripcion.trim() ? { descripcion: descripcion.trim() } : {}),
      ...(cultivo.trim() ? { cultivo: cultivo.trim() } : {}),
      ...(area !== undefined ? { areaHectareas: area } : {}),
    };

    try {
      if (editing) {
        await apiFetch<void>(`/api/v1/produccion/parcelas/${editing.parcelaId}`, {
          method: 'PUT',
          body: JSON.stringify(payload),
        });
      } else {
        await apiFetch<{ id: number }>('/api/v1/produccion/parcelas', {
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

  function openDelete(item: Parcela) {
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
      await apiFetch<void>(`/api/v1/produccion/parcelas/${deleteTarget.parcelaId}`, { method: 'DELETE' });
      setDeleteOpen(false);
      setDeleteTarget(null);
      await reload();
    } catch (e) {
      setDeleteError(e instanceof Error ? e.message : tcrud('errorDeleting'));
    } finally {
      setDeleteSaving(false);
    }
  }

  const canCreateRecord = canCreate && activeFincas.length > 0 && activeProductos.length > 0;
  const canSave = Boolean(fincaId) && Boolean(productoCosechaId) && Boolean(nombre.trim());

  if (!canView) {
    return (
      <Stack spacing={1}>
        <Typography variant="h4">{nav('parcelas')}</Typography>
        <Typography variant="body2" color="text.secondary">
          {tp('noPermission')}
        </Typography>
      </Stack>
    );
  }

  return (
    <Stack spacing={2}>
      <Stack direction="row" alignItems="center" justifyContent="space-between" spacing={2}>
        <Typography variant="h4">{nav('parcelas')}</Typography>
        <Stack direction="row" spacing={1}>
          <Button variant="outlined" onClick={() => void reload()} disabled={loading}>
            {tc('refresh')}
          </Button>
          <Button variant="contained" onClick={openCreate} disabled={!canCreateRecord}>
            {tcrud('new')}
          </Button>
        </Stack>
      </Stack>

      {error ? <Alert severity="error">{error}</Alert> : null}
      {!loading && activeFincas.length === 0 ? <Alert severity="warning">{tproduction('missingActiveFarmsForPlots')}</Alert> : null}
      {!loading && activeProductos.length === 0 ? <Alert severity="warning">{tproduction('missingActiveProductsForPlots')}</Alert> : null}

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
                    <TableCell>{tf('farm')}</TableCell>
                    <TableCell>{tf('product')}</TableCell>
                    <TableCell>{tf('crop')}</TableCell>
                    <TableCell>{tf('areaHectares')}</TableCell>
                    <TableCell>{tf('status')}</TableCell>
                    <TableCell align="right">{tf('actions')}</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {items.map((item) => (
                    <TableRow key={item.parcelaId} hover>
                      <TableCell>{item.parcelaId}</TableCell>
                      <TableCell>{item.nombre}</TableCell>
                      <TableCell>{item.fincaNombre}</TableCell>
                      <TableCell>{item.productoCosechaNombre || '-'}</TableCell>
                      <TableCell>{item.cultivo || '-'}</TableCell>
                      <TableCell>{item.areaHectareas ?? '-'}</TableCell>
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
          {activeProductos.length === 0 ? <Alert severity="warning">{tproduction('missingActiveProductsForPlots')}</Alert> : null}
          <TextField select label={tf('farm')} value={fincaId} onChange={(e) => setFincaId(e.target.value)} required>
            {activeFincas.map((item) => (
              <MenuItem key={item.fincaId} value={String(item.fincaId)}>
                {item.nombre}
              </MenuItem>
            ))}
          </TextField>
          <TextField select label={tf('product')} value={productoCosechaId} onChange={(e) => setProductoCosechaId(e.target.value)} required>
            {activeProductos.map((item) => (
              <MenuItem key={item.productoId} value={String(item.productoId)}>
                {item.nombre} ({item.unidadMedida})
              </MenuItem>
            ))}
          </TextField>
          <TextField label={tf('name')} value={nombre} onChange={(e) => setNombre(e.target.value)} required autoFocus />
          <TextField label={tf('description')} value={descripcion} onChange={(e) => setDescripcion(e.target.value)} multiline minRows={2} />
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
            <TextField fullWidth label={tf('crop')} value={cultivo} onChange={(e) => setCultivo(e.target.value)} />
            <TextField
              fullWidth
              label={tf('areaHectares')}
              value={areaHectareas}
              onChange={(e) => setAreaHectareas(e.target.value)}
              type="number"
              slotProps={{ htmlInput: { min: 0, step: '0.01' } }}
            />
          </Stack>
          <FormControlLabel control={<Checkbox checked={estado} onChange={(e) => setEstado(e.target.checked)} />} label={tf('active')} />
        </DialogContent>
        <DialogActions>
          <Button onClick={closeEdit} disabled={editSaving}>
            {tc('cancel')}
          </Button>
          <Button onClick={() => void saveEdit()} disabled={editSaving || !canSave || (!editing && !canCreateRecord)} variant="contained">
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