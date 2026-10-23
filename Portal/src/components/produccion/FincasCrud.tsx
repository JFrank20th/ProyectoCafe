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

import { ApiError, apiFetch } from '@/lib/api/client';
import { useAuth } from '@/lib/auth/AuthProvider';
import { useTranslations } from '@/hooks/useTranslations';

type Finca = {
  fincaId: number;
  nombre: string;
  descripcion?: string | null;
  ubicacion?: string | null;
  municipio?: string | null;
  departamento?: string | null;
  areaHectareas?: number | null;
  estado: boolean;
};

type FincaRequest = {
  nombre: string;
  descripcion: string;
  ubicacion: string;
  municipio: string;
  departamento: string;
  areaHectareas?: number;
  estado: boolean;
};

function parseOptionalNumber(value: string) {
  const trimmed = value.trim();
  if (!trimmed) return undefined;
  return Number(trimmed);
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return Boolean(value) && typeof value === 'object' && !Array.isArray(value);
}

function describeApiError(err: unknown, fallback: string) {
  if (err instanceof ApiError) {
    if (err.bodyText) {
      try {
        const data: unknown = JSON.parse(err.bodyText);
        if (isRecord(data)) {
          const maybeError = data.error;
          if (isRecord(maybeError)) {
            const message = maybeError.message;
            if (typeof message === 'string' && message.trim()) {
              return message;
            }
          }

          const maybeErrors = data.errors;
          if (isRecord(maybeErrors)) {
            for (const value of Object.values(maybeErrors)) {
              if (Array.isArray(value)) {
                const first = value.find((entry) => typeof entry === 'string' && entry.trim());
                if (typeof first === 'string') {
                  return first;
                }
              }
            }
          }
        }
      } catch {
        // ignore invalid error payloads
      }
    }

    return err.message || fallback;
  }

  if (err instanceof Error) {
    return err.message;
  }

  return fallback;
}

function validateForm(values: {
  nombre: string;
  descripcion: string;
  ubicacion: string;
  municipio: string;
  departamento: string;
  areaHectareas: string;
}) {
  if (!values.nombre.trim()) return 'Nombre es requerido.';
  if (!values.descripcion.trim()) return 'Descripcion es requerida.';
  if (!values.ubicacion.trim()) return 'Ubicacion es requerida.';
  if (!values.municipio.trim()) return 'Municipio es requerido.';
  if (!values.departamento.trim()) return 'Departamento es requerido.';

  const area = parseOptionalNumber(values.areaHectareas);
  if (area !== undefined && (Number.isNaN(area) || area < 0)) {
    return 'Area no puede ser negativa.';
  }

  return null;
}

