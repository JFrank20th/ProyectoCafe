'use client';

import { useEffect, useState } from 'react';

import Alert from '@mui/material/Alert';
import Button from '@mui/material/Button';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import CardHeader from '@mui/material/CardHeader';
import Chip from '@mui/material/Chip';
import Checkbox from '@mui/material/Checkbox';
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

type Costo = {
  costoId: number;
  nombre: string;
  tipoCosto: string;
  fuenteModulo?: string | null;
  referenciaId?: number | null;
  cultivoId?: number | null;
  cultivoNombre?: string | null;
  fincaId?: number | null;
  fincaNombre?: string | null;
  loteId?: number | null;
  codigoLote?: string | null;
  monto: number;
  fecha?: string | null;
  estado: boolean;
};

type CostoRequest = {
  nombre: string;
  monto: number;
  fecha?: string;
  estado: boolean;
};

function toInputDate(value?: string | null) {
  if (!value) return '';
  return value.slice(0, 10);
}

function isManualCost(item: Costo) {
  return item.tipoCosto === 'Manual' && !item.fuenteModulo;
}

function mapCostTypeLabel(value: string, tproduction: (key: string) => string) {
  switch (value) {
    case 'OperacionAgricola':
      return tproduction('costTypeOperacionAgricola');
    case 'CosechaCafe':
      return tproduction('costTypeCosechaCafe');
    case 'ProcesoInventario':
      return tproduction('costTypeProcesoInventario');
    default:
      return tproduction('costTypeManual');
  }
}

function mapSourceModuleLabel(value: string | null | undefined, tproduction: (key: string) => string) {
  switch (value) {
    case 'Produccion.OperacionesAgricolas':
      return tproduction('sourceModuleOperacionesAgricolas');
    case 'Produccion.CosechasCafe':
      return tproduction('sourceModuleCosechasCafe');
    case 'Inventario.ProcesosInventario':
      return tproduction('sourceModuleProcesosInventario');
    default:
      return '—';
  }
}

function buildRelationLabel(item: Costo) {
  const parts: string[] = [];

  if (item.fincaNombre || item.cultivoNombre) {
    parts.push([item.fincaNombre, item.cultivoNombre].filter(Boolean).join(' / '));
  }

  if (item.codigoLote) {
    parts.push(item.codigoLote);
  }

  return parts.length > 0 ? parts.join(' · ') : '—';
}

