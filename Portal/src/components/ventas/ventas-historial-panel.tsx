'use client';

import { useEffect, useMemo, useState } from 'react';

import { useLocale, useTranslations } from '@/hooks/useTranslations';
import { apiFetch } from '@/lib/api/client';
import { type Cliente } from '@/lib/ventas/clientes';
import { type VentaHistorial } from '@/lib/ventas/ventas';

import Alert from '@mui/material/Alert';
import Button from '@mui/material/Button';
import Card from '@mui/material/Card';
import Chip from '@mui/material/Chip';
import CardContent from '@mui/material/CardContent';
import CardHeader from '@mui/material/CardHeader';
import FormControl from '@mui/material/FormControl';
import InputLabel from '@mui/material/InputLabel';
import MenuItem from '@mui/material/MenuItem';
import Select, { type SelectChangeEvent } from '@mui/material/Select';
import Stack from '@mui/material/Stack';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableContainer from '@mui/material/TableContainer';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import TextField from '@mui/material/TextField';
import Typography from '@mui/material/Typography';

import { VentaDetalleDialog } from './venta-detalle-dialog';

export type VentasHistorialPanelProps = {
  canView: boolean;
  canQueryMovements: boolean;
};

export function VentasHistorialPanel({ canView, canQueryMovements }: VentasHistorialPanelProps) {
  const tc = useTranslations('common');
  const tf = useTranslations('fields');
  const ts = useTranslations('sales');
  const locale = useLocale();

  const [items, setItems] = useState<VentaHistorial[]>([]);
  const [clientes, setClientes] = useState<Cliente[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [numeroFiltro, setNumeroFiltro] = useState('');
  const [clienteFiltro, setClienteFiltro] = useState<string>('all');
  const [estadoFiltro, setEstadoFiltro] = useState<string>('all');
  const [fechaDesde, setFechaDesde] = useState('');
  const [fechaHasta, setFechaHasta] = useState('');

  const [selectedVenta, setSelectedVenta] = useState<VentaHistorial | null>(null);
  const [detailOpen, setDetailOpen] = useState(false);

  async function loadVentas() {
    if (!canView) return;
    setLoading(true);
    setError(null);
    try {
      const list = await apiFetch<VentaHistorial[]>('/api/v1/ventas/ventas');
      setItems(list);
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : ts('errorLoadingVentas'));
    } finally {
      setLoading(false);
    }
  }

  async function loadClientes() {
    try {
      const list = await apiFetch<Cliente[]>('/api/v1/ventas/clientes');
      setClientes(list);
    } catch {
      // Opcional para el filtro.
    }
  }

  useEffect(() => {
    void loadVentas();
    void loadClientes();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [canView]);

  const filteredItems = useMemo(() => {
    return items.filter((venta) => {
      if (numeroFiltro.trim() && !String(venta.numeroVenta).includes(numeroFiltro.trim())) {
        return false;
      }

      if (clienteFiltro !== 'all' && String(venta.clienteId ?? '') !== clienteFiltro) {
        return false;
      }

      if (estadoFiltro !== 'all' && String(venta.estado) !== estadoFiltro) {
        return false;
      }

      if (fechaDesde) {
        const desde = new Date(fechaDesde);
        if (new Date(venta.fechaVenta) < desde) return false;
      }

      if (fechaHasta) {
        const hasta = new Date(fechaHasta);
        hasta.setHours(23, 59, 59, 999);
        if (new Date(venta.fechaVenta) > hasta) return false;
      }

      return true;
    });
  }, [items, numeroFiltro, clienteFiltro, estadoFiltro, fechaDesde, fechaHasta]);

  function estadoLabel(estado: number) {
    if (estado === 1) return ts('statusRegistered');
    if (estado === 2) return ts('statusCancelled');
    return ts('statusAnnulled');
  }

  function openDetail(venta: VentaHistorial) {
    setSelectedVenta(venta);
    setDetailOpen(true);
  }

  if (!canView) {
    return <Alert severity="info">{ts('ventasHistoryNoPermission')}</Alert>;
  }

  return (
    <Card variant="outlined">
      <CardHeader
        title={ts('salesHistoryTitle')}
        subheader={ts('salesHistoryCount', { count: filteredItems.length })}
        action={
          <Button variant="outlined" size="small" onClick={() => void loadVentas()} disabled={loading}>
            {tc('refresh')}
          </Button>
        }
      />
      <CardContent sx={{ pt: 0 }}>
        <Stack spacing={2}>
          {error ? <Alert severity="error">{error}</Alert> : null}

          <Stack
            direction={{ xs: 'column', md: 'row' }}
            spacing={2}
            alignItems={{ xs: 'stretch', md: 'center' }}
            useFlexGap
            flexWrap="wrap"
          >
            <TextField
              size="small"
              label={ts('saleNumberFilter')}
              value={numeroFiltro}
              onChange={(event) => setNumeroFiltro(event.target.value)}
              sx={{ minWidth: { xs: '100%', md: 160 } }}
            />

            <FormControl size="small" sx={{ minWidth: { xs: '100%', md: 200 } }}>
              <InputLabel id="historial-cliente-label">{ts('clientLabel')}</InputLabel>
              <Select
                labelId="historial-cliente-label"
                label={ts('clientLabel')}
                value={clienteFiltro}
                onChange={(event: SelectChangeEvent) => setClienteFiltro(event.target.value)}
              >
                <MenuItem value="all">{ts('allClients')}</MenuItem>
                {clientes.map((cliente) => (
                  <MenuItem key={cliente.clienteId} value={String(cliente.clienteId)}>
                    {cliente.nombre}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>

            <FormControl size="small" sx={{ minWidth: { xs: '100%', md: 160 } }}>
              <InputLabel id="historial-estado-label">{tf('status')}</InputLabel>
              <Select
                labelId="historial-estado-label"
                label={tf('status')}
                value={estadoFiltro}
                onChange={(event: SelectChangeEvent) => setEstadoFiltro(event.target.value)}
              >
                <MenuItem value="all">{ts('allStatuses')}</MenuItem>
                <MenuItem value="1">{ts('statusRegistered')}</MenuItem>
                <MenuItem value="2">{ts('statusCancelled')}</MenuItem>
                <MenuItem value="3">{ts('statusAnnulled')}</MenuItem>
              </Select>
            </FormControl>

            <TextField
              size="small"
              type="date"
              label={ts('dateFrom')}
              value={fechaDesde}
              onChange={(event) => setFechaDesde(event.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
              sx={{ minWidth: { xs: '100%', md: 170 } }}
            />

            <TextField
              size="small"
              type="date"
              label={ts('dateTo')}
              value={fechaHasta}
              onChange={(event) => setFechaHasta(event.target.value)}
              slotProps={{ inputLabel: { shrink: true } }}
              sx={{ minWidth: { xs: '100%', md: 170 } }}
            />
          </Stack>

          {loading ? (
            <Typography variant="body2" color="text.secondary">
              {tc('loading')}
            </Typography>
          ) : filteredItems.length === 0 ? (
            <Alert severity="info">{ts('noSalesHistory')}</Alert>
          ) : (
            <TableContainer>
              <Table size="small" sx={{ minWidth: 820 }}>
                <TableHead>
                  <TableRow>
                    <TableCell>{ts('saleNumberLabel')}</TableCell>
                    <TableCell>{tf('date')}</TableCell>
                    <TableCell>{ts('clientLabel')}</TableCell>
                    <TableCell align="right">{ts('productsCountLabel')}</TableCell>
                    <TableCell align="right">{ts('totalLabel')}</TableCell>
                    <TableCell>{tf('status')}</TableCell>
                    <TableCell align="right">{tf('actions')}</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {filteredItems.map((venta) => (
                    <TableRow key={venta.ventaId} hover>
                      <TableCell>{venta.numeroVenta}</TableCell>
                      <TableCell>{new Date(venta.fechaVenta).toLocaleString(locale)}</TableCell>
                      <TableCell>{venta.clienteNombre || ts('noCliente')}</TableCell>
                      <TableCell align="right">{venta.detalles.length}</TableCell>
                      <TableCell align="right">
                        {new Intl.NumberFormat(locale, {
                          style: 'currency',
                          currency: 'COP',
                          maximumFractionDigits: 0,
                        }).format(venta.total)}
                      </TableCell>
                      <TableCell>
                        <Chip
                          size="small"
                          color={venta.estado === 1 ? 'success' : 'default'}
                          label={estadoLabel(venta.estado)}
                        />
                      </TableCell>
                      <TableCell align="right">
                        <Button size="small" variant="outlined" onClick={() => openDetail(venta)}>
                          {ts('viewDetailLabel')}
                        </Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          )}
        </Stack>
      </CardContent>

      <VentaDetalleDialog
        open={detailOpen}
        venta={selectedVenta}
        canQueryMovements={canQueryMovements}
        onClose={() => setDetailOpen(false)}
      />
    </Card>
  );
}