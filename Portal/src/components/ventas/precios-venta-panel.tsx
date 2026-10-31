'use client';

import { useEffect, useState } from 'react';

import { useLocale, useTranslations } from '@/hooks/useTranslations';
import { apiFetch } from '@/lib/api/client';
import { type PresentacionCafe } from '@/lib/inventario/cafe';
import {
  type PrecioVenta,
  type PrecioVentaSaved,
} from '@/lib/ventas/precios';

import Alert from '@mui/material/Alert';
import Button from '@mui/material/Button';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import CardHeader from '@mui/material/CardHeader';
import Chip from '@mui/material/Chip';
import Stack from '@mui/material/Stack';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import Typography from '@mui/material/Typography';

import { PrecioVentaDialog } from './precio-venta-dialog';

export type PreciosVentaPanelProps = {
  canView: boolean;
  canCreate: boolean;
  canEdit: boolean;
};

export function PreciosVentaPanel({
  canView,
  canCreate,
  canEdit,
}: PreciosVentaPanelProps) {
  const tc = useTranslations('common');
  const tf = useTranslations('fields');
  const ts = useTranslations('sales');
  const locale = useLocale();

  const [items, setItems] = useState<PrecioVenta[]>([]);
  const [presentaciones, setPresentaciones] = useState<PresentacionCafe[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingItem, setEditingItem] = useState<PrecioVenta | null>(null);

  async function loadPrecios() {
    if (!canView) return;
    setLoading(true);
    setError(null);
    try {
      const list = await apiFetch<PrecioVenta[]>('/api/v1/ventas/precios');
      setItems(list);
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : ts('errorLoadingPrecios'));
    } finally {
      setLoading(false);
    }
  }

  async function loadPresentaciones() {
    try {
      const list = await apiFetch<PresentacionCafe[]>('/api/v1/inventario/presentaciones');
      setPresentaciones(list);
    } catch {
      // Opcional para el filtro; no bloquea la tabla.
    }
  }

  useEffect(() => {
    void loadPrecios();
    void loadPresentaciones();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [canView]);

  function openCreate() {
    setEditingItem(null);
    setDialogOpen(true);
  }

  function openEdit(item: PrecioVenta) {
    setEditingItem(item);
    setDialogOpen(true);
  }

  async function toggleEstado(item: PrecioVenta) {
    if (!canEdit) return;
    try {
      await apiFetch<void>(`/api/v1/ventas/precios/${item.precioVentaId}`, {
        method: 'PUT',
        body: JSON.stringify({
          nombre: item.nombre,
          presentacionCafeId: item.presentacionCafeId,
          precio: item.precio,
          estado: item.estado === 1 ? 2 : 1,
        }),
      });
      setSuccess(ts('precioUpdated'));
      await loadPrecios();
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : ts('errorUpdatingPrecio'));
    }
  }

  function onSaved(result: PrecioVentaSaved) {
    setSuccess(
      ts(result.created ? 'precioCreated' : 'precioUpdated', { name: result.nombre }),
    );
    void loadPrecios();
  }

  if (!canView) {
    return <Alert severity="info">{ts('preciosNoPermission')}</Alert>;
  }

  return (
    <Card variant="outlined">
      <CardHeader
        title={ts('preciosTitle')}
        subheader={ts('preciosCount', { count: items.length })}
        action={
          <Button variant="contained" size="small" onClick={openCreate} disabled={!canCreate}>
            {ts('newPrecio')}
          </Button>
        }
      />
      <CardContent sx={{ pt: 0 }}>
        <Stack spacing={2}>
          {success ? (
            <Alert severity="success" onClose={() => setSuccess(null)}>
              {success}
            </Alert>
          ) : null}
          {error ? <Alert severity="error">{error}</Alert> : null}

          {loading ? (
            <Typography variant="body2" color="text.secondary">
              {tc('loading')}
            </Typography>
          ) : items.length === 0 ? (
            <Alert severity="info">{ts('noPrecios')}</Alert>
          ) : (
            <TableContainer>
              <Table size="small" sx={{ minWidth: 720 }}>
                <TableHead>
                  <TableRow>
                    <TableCell>{tf('name')}</TableCell>
                    <TableCell>{tf('presentation')}</TableCell>
                    <TableCell align="right">{ts('priceLabel')}</TableCell>
                    <TableCell>{tf('status')}</TableCell>
                    <TableCell>{tf('updatedAt')}</TableCell>
                    {canEdit ? <TableCell align="right">{tf('actions')}</TableCell> : null}
                  </TableRow>
                </TableHead>
                <TableBody>
                  {items.map((item) => (
                    <TableRow key={item.precioVentaId} hover>
                      <TableCell>
                        <Typography variant="body2" sx={{ fontWeight: 600 }}>
                          {item.nombre}
                        </Typography>
                      </TableCell>
                      <TableCell>
                        <Stack spacing={0.25}>
                          <Typography variant="body2">{item.presentacionNombre}</Typography>
                          <Typography variant="caption" color="text.secondary">
                            {item.presentacionCodigo}
                          </Typography>
                        </Stack>
                      </TableCell>
                      <TableCell align="right">
                        {new Intl.NumberFormat(locale, {
                          style: 'currency',
                          currency: 'COP',
                          maximumFractionDigits: 0,
                        }).format(item.precio)}
                      </TableCell>
                      <TableCell>
                        <Chip
                          size="small"
                          color={item.estado === 1 ? 'success' : 'default'}
                          label={item.estado === 1 ? ts('statusActive') : ts('statusInactive')}
                        />
                      </TableCell>
                      <TableCell>
                        {item.actualizadoEn
                          ? new Date(item.actualizadoEn).toLocaleString(locale)
                          : '-'}
                      </TableCell>
                      {canEdit ? (
                        <TableCell align="right">
                          <Stack
                            direction="row"
                            spacing={1}
                            justifyContent="flex-end"
                            useFlexGap
                            flexWrap="wrap"
                          >
                            <Button size="small" variant="outlined" onClick={() => openEdit(item)}>
                              {tc('edit')}
                            </Button>
                            <Button
                              size="small"
                              color={item.estado === 1 ? 'warning' : 'success'}
                              variant="outlined"
                              onClick={() => void toggleEstado(item)}
                            >
                              {item.estado === 1 ? ts('deactivate') : ts('activate')}
                            </Button>
                          </Stack>
                        </TableCell>
                      ) : null}
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          )}
        </Stack>
      </CardContent>

      <PrecioVentaDialog
        open={dialogOpen}
        item={editingItem}
        presentaciones={presentaciones}
        onClose={() => setDialogOpen(false)}
        onSaved={onSaved}
      />
    </Card>
  );
}