export function CostosCrud() {
  const nav = useTranslations('navigation');
  const tc = useTranslations('common');
  const tf = useTranslations('fields');
  const tp = useTranslations('pages');
  const tcrud = useTranslations('crud');
  const tproduction = useTranslations('production');

  const { has } = useAuth();
  const canView = has('Contabilidad.Costos', 'Ver');
  const canCreate = has('Contabilidad.Costos', 'Crear');
  const canEdit = has('Contabilidad.Costos', 'Editar');
  const canDelete = has('Contabilidad.Costos', 'Eliminar');

  const [items, setItems] = useState<Costo[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [editOpen, setEditOpen] = useState(false);
  const [editError, setEditError] = useState<string | null>(null);
  const [editSaving, setEditSaving] = useState(false);
  const [editing, setEditing] = useState<Costo | null>(null);

  const [nombre, setNombre] = useState('');
  const [monto, setMonto] = useState('0');
  const [fecha, setFecha] = useState('');
  const [estado, setEstado] = useState(true);

  const [deleteOpen, setDeleteOpen] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);
  const [deleteSaving, setDeleteSaving] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState<Costo | null>(null);

  async function reload() {
    setLoading(true);
    setError(null);
    try {
      const data = await apiFetch<Costo[]>('/api/v1/costos');
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

  function openEdit(item: Costo) {
    if (!canEdit || !isManualCost(item)) return;
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

    const payload: CostoRequest = {
      nombre: nombre.trim(),
      monto: Number(monto),
      estado,
      ...(fecha ? { fecha: new Date(fecha).toISOString() } : {}),
    };

    try {
      if (editing) {
        await apiFetch<void>(`/api/v1/costos/${editing.costoId}`, {
          method: 'PUT',
          body: JSON.stringify(payload),
        });
      } else {
        await apiFetch<{ id: number }>('/api/v1/costos', {
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

  function openDelete(item: Costo) {
    if (!canDelete || !isManualCost(item)) return;
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
      await apiFetch<void>(`/api/v1/costos/${deleteTarget.costoId}`, { method: 'DELETE' });
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
        <Typography variant="h4">{nav('costos')}</Typography>
        <Typography variant="body2" color="text.secondary">
          {tp('noPermission')}
        </Typography>
      </Stack>
    );
  }

  return (
    <Stack spacing={2}>
      <Stack direction="row" alignItems="center" justifyContent="space-between" spacing={2}>
        <Typography variant="h4">{nav('costos')}</Typography>
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
                    <TableCell>{tf('costType')}</TableCell>
                    <TableCell>{tf('source')}</TableCell>
                    <TableCell>{tf('relation')}</TableCell>
                    <TableCell>{tf('amount')}</TableCell>
                    <TableCell>{tf('date')}</TableCell>
                    <TableCell>{tf('status')}</TableCell>
                    <TableCell align="right">{tf('actions')}</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {items.map((item) => {
                    const manual = isManualCost(item);
                    return (
                      <TableRow key={item.costoId} hover>
                        <TableCell>{item.costoId}</TableCell>
                        <TableCell>{item.nombre}</TableCell>
                        <TableCell>{mapCostTypeLabel(item.tipoCosto, tproduction)}</TableCell>
                        <TableCell>
                          {item.fuenteModulo ? (
                            <Stack spacing={0.25}>
                              <Typography variant="body2">{mapSourceModuleLabel(item.fuenteModulo, tproduction)}</Typography>
                              {item.referenciaId ? <Typography variant="caption" color="text.secondary">#{item.referenciaId}</Typography> : null}
                            </Stack>
                          ) : (
                            '—'
                          )}
                        </TableCell>
                        <TableCell>{buildRelationLabel(item)}</TableCell>
                        <TableCell>{item.monto}</TableCell>
                        <TableCell>{toInputDate(item.fecha) || '—'}</TableCell>
                        <TableCell>
                          <Chip size="small" color={item.estado ? 'success' : 'default'} label={item.estado ? tf('active') : tf('inactive')} />
                        </TableCell>
                        <TableCell align="right">
                          <Stack direction="row" spacing={1} justifyContent="flex-end">
                            <Button size="small" variant="outlined" onClick={() => openEdit(item)} disabled={!canEdit || !manual}>
                              {tc('edit')}
                            </Button>
                            <Button size="small" color="error" variant="outlined" onClick={() => openDelete(item)} disabled={!canDelete || !manual}>
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

      <Dialog open={editOpen} onClose={closeEdit} fullWidth maxWidth="sm">
        <DialogTitle>{editing ? `${tc('edit')} — ${nav('costos')}` : `${tcrud('new')} — ${nav('costos')}`}</DialogTitle>
        <DialogContent dividers>
          <Stack spacing={2} sx={{ pt: 1 }}>
            {editError ? <Alert severity="error">{editError}</Alert> : null}
            <TextField label={tf('name')} value={nombre} onChange={(e) => setNombre(e.target.value)} fullWidth autoFocus />
            <TextField label={tf('amount')} value={monto} onChange={(e) => setMonto(e.target.value)} type="number" inputProps={{ step: 0.01 }} fullWidth />
            <TextField label={tf('date')} value={fecha} onChange={(e) => setFecha(e.target.value)} type="date" fullWidth InputLabelProps={{ shrink: true }} />
            <FormControlLabel control={<Checkbox checked={estado} onChange={(e) => setEstado(e.target.checked)} />} label={tf('active')} />
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={closeEdit} disabled={editSaving}>{tc('cancel')}</Button>
          <Button onClick={() => void saveEdit()} variant="contained" disabled={editSaving || !(editing ? canEdit : canCreate)}>{tc('save')}</Button>
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
          <Button onClick={closeDelete} disabled={deleteSaving}>{tc('cancel')}</Button>
          <Button onClick={() => void confirmDelete()} color="error" variant="contained" disabled={deleteSaving || !deleteTarget || !canDelete}>{tc('delete')}</Button>
        </DialogActions>
      </Dialog>
    </Stack>
  );
}