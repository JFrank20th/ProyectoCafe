'use client';

import Link from 'next/link';

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

import { useTranslations } from '@/hooks/useTranslations';
import { apiFetch } from '@/lib/api/client';
import { useAuth } from '@/lib/auth/AuthProvider';

type UnidadMedida = {
  codigo: string;
  nombre: string;
  estado: boolean;
};

type TipoVenta = {
  tipoVentaId: number;
  codigo: string;
  nombre: string;
  orden: number;
  estado: boolean;
};

type ProductoPrecioTipoVenta = {
  tipoVentaId: number;
  tipoVentaCodigo: string;
  tipoVentaNombre: string;
  precio: number;
  estado: boolean;
};

type Producto = {
  productoId: number;
  nombre: string;
  descripcion?: string | null;
  precio: number;
  precioInternet: number;
  precioMayorista: number;
  estado: boolean;
  presentacionCodigo?: string | null;
  presentacionNombre?: string | null;
  presentacionCantidad?: number | null;
  presentacionUnidadMedida?: string | null;
  unidadMedida: string;
  preciosTipoVenta: ProductoPrecioTipoVenta[];
};

type ProductoRequest = {
  nombre: string;
  descripcion: string;
  precio: number;
  precioInternet: number;
  precioMayorista: number;
  estado: boolean;
  presentacionCodigo?: string | null;
  presentacionNombre?: string | null;
  presentacionCantidad?: number | null;
  presentacionUnidadMedida?: string | null;
  unidadMedida: string;
  preciosTipoVenta: Array<{
    tipoVentaId: number;
    precio: number;
    estado: boolean;
  }>;
};

type EditablePrecioTipoVenta = {
  tipoVentaId: number;
  tipoVentaCodigo: string;
  tipoVentaNombre: string;
  precio: string;
  estado: boolean;
};

function resolveLegacyPriceFromProduct(producto: Producto | null, tipoVentaCodigo: string) {
  if (!producto) {
    return 0;
  }

  switch (tipoVentaCodigo) {
    case 'Internet':
      return producto.precioInternet;
    case 'Mayorista':
      return producto.precioMayorista;
    default:
      return producto.precio;
  }
}

function resolveLegacyPriceFromRows(rows: EditablePrecioTipoVenta[], tipoVentaCodigo: string) {
  const row = rows.find((item) => item.tipoVentaCodigo === tipoVentaCodigo);
  const parsed = row ? Number(row.precio) : 0;
  return Number.isFinite(parsed) ? parsed : 0;
}

function buildPrecioRows(producto: Producto | null, tiposVenta: TipoVenta[]) {
  return tiposVenta
    .filter((item) => item.estado)
    .sort((left, right) => left.orden - right.orden || left.tipoVentaId - right.tipoVentaId)
    .map((tipoVenta) => {
      const precioActual = producto?.preciosTipoVenta.find((item) => item.tipoVentaId === tipoVenta.tipoVentaId);
      const precio = precioActual?.precio ?? resolveLegacyPriceFromProduct(producto, tipoVenta.codigo);

      return {
        tipoVentaId: tipoVenta.tipoVentaId,
        tipoVentaCodigo: tipoVenta.codigo,
        tipoVentaNombre: tipoVenta.nombre,
        precio: String(precio),
        estado: precioActual?.estado ?? true,
      } satisfies EditablePrecioTipoVenta;
    });
}

