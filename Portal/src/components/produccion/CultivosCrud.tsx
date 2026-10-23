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

type UnidadMedida = {
  codigo: string;
  nombre: string;
  estado: boolean;
};

type CultivoConfiguracionCosecha = {
  cultivoConfiguracionCosechaId: number;
  nombreTipoCosecha: string;
  unidadMedidaCodigo: string;
  unidadMedidaNombre: string;
};

type CultivoConfiguracionCosechaRequest = {
  nombreTipoCosecha?: string;
  unidadMedidaCodigo?: string;
};

type Cultivo = {
  cultivoId: number;
  fincaId: number;
  configuracionesCosecha: CultivoConfiguracionCosecha[];
  fincaNombre: string;
  nombre: string;
  descripcion?: string | null;
  areaHectareas?: number | null;
  estado: boolean;
};

type CultivoRequest = {
  fincaId: number;
  configuracionesCosecha: CultivoConfiguracionCosechaRequest[];
  nombre: string;
  descripcion?: string;
  areaHectareas?: number;
  estado: boolean;
};

type ConfiguracionForm = {
  nombreTipoCosecha: string;
  unidadMedidaCodigo: string;
};

function parseOptionalNumber(value: string) {
  const trimmed = value.trim();
  if (!trimmed) return undefined;
  return Number(trimmed);
}

const selectMenuProps = {
  PaperProps: {
    sx: {
      maxHeight: 320,
    },
  },
};