export function FincasCrud() {
  const nav = useTranslations('navigation');
  const tc = useTranslations('common');
  const tf = useTranslations('fields');
  const tp = useTranslations('pages');
  const tcrud = useTranslations('crud');

  const { has } = useAuth();
  const canView = has('Produccion.Fincas', 'Ver');
  const canCreate = has('Produccion.Fincas', 'Crear');
  const canEdit = has('Produccion.Fincas', 'Editar');
  const canDelete = has('Produccion.Fincas', 'Eliminar');

  const [items, setItems] = useState<Finca[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [editOpen, setEditOpen] = useState(false);
  const [editError, setEditError] = useState<string | null>(null);
  const [editSaving, setEditSaving] = useState(false);
  const [editing, setEditing] = useState<Finca | null>(null);

  const [nombre, setNombre] = useState('');
  const [descripcion, setDescripcion] = useState('');
  const [ubicacion, setUbicacion] = useState('');
  const [municipio, setMunicipio] = useState('');
  const [departamento, setDepartamento] = useState('');
  const [areaHectareas, setAreaHectareas] = useState('');
  const [estado, setEstado] = useState(true);

  const [deleteOpen, setDeleteOpen] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);
  const [deleteSaving, setDeleteSaving] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState<Finca | null>(null);

  async function reload() {
    setLoading(true);
    setError(null);
    try {
      const data = await apiFetch<Finca[]>('/api/v1/produccion/fincas');
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
    setDescripcion('');
    setUbicacion('');
    setMunicipio('');
    setDepartamento('');
    setAreaHectareas('');
    setEstado(true);
  }

  function openCreate() {
    if (!canCreate) return;
    setEditing(null);
    resetForm();
    setEditError(null);
    setEditOpen(true);
  }

  function openEdit(item: Finca) {
    if (!canEdit) return;
    setEditing(item);
    setNombre(item.nombre ?? '');
    setDescripcion(item.descripcion ?? '');
    setUbicacion(item.ubicacion ?? '');
    setMunicipio(item.municipio ?? '');
    setDepartamento(item.departamento ?? '');
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
    setEditSaving(true);
    setEditError(null);

    const validationError = validateForm({
      nombre,
      descripcion,
      ubicacion,
      municipio,
      departamento,
      areaHectareas,
    });

    if (validationError) {
      setEditError(validationError);
      setEditSaving(false);
      return;
    }

    const area = parseOptionalNumber(areaHectareas);
    const payload: FincaRequest = {
      nombre: nombre.trim(),
      descripcion: descripcion.trim(),
      ubicacion: ubicacion.trim(),
      municipio: municipio.trim(),
      departamento: departamento.trim(),
      estado,
      ...(area !== undefined ? { areaHectareas: area } : {}),
    };

    try {
      if (editing) {
        await apiFetch<void>(`/api/v1/produccion/fincas/${editing.fincaId}`, {
          method: 'PUT',
          body: JSON.stringify(payload),
        });
      } else {
        await apiFetch<{ id: number }>('/api/v1/produccion/fincas', {
          method: 'POST',
          body: JSON.stringify(payload),
        });
      }

      setEditOpen(false);
      setEditing(null);
      await reload();
    } catch (e) {
      setEditError(describeApiError(e, tcrud('errorSaving')));
    } finally {
      setEditSaving(false);
    }
  }

  function openDelete(item: Finca) {
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
      await apiFetch<void>(`/api/v1/produccion/fincas/${deleteTarget.fincaId}`, { method: 'DELETE' });
      setDeleteOpen(false);
      setDeleteTarget(null);
      await reload();
    } catch (e) {
      setDeleteError(describeApiError(e, tcrud('errorDeleting')));
    } finally {
      setDeleteSaving(false);
    }
  }

  if (!canView) {
    return (
      <Stack spacing={1}>
        <Typography variant="h4">{nav('fincas')}</Typography>
        <Typography variant="body2" color="text.secondary">
          {tp('noPermission')}
        </Typography>
      </Stack>
    );
  }

  return (
    <Stack spacing={2}>
      <Stack direction="row" alignItems="center" justifyContent="space-between" spacing={2}>
        <Typography variant="h4">{nav('fincas')}</Typography>
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
                    <TableCell>{tf('location')}</TableCell>
                    <TableCell>{tf('municipality')}</TableCell>
                    <TableCell>{tf('department')}</TableCell>
                    <TableCell>{tf('areaHectares')}</TableCell>
                    <TableCell>{tf('status')}</TableCell>
                    <TableCell align="right">{tf('actions')}</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {items.map((item) => (
                    <TableRow key={item.fincaId} hover>
                      <TableCell>{item.fincaId}</TableCell>
                      <TableCell>{item.nombre}</TableCell>
                      <TableCell>{item.ubicacion || '-'}</TableCell>
                      <TableCell>{item.municipio || '-'}</TableCell>
                      <TableCell>{item.departamento || '-'}</TableCell>
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
          <TextField label={tf('name')} value={nombre} onChange={(e) => setNombre(e.target.value)} required autoFocus />
          <TextField label={tf('description')} value={descripcion} onChange={(e) => setDescripcion(e.target.value)} multiline minRows={2} required />
          <TextField label={tf('location')} value={ubicacion} onChange={(e) => setUbicacion(e.target.value)} required />
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
            <TextField fullWidth label={tf('municipality')} value={municipio} onChange={(e) => setMunicipio(e.target.value)} required />
            <TextField fullWidth label={tf('department')} value={departamento} onChange={(e) => setDepartamento(e.target.value)} required />
          </Stack>
          <TextField
            label={tf('areaHectares')}
            value={areaHectareas}
            onChange={(e) => setAreaHectareas(e.target.value)}
            type="number"
            slotProps={{ htmlInput: { min: 0, step: '0.01' } }}
          />
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