export function ProductosCrud() {
  const nav = useTranslations('navigation');
  const tc = useTranslations('common');
  const tf = useTranslations('fields');
  const tp = useTranslations('pages');
  const tcrud = useTranslations('crud');

  const { has } = useAuth();
  const canView = has('Inventario.Productos', 'Ver');
  const canCreate = has('Inventario.Productos', 'Crear');
  const canEdit = has('Inventario.Productos', 'Editar');
  const canDelete = has('Inventario.Productos', 'Eliminar');
  const canViewUnidades = has('Inventario.UnidadesMedida', 'Ver');

  const [items, setItems] = useState<Producto[]>([]);
  const [unidades, setUnidades] = useState<UnidadMedida[]>([]);
  const [tiposVenta, setTiposVenta] = useState<TipoVenta[]>([]);

  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [editOpen, setEditOpen] = useState(false);
  const [editError, setEditError] = useState<string | null>(null);
  const [editSaving, setEditSaving] = useState(false);
  const [editing, setEditing] = useState<Producto | null>(null);

  const [nombre, setNombre] = useState('');
  const [descripcion, setDescripcion] = useState('');
  const [unidadMedida, setUnidadMedida] = useState('');
  const [estado, setEstado] = useState(true);
  const [preciosTipoVenta, setPreciosTipoVenta] = useState<EditablePrecioTipoVenta[]>([]);

  const [deleteOpen, setDeleteOpen] = useState(false);
  const [deleteError, setDeleteError] = useState<string | null>(null);
  const [deleteSaving, setDeleteSaving] = useState(false);
  const [deleteTarget, setDeleteTarget] = useState<Producto | null>(null);

  const activeUnidades = unidades.filter((item) => item.estado);
  const unidadSeleccionada = unidades.find((item) => item.codigo === unidadMedida) ?? null;
  const selectableUnidades = unidadSeleccionada && !unidadSeleccionada.estado
    ? [unidadSeleccionada, ...activeUnidades.filter((item) => item.codigo !== unidadSeleccionada.codigo)]
    : activeUnidades;
  const hasValidUnidadMedida = activeUnidades.some((item) => item.codigo === unidadMedida);
  const activeTiposVenta = tiposVenta.filter((item) => item.estado);
  const setupHref = '/inventario/unidades-medida';

  async function reload() {
    setLoading(true);
    setError(null);
    try {
      const [productosRes, unidadesRes, tiposVentaRes] = await Promise.allSettled([
        apiFetch<Producto[]>('/api/v1/productos'),
        apiFetch<UnidadMedida[]>('/api/v1/unidades-medida'),
        apiFetch<TipoVenta[]>('/api/v1/tipos-venta'),
      ]);

      if (productosRes.status === 'fulfilled') {
        setItems(productosRes.value);
      } else {
        setItems([]);
        setError(productosRes.reason instanceof Error ? productosRes.reason.message : tcrud('errorLoadingData'));
      }

      const unidadesList = unidadesRes.status === 'fulfilled' ? unidadesRes.value : [];
      const tiposVentaList = tiposVentaRes.status === 'fulfilled' ? tiposVentaRes.value : [];

      setUnidades(unidadesList);
      setTiposVenta(tiposVentaList);

      if (!unidadMedida && unidadesList.some((item) => item.estado)) {
        const unidadDefault = unidadesList.find((item) => item.estado && item.codigo === 'UN')?.codigo ?? unidadesList.find((item) => item.estado)?.codigo ?? '';
        setUnidadMedida(unidadDefault);
      }

      if (preciosTipoVenta.length === 0 && tiposVentaList.some((item) => item.estado)) {
        setPreciosTipoVenta(buildPrecioRows(null, tiposVentaList));
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
    setUnidadMedida(activeUnidades.find((item) => item.codigo === 'UN')?.codigo ?? activeUnidades[0]?.codigo ?? '');
    setEstado(true);
    setPreciosTipoVenta(buildPrecioRows(null, tiposVenta));
    setEditError(null);
    setEditOpen(true);
  }

  function openEdit(item: Producto) {
    if (!canEdit) return;
    setEditing(item);
    setNombre(item.nombre);
    setDescripcion(item.descripcion ?? '');
    setUnidadMedida(item.unidadMedida ?? '');
    setEstado(Boolean(item.estado));
    setPreciosTipoVenta(buildPrecioRows(item, tiposVenta));
    setEditError(null);
    setEditOpen(true);
  }

  function closeEdit() {
    if (editSaving) return;
    setEditOpen(false);
    setEditing(null);
    setEditError(null);
  }

  function updatePrecio(tipoVentaId: number, changes: Partial<EditablePrecioTipoVenta>) {
    setPreciosTipoVenta((current) =>
      current.map((item) => (item.tipoVentaId === tipoVentaId ? { ...item, ...changes } : item))
    );
  }

  async function saveEdit() {
    setEditSaving(true);
    setEditError(null);

    const normalizedUnidadMedida = unidadMedida.trim().toUpperCase();

    if (!hasValidUnidadMedida || !normalizedUnidadMedida) {
      setEditError(tp('invalidProductUnit'));
      setEditSaving(false);
      return;
    }

    const payload: ProductoRequest = {
      nombre: nombre.trim(),
      descripcion: descripcion.trim(),
      precio: resolveLegacyPriceFromRows(preciosTipoVenta, 'Normal'),
      precioInternet: resolveLegacyPriceFromRows(preciosTipoVenta, 'Internet'),
      precioMayorista: resolveLegacyPriceFromRows(preciosTipoVenta, 'Mayorista'),
      estado,
      presentacionCodigo: null,
      presentacionNombre: null,
      presentacionCantidad: null,
      presentacionUnidadMedida: null,
      unidadMedida: normalizedUnidadMedida,
      preciosTipoVenta: preciosTipoVenta.map((item) => ({
        tipoVentaId: item.tipoVentaId,
        precio: Number(item.precio),
        estado: item.estado,
      })),
    };

    try {
      if (editing) {
        await apiFetch<void>(`/api/v1/productos/${editing.productoId}`, {
          method: 'PUT',
          body: JSON.stringify(payload),
        });
      } else {
        await apiFetch<{ id: number }>('/api/v1/productos', {
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

  function openDelete(item: Producto) {
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
      await apiFetch<void>(`/api/v1/productos/${deleteTarget.productoId}`, { method: 'DELETE' });
      setDeleteOpen(false);
      setDeleteTarget(null);
      await reload();
    } catch (e) {
      setDeleteError(e instanceof Error ? e.message : tcrud('errorDeleting'));
    } finally {
      setDeleteSaving(false);
    }
  }

  const canSave =
    Boolean(nombre.trim()) &&
    Boolean(unidadMedida.trim()) &&
    hasValidUnidadMedida &&
    preciosTipoVenta.length > 0 &&
    preciosTipoVenta.every((item) => Number(item.precio) >= 0);

  if (!canView) {
    return (
      <Stack spacing={1}>
        <Typography variant="h4">{nav('productos')}</Typography>
        <Typography variant="body2" color="text.secondary">
          {tp('noPermission')}
        </Typography>
      </Stack>
    );
  }

  return (
    <Stack spacing={2}>
      <Stack direction="row" alignItems="center" justifyContent="space-between" spacing={2}>
        <Typography variant="h4">{nav('productos')}</Typography>
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
      {!loading && activeUnidades.length === 0 ? (
        <Alert
          severity="warning"
          action={
            canViewUnidades ? (
              <Button component={Link} href={setupHref} color="inherit" size="small">
                {tc('open')}
              </Button>
            ) : null
          }
        >
          {tp('missingActiveUnitsForProducts')}
        </Alert>
      ) : null}
      {!loading && activeTiposVenta.length === 0 ? <Alert severity="warning">{tp('pendingEndpoint')}</Alert> : null}

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
                    <TableCell>{tf('uomAbbr')}</TableCell>
                    <TableCell>{tf('price')}</TableCell>
                    <TableCell>{tf('status')}</TableCell>
                    <TableCell align="right">{tf('actions')}</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {items.map((item) => (
                    <TableRow key={item.productoId} hover>
                      <TableCell>{item.productoId}</TableCell>
                      <TableCell>{item.nombre}</TableCell>
                      <TableCell>{item.unidadMedida}</TableCell>
                      <TableCell>{item.precio}</TableCell>
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

      <Dialog open={editOpen} onClose={closeEdit} fullWidth maxWidth="lg">
        <DialogTitle>{editing ? `${tc('edit')} — ${nav('productos')}` : `${tcrud('new')} — ${nav('productos')}`}</DialogTitle>
        <DialogContent dividers>
          <Stack spacing={2} sx={{ pt: 1 }}>
            {editError ? <Alert severity="error">{editError}</Alert> : null}

            <TextField label={tf('name')} value={nombre} onChange={(e) => setNombre(e.target.value)} autoFocus fullWidth />

            <TextField label={tf('description')} value={descripcion} onChange={(e) => setDescripcion(e.target.value)} fullWidth multiline minRows={2} />

            <TextField
              fullWidth
              select
              label={tf('unitOfMeasure')}
              value={unidadMedida}
              onChange={(e) => setUnidadMedida(e.target.value)}
              helperText={
                activeUnidades.length === 0
                  ? tp('missingActiveUnitsForProducts')
                  : unidadSeleccionada && !unidadSeleccionada.estado
                    ? tp('invalidProductUnit')
                    : tf('productUnitHelper')
              }
            >
              {selectableUnidades.map((unidad) => (
                <MenuItem key={unidad.codigo} value={unidad.codigo}>
                  {unidad.codigo} — {unidad.nombre}{unidad.estado ? '' : ` (${tf('inactive')})`}
                </MenuItem>
              ))}
            </TextField>

            <Card variant="outlined">
              <CardHeader title={tf('priceType')} subheader={tf('price')} />
              <CardContent sx={{ pt: 0 }}>
                <Stack spacing={2}>
                  {preciosTipoVenta.map((precio) => (
                    <Stack key={precio.tipoVentaId} direction={{ xs: 'column', md: 'row' }} spacing={2} alignItems={{ md: 'center' }}>
                      <TextField fullWidth label={tf('priceType')} value={precio.tipoVentaNombre} InputProps={{ readOnly: true }} />
                      <TextField
                        fullWidth
                        label={tf('price')}
                        value={precio.precio}
                        onChange={(e) => updatePrecio(precio.tipoVentaId, { precio: e.target.value })}
                        type="number"
                        inputProps={{ min: 0, step: 0.01 }}
                      />
                      <FormControlLabel
                        control={<Checkbox checked={precio.estado} onChange={(e) => updatePrecio(precio.tipoVentaId, { estado: e.target.checked })} />}
                        label={tf('active')}
                      />
                    </Stack>
                  ))}
                </Stack>
              </CardContent>
            </Card>

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
            <Typography variant="body2">{tcrud('confirmDeleteProduct')}</Typography>
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