export function CultivosCrud() {
  const nav = useTranslations('navigation');
  const tc = useTranslations('common');
  const tf = useTranslations('fields');
  const tp = useTranslations('pages');
  const tcrud = useTranslations('crud');
  const tproduction = useTranslations('production');

  const { has } = useAuth();
  const canView = has('Produccion.Cultivos', 'Ver');
  const canCreate = has('Produccion.Cultivos', 'Crear');
  const canEdit = has('Produccion.Cultivos', 'Editar');
  const canDelete = has('Produccion.Cultivos', 'Eliminar');

  const [items, setItems] = useState<Cultivo[]>([]);
  const [fincas, setFincas] = useState<Finca[]>([]);
  const [unidades, setUnidades] = useState<UnidadMedida[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [editOpen, setEditOpen] = useState(false);
  const [editError, setEditError] = useState<string | null>(null);
  const [editSaving, setEditSaving] = useState(false);
  const [editing, setEditing] = useState<Cultivo | null>(null);

  const [fincaId, setFincaId] = useState('');
  const [nombre, setNombre] = useState('');
  const [descripcion, setDescripcion] = useState('');
  const [areaHectareas, setAreaHectareas] = useState('');
  const [estado, setEstado] = useState(true);
  const [configuracionesCosecha, setConfiguracionesCosecha] = useState<ConfiguracionForm[]>([{ nombreTipoCosecha: '', unidadMedidaCodigo: '' }]);

  const [deleteOpen, setDeleteOpen] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);
  const [deleteSaving, setDeleteSaving] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState<Cultivo | null>(null);

  const activeFincas = fincas.filter((item) => item.estado);
  const activeUnidades = unidades.filter((item) => item.estado);

  async function reload() {
    setLoading(true);
    setError(null);
    try {
      const [cultivosRes, fincasRes, unidadesRes] = await Promise.allSettled([
        apiFetch<Cultivo[]>('/api/v1/produccion/cultivos'),
        apiFetch<Finca[]>('/api/v1/produccion/fincas'),
        apiFetch<UnidadMedida[]>('/api/v1/unidades-medida'),
      ]);

      if (cultivosRes.status === 'fulfilled') {
        setItems(cultivosRes.value);
      } else {
        setItems([]);
        setError(cultivosRes.reason instanceof Error ? cultivosRes.reason.message : tcrud('errorLoadingData'));
      }

      const fincasData = fincasRes.status === 'fulfilled' ? fincasRes.value : [];
      const unidadesData = unidadesRes.status === 'fulfilled' ? unidadesRes.value : [];
      setFincas(fincasData);
      setUnidades(unidadesData);
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
    setFincaId('');
    setNombre('');
    setDescripcion('');
    setAreaHectareas('');
    setEstado(true);
    setConfiguracionesCosecha([{ nombreTipoCosecha: '', unidadMedidaCodigo: '' }]);
  }

  function openCreate() {
    if (!canCreateRecord) return;
    setEditing(null);
    resetForm();
    setEditError(null);
    setEditOpen(true);
  }

  function openEdit(item: Cultivo) {
    if (!canEdit) return;
    setEditing(item);
    setFincaId(String(item.fincaId));
    setNombre(item.nombre ?? '');
    setDescripcion(item.descripcion ?? '');
    setAreaHectareas(item.areaHectareas != null ? String(item.areaHectareas) : '');
    setEstado(Boolean(item.estado));
    setConfiguracionesCosecha(
      item.configuracionesCosecha.length > 0
        ? item.configuracionesCosecha.map((config) => ({
            nombreTipoCosecha: config.nombreTipoCosecha,
            unidadMedidaCodigo: config.unidadMedidaCodigo,
          }))
        : [{ nombreTipoCosecha: '', unidadMedidaCodigo: '' }],
    );
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

    const normalizedConfigurations = configuracionesCosecha
      .map((item) => ({
        nombreTipoCosecha: item.nombreTipoCosecha.trim() || undefined,
        unidadMedidaCodigo: item.unidadMedidaCodigo.trim().toUpperCase() || undefined,
      }))
      .filter((item) => item.nombreTipoCosecha && item.unidadMedidaCodigo);

    if (normalizedConfigurations.length === 0) {
      setEditError(tproduction('plotHarvestProductRequired'));
      return;
    }

    const area = parseOptionalNumber(areaHectareas);
    const hasAllRequiredFields = Boolean(nombre.trim()) && Boolean(descripcion.trim()) && area !== undefined && !Number.isNaN(area);
    if (!hasAllRequiredFields) {
      setEditError(tproduction('plotRequiredFields'));
      return;
    }

    setEditSaving(true);
    setEditError(null);

    const payload: CultivoRequest = {
      fincaId: Number(fincaId),
      configuracionesCosecha: normalizedConfigurations,
      nombre: nombre.trim(),
      descripcion: descripcion.trim(),
      areaHectareas: area,
      estado,
    };

    try {
      if (editing) {
        await apiFetch<void>(`/api/v1/produccion/cultivos/${editing.cultivoId}`, {
          method: 'PUT',
          body: JSON.stringify(payload),
        });
      } else {
        await apiFetch<{ id: number }>('/api/v1/produccion/cultivos', {
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

  function openDelete(item: Cultivo) {
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
      await apiFetch<void>(`/api/v1/produccion/cultivos/${deleteTarget.cultivoId}`, { method: 'DELETE' });
      setDeleteOpen(false);
      setDeleteTarget(null);
      await reload();
    } catch (e) {
      setDeleteError(e instanceof Error ? e.message : tcrud('errorDeleting'));
    } finally {
      setDeleteSaving(false);
    }
  }

  const canCreateRecord = canCreate && activeFincas.length > 0 && activeUnidades.length > 0;
  const parsedArea = parseOptionalNumber(areaHectareas);
  const hasValidConfigurations = configuracionesCosecha.some((item) => Boolean(item.nombreTipoCosecha.trim()) && Boolean(item.unidadMedidaCodigo));
  const canSave =
    Boolean(fincaId) &&
    hasValidConfigurations &&
    Boolean(nombre.trim()) &&
    Boolean(descripcion.trim()) &&
    parsedArea !== undefined &&
    !Number.isNaN(parsedArea) &&
    parsedArea >= 0;

  function updateConfiguracion(index: number, changes: Partial<ConfiguracionForm>) {
    setConfiguracionesCosecha((current) => current.map((item, itemIndex) => {
      if (itemIndex !== index) {
        return item;
      }

      return { ...item, ...changes };
    }));
  }

  function addConfiguracion() {
    setConfiguracionesCosecha((current) => [...current, { nombreTipoCosecha: '', unidadMedidaCodigo: '' }]);
  }

  function removeConfiguracion(index: number) {
    setConfiguracionesCosecha((current) => current.filter((_, itemIndex) => itemIndex !== index));
  }

  function formatHarvestConfigurations(configuraciones: CultivoConfiguracionCosecha[]) {
    if (configuraciones.length === 0) {
      return '-';
    }

    return configuraciones
      .map((item) => `${item.nombreTipoCosecha} (${item.unidadMedidaCodigo})`)
      .join(', ');
  }

  const hasManyConfigurations = configuracionesCosecha.length > 3;

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
      {!loading && activeUnidades.length === 0 ? <Alert severity="warning">{tp('missingActiveUnitsForProducts')}</Alert> : null}

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
                    <TableCell>{tproduction('harvestConfigurationColumn')}</TableCell>
                    <TableCell>{tf('areaHectares')}</TableCell>
                    <TableCell>{tf('status')}</TableCell>
                    <TableCell align="right">{tf('actions')}</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {items.map((item) => (
                    <TableRow key={item.cultivoId} hover>
                      <TableCell>{item.cultivoId}</TableCell>
                      <TableCell>{item.nombre}</TableCell>
                      <TableCell>{item.fincaNombre}</TableCell>
                      <TableCell>{formatHarvestConfigurations(item.configuracionesCosecha)}</TableCell>
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

      <Dialog open={editOpen} onClose={closeEdit} fullWidth maxWidth="md">
        <DialogTitle>{editing ? tc('edit') : tcrud('create')}</DialogTitle>
        <DialogContent sx={{ display: 'grid', gap: 2, pt: '12px !important', maxHeight: '78vh', overflowY: 'auto' }}>
          {editError ? <Alert severity="error">{editError}</Alert> : null}
          <TextField
            select
            label={tf('farm')}
            value={fincaId}
            onChange={(e) => setFincaId(e.target.value)}
            required
            InputLabelProps={{ shrink: true }}
            SelectProps={{ displayEmpty: true, MenuProps: selectMenuProps }}
          >
            <MenuItem value="">{tproduction('selectFarmForPlot')}</MenuItem>
            {activeFincas.map((item) => (
              <MenuItem key={item.fincaId} value={String(item.fincaId)}>
                {item.nombre}
              </MenuItem>
            ))}
          </TextField>
          <Card variant="outlined" sx={{ borderStyle: hasManyConfigurations ? 'solid' : 'dashed' }}>
            <CardContent sx={{ display: 'grid', gap: 1.5 }}>
            <Stack direction="row" alignItems="center" justifyContent="space-between" spacing={2}>
              <Stack direction="row" spacing={1} alignItems="center">
                <Typography variant="subtitle2">{`${tproduction('rawMaterialLabel')} + ${tf('unitOfMeasure')}`}</Typography>
                <Chip size="small" color="default" label={String(configuracionesCosecha.length)} />
              </Stack>
              <Button variant="outlined" size="small" onClick={addConfiguracion} disabled={activeUnidades.length === 0}>
                {tc('addLine')}
              </Button>
            </Stack>
            <Stack spacing={1.5} sx={{ maxHeight: hasManyConfigurations ? 280 : 'none', overflowY: hasManyConfigurations ? 'auto' : 'visible', pr: hasManyConfigurations ? 1 : 0 }}>
            {configuracionesCosecha.map((item, index) => (
              <Card key={`${index}-${item.nombreTipoCosecha}-${item.unidadMedidaCodigo}`} variant="outlined" sx={{ bgcolor: 'background.default' }}>
                <CardContent sx={{ display: 'grid', gap: 1.5 }}>
                <Stack direction="row" alignItems="center" justifyContent="space-between" spacing={2}>
                  <Typography variant="caption" color="text.secondary">#{index + 1}</Typography>
                  <Button color="error" variant="outlined" size="small" onClick={() => removeConfiguracion(index)} disabled={configuracionesCosecha.length === 1}>
                    {tc('delete')}
                  </Button>
                </Stack>
                <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
                <TextField
                  fullWidth
                  label={tproduction('rawMaterialLabel')}
                  value={item.nombreTipoCosecha}
                  onChange={(e) => updateConfiguracion(index, { nombreTipoCosecha: e.target.value })}
                  required
                  placeholder={tproduction('selectRawMaterialForPlot')}
                />
                <TextField
                  select
                  fullWidth
                  label={tf('unitOfMeasure')}
                  value={item.unidadMedidaCodigo}
                  onChange={(e) => updateConfiguracion(index, { unidadMedidaCodigo: e.target.value })}
                  required
                  InputLabelProps={{ shrink: true }}
                  SelectProps={{ displayEmpty: true, MenuProps: selectMenuProps }}
                >
                  <MenuItem value="">{tf('unitOfMeasure')}</MenuItem>
                  {activeUnidades.map((unidad) => (
                    <MenuItem key={unidad.codigo} value={unidad.codigo}>
                      {unidad.nombre} ({unidad.codigo})
                    </MenuItem>
                  ))}
                </TextField>
                </Stack>
                </CardContent>
              </Card>
            ))}
            </Stack>
            </CardContent>
          </Card>
          <TextField label={tf('name')} value={nombre} onChange={(e) => setNombre(e.target.value)} required autoFocus />
          <TextField label={tf('description')} value={descripcion} onChange={(e) => setDescripcion(e.target.value)} multiline minRows={2} required />
          <TextField
            fullWidth
            label={tf('areaHectares')}
            value={areaHectareas}
            onChange={(e) => setAreaHectareas(e.target.value)}
            type="number"
            required
            slotProps={{ htmlInput: { min: 0, step: '0.01' } }}
          />
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