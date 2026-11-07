'use client';

import { useEffect, useMemo, useState } from 'react';

import AddIcon from '@mui/icons-material/Add';
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutline';
import Alert from '@mui/material/Alert';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogTitle from '@mui/material/DialogTitle';
import FormControl from '@mui/material/FormControl';
import FormHelperText from '@mui/material/FormHelperText';
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

import { useLocale, useTranslations } from '@/hooks/useTranslations';
import { apiFetch } from '@/lib/api/client';
import {
  type ExistenciaCafe,
  type LoteCafeResumen,
  type PresentacionCafe,
  type TipoCafe,
} from '@/lib/inventario/cafe';
import { type Cliente } from '@/lib/ventas/clientes';
import { type PrecioVenta } from '@/lib/ventas/precios';

type LineaForm = {
  id: string;
  presentacionCafeId: string;
  tipoCafeId: string;
  loteCafeId: string;
  cantidad: string;
  precioVentaId: string;
};

let nextLineId = 1;

function createEmptyLine(): LineaForm {
  const id = `venta-linea-${nextLineId}`;
  nextLineId += 1;
  return {
    id,
    presentacionCafeId: '',
    tipoCafeId: '',
    loteCafeId: '',
    cantidad: '',
    precioVentaId: '',
  };
}

export type VentaFormDialogProps = {
  open: boolean;
  onClose: () => void;
  canQueryInventory: boolean;
  canLoadCatalogs: boolean;
  canLoadClientes: boolean;
  canLoadPrecios: boolean;
};

export function VentaFormDialog({
  open,
  onClose,
  canQueryInventory,
  canLoadCatalogs,
  canLoadClientes,
  canLoadPrecios,
}: VentaFormDialogProps) {
  const tc = useTranslations('common');
  const tf = useTranslations('fields');
  const ts = useTranslations('sales');
  const locale = useLocale();

  const [clientes, setClientes] = useState<Cliente[]>([]);
  const [presentaciones, setPresentaciones] = useState<PresentacionCafe[]>([]);
  const [tiposCafe, setTiposCafe] = useState<TipoCafe[]>([]);
  const [precios, setPrecios] = useState<PrecioVenta[]>([]);
  const [existencias, setExistencias] = useState<ExistenciaCafe[]>([]);
  const [lotes, setLotes] = useState<LoteCafeResumen[]>([]);

  const [clienteId, setClienteId] = useState<string>('');
  const [lineas, setLineas] = useState<LineaForm[]>([createEmptyLine()]);
  const [submitted, setSubmitted] = useState(false);
  const [loading, setLoading] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [dialogError, setDialogError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  const stockByLotSku = useMemo(() => {
    const map = new Map<string, number>();
    for (const existencia of existencias) {
      if (existencia.cantidadPaquetes <= 0) continue;
      const key = `${existencia.loteCafeId}-${existencia.presentacionCafeId}-${existencia.tipoCafeId}`;
      map.set(key, (map.get(key) ?? 0) + existencia.cantidadPaquetes);
    }
    return map;
  }, [existencias]);

  const requestedByLot = useMemo(() => {
    const map = new Map<string, number>();
    for (const linea of lineas) {
      const cantidad = Number(linea.cantidad.trim());
      if (!linea.presentacionCafeId || !linea.tipoCafeId || !linea.loteCafeId) continue;
      if (!Number.isInteger(cantidad) || cantidad <= 0) continue;
      const key = `${linea.loteCafeId}-${linea.presentacionCafeId}-${linea.tipoCafeId}`;
      map.set(key, (map.get(key) ?? 0) + cantidad);
    }
    return map;
  }, [lineas]);

  const lotesById = useMemo(
    () => new Map(lotes.map((lote) => [lote.loteCafeId, lote.codigoLote])),
    [lotes],
  );

  function lotesForLine(presentacionId: string, tipoId: string) {
    const ids = new Set(
      existencias
        .filter(
          (existencia) =>
            existencia.cantidadPaquetes > 0 &&
            String(existencia.presentacionCafeId) === presentacionId &&
            String(existencia.tipoCafeId) === tipoId,
        )
        .map((existencia) => existencia.loteCafeId),
    );

    return [...ids]
      .map((loteCafeId) => {
        const existencia = existencias.find(
          (item) =>
            item.loteCafeId === loteCafeId &&
            String(item.presentacionCafeId) === presentacionId &&
            String(item.tipoCafeId) === tipoId,
        );
        return {
          loteCafeId,
          codigo: lotesById.get(loteCafeId) ?? String(loteCafeId),
          cantidad: existencia?.cantidadPaquetes ?? 0,
        };
      })
      .sort((a, b) => a.codigo.localeCompare(b.codigo));
  }

  const activePrecios = useMemo(
    () => precios.filter((precio) => precio.estado === 1),
    [precios],
  );

  async function loadData() {
    if (!open) return;

    setLoading(true);
    setLoadError(null);

    try {
      const tasks: Promise<void>[] = [];

      if (canLoadClientes) {
        tasks.push(
          apiFetch<Cliente[]>('/api/v1/ventas/clientes').then((list) => {
            setClientes(list);
          }),
        );
      }

      if (canLoadCatalogs) {
        tasks.push(
          apiFetch<PresentacionCafe[]>('/api/v1/inventario/presentaciones').then((list) => {
            setPresentaciones(list);
          }),
        );
        tasks.push(
          apiFetch<TipoCafe[]>('/api/v1/inventario/tipos-cafe').then((list) => {
            setTiposCafe(list);
          }),
        );
      }

      if (canLoadPrecios) {
        tasks.push(
          apiFetch<PrecioVenta[]>('/api/v1/ventas/precios').then((list) => {
            setPrecios(list);
          }),
        );
      }

      if (canQueryInventory) {
        tasks.push(
          (async () => {
            const lotesList = await apiFetch<LoteCafeResumen[]>('/api/v1/inventario/lotes');
            setLotes(lotesList);
            const responses = await Promise.all(
              lotesList.map((lote) =>
                apiFetch<ExistenciaCafe[]>(
                  `/api/v1/inventario/lotes/${lote.loteCafeId}/existencias`,
                ),
              ),
            );
            setExistencias(responses.flat());
          })(),
        );
      }

      await Promise.all(tasks);
    } catch (requestError) {
      setLoadError(requestError instanceof Error ? requestError.message : ts('inventoryLoadError'));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    if (!open) return;
    setClienteId('');
    setLineas([createEmptyLine()]);
    setSubmitted(false);
    setDialogError(null);
    setSuccess(null);
    void loadData();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open]);

  function updateLine(id: string, patch: Partial<LineaForm>) {
    setLineas((current) =>
      current.map((linea) => (linea.id === id ? { ...linea, ...patch } : linea)),
    );
  }

  function addLine() {
    setLineas((current) => [...current, createEmptyLine()]);
  }

  function removeLine(id: string) {
    setLineas((current) => (current.length > 1 ? current.filter((linea) => linea.id !== id) : current));
  }

  const lineErrors = useMemo(() => {
    const errors: Record<
      string,
      { presentacion?: string; tipo?: string; lote?: string; cantidad?: string; precio?: string }
    > = {};

    for (const linea of lineas) {
      const lineError: {
        presentacion?: string;
        tipo?: string;
        lote?: string;
        cantidad?: string;
        precio?: string;
      } = {};

      if (!linea.presentacionCafeId) {
        lineError.presentacion = 'required';
      }
      if (!linea.tipoCafeId) {
        lineError.tipo = 'required';
      }

      const loteValido =
        !!linea.presentacionCafeId &&
        !!linea.tipoCafeId &&
        !!linea.loteCafeId &&
        existencias.some(
          (existencia) =>
            existencia.loteCafeId === Number(linea.loteCafeId) &&
            String(existencia.presentacionCafeId) === linea.presentacionCafeId &&
            String(existencia.tipoCafeId) === linea.tipoCafeId &&
            existencia.cantidadPaquetes > 0,
        );

      if (!linea.loteCafeId) {
        lineError.lote = 'required';
      } else if (!loteValido) {
        lineError.lote = 'required';
      }

      const cantidad = Number(linea.cantidad.trim());
      if (!linea.cantidad.trim()) {
        lineError.cantidad = 'required';
      } else if (!Number.isInteger(cantidad) || cantidad <= 0) {
        lineError.cantidad = 'invalid';
      } else if (linea.loteCafeId && loteValido) {
        const key = `${linea.loteCafeId}-${linea.presentacionCafeId}-${linea.tipoCafeId}`;
        const disponible = stockByLotSku.get(key) ?? 0;
        const solicitado = requestedByLot.get(key) ?? 0;
        if (solicitado > disponible) {
          lineError.cantidad = 'insufficient';
        }
      }

      if (!linea.precioVentaId) {
        lineError.precio = 'required';
      } else {
        const precio = activePrecios.find((item) => String(item.precioVentaId) === linea.precioVentaId);
        if (!precio || precio.estado !== 1) {
          lineError.precio = 'inactive';
        }
        if (precio && linea.presentacionCafeId && precio.presentacionCafeId !== Number(linea.presentacionCafeId)) {
          lineError.precio = 'mismatch';
        }
      }

      if (Object.keys(lineError).length > 0) {
        errors[linea.id] = lineError;
      }
    }

    return errors;
  }, [lineas, existencias, stockByLotSku, requestedByLot, activePrecios]);

  const hasErrors = Object.keys(lineErrors).length > 0 || lineas.length === 0;

  const total = useMemo(() => {
    return lineas.reduce((sum, linea) => {
      const cantidad = Number(linea.cantidad.trim());
      const precio = activePrecios.find((item) => String(item.precioVentaId) === linea.precioVentaId);
      if (!Number.isFinite(cantidad) || !precio) return sum;
      return sum + cantidad * precio.precio;
    }, 0);
  }, [lineas, activePrecios]);

  function closeDialog() {
    if (loading) return;
    onClose();
  }

  async function submit() {
    if (saving) return;

    setSubmitted(true);
    setDialogError(null);
    setSuccess(null);

    if (hasErrors) {
      setDialogError(ts('validationErrorSummary') ?? 'Revise los campos obligatorios.');
      return;
    }

    setSaving(true);

    const payload = {
      clienteId: clienteId ? Number(clienteId) : null,
      fechaVenta: null,
      observacion: null,
      detalles: lineas.map((linea) => ({
        loteCafeId: Number(linea.loteCafeId),
        presentacionCafeId: Number(linea.presentacionCafeId),
        tipoCafeId: Number(linea.tipoCafeId),
        precioVentaId: Number(linea.precioVentaId),
        cantidadPaquetes: Number(linea.cantidad.trim()),
      })),
    };

    try {
      await apiFetch<{ ventaId: number }>('/api/v1/ventas/ventas', {
        method: 'POST',
        body: JSON.stringify(payload),
      });

      setSuccess(ts('ventaRegistered'));
      setLineas([createEmptyLine()]);
      setClienteId('');
      setSubmitted(false);
    } catch (requestError) {
      setDialogError(
        requestError instanceof Error ? requestError.message : ts('ventaRegisterError'),
      );
    } finally {
      setSaving(false);
    }
  }

  const activePresentaciones = useMemo(() => {
    const ids = new Set(existencias.filter((e) => e.cantidadPaquetes > 0).map((e) => e.presentacionCafeId));
    return presentaciones.filter((presentacion) => ids.has(presentacion.presentacionCafeId));
  }, [existencias, presentaciones]);

  function tiposForPresentacion(presentacionId: string) {
    const ids = new Set(
      existencias
        .filter((e) => e.cantidadPaquetes > 0 && String(e.presentacionCafeId) === presentacionId)
        .map((e) => e.tipoCafeId),
    );
    return tiposCafe.filter((tipo) => ids.has(tipo.tipoCafeId));
  }

  function preciosForPresentacion(presentacionId: string) {
    return activePrecios.filter((precio) => String(precio.presentacionCafeId) === presentacionId);
  }

  return (
    <Dialog open={open} onClose={closeDialog} fullWidth maxWidth="lg">
      <DialogTitle>{ts('newVentaTitle')}</DialogTitle>
      <DialogContent dividers>
        <Stack spacing={2} sx={{ pt: 1 }}>
          {loadError ? <Alert severity="error">{loadError}</Alert> : null}
          {dialogError ? <Alert severity="error">{dialogError}</Alert> : null}
          {success ? <Alert severity="success">{success}</Alert> : null}

          <FormControl fullWidth disabled={loading || !canLoadClientes}>
            <InputLabel id="venta-cliente-label">{ts('clienteOptional')}</InputLabel>
            <Select
              labelId="venta-cliente-label"
              label={ts('clienteOptional')}
              value={clienteId}
              onChange={(event: SelectChangeEvent) => setClienteId(event.target.value)}
            >
              <MenuItem value="">{ts('noCliente')}</MenuItem>
              {clientes.map((cliente) => (
                <MenuItem key={cliente.clienteId} value={String(cliente.clienteId)}>
                  {cliente.nombre} ({cliente.numeroIdentificacion})
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          {loading ? (
            <Typography variant="body2" color="text.secondary">
              {tc('loading')}
            </Typography>
          ) : (
            <Stack spacing={2}>
              <TableContainer>
                <Table size="small" sx={{ minWidth: 900 }}>
                  <TableHead>
                    <TableRow>
                      <TableCell>{tf('presentation')}</TableCell>
                      <TableCell>{tf('coffeeType')}</TableCell>
                      <TableCell>{tf('lotCode')}</TableCell>
                      <TableCell>{ts('quantityLabel')}</TableCell>
                      <TableCell>{ts('priceLabel')}</TableCell>
                      <TableCell align="right">{ts('unitPriceLabel')}</TableCell>
                      <TableCell align="right">{ts('subtotalLabel')}</TableCell>
                      <TableCell align="right">{tf('actions')}</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {lineas.map((linea) => {
                      const errors = submitted ? lineErrors[linea.id] : undefined;
                      const selectedPrecio = activePrecios.find(
                        (precio) => String(precio.precioVentaId) === linea.precioVentaId,
                      );
                      const cantidad = Number(linea.cantidad.trim());
                      const subtotal =
                        selectedPrecio && Number.isFinite(cantidad)
                          ? cantidad * selectedPrecio.precio
                          : 0;

                      return (
                        <TableRow key={linea.id}>
                          <TableCell sx={{ minWidth: 180 }}>
                            <FormControl
                              size="small"
                              fullWidth
                              error={Boolean(errors?.presentacion)}
                            >
                              <InputLabel id={`presentacion-${linea.id}`}>
                                {tf('presentation')}
                              </InputLabel>
                              <Select
                                labelId={`presentacion-${linea.id}`}
                                label={tf('presentation')}
                                value={linea.presentacionCafeId}
                                onChange={(event: SelectChangeEvent) =>
                                  updateLine(linea.id, {
                                    presentacionCafeId: event.target.value,
                                    tipoCafeId: '',
                                    loteCafeId: '',
                                    precioVentaId: '',
                                  })
                                }
                              >
                                {activePresentaciones.map((presentacion) => (
                                  <MenuItem
                                    key={presentacion.presentacionCafeId}
                                    value={String(presentacion.presentacionCafeId)}
                                  >
                                    {presentacion.nombre}
                                  </MenuItem>
                                ))}
                              </Select>
                              {errors?.presentacion ? (
                                <FormHelperText>{ts('presentationRequired')}</FormHelperText>
                              ) : null}
                            </FormControl>
                          </TableCell>

                          <TableCell sx={{ minWidth: 160 }}>
                            <FormControl
                              size="small"
                              fullWidth
                              error={Boolean(errors?.tipo)}
                              disabled={!linea.presentacionCafeId}
                            >
                              <InputLabel id={`tipo-${linea.id}`}>{tf('coffeeType')}</InputLabel>
                              <Select
                                labelId={`tipo-${linea.id}`}
                                label={tf('coffeeType')}
                                value={linea.tipoCafeId}
                                onChange={(event: SelectChangeEvent) =>
                                  updateLine(linea.id, { tipoCafeId: event.target.value, loteCafeId: '' })
                                }
                              >
                                {tiposForPresentacion(linea.presentacionCafeId).map((tipo) => (
                                  <MenuItem key={tipo.tipoCafeId} value={String(tipo.tipoCafeId)}>
                                    {tipo.nombre}
                                  </MenuItem>
                                ))}
                              </Select>
                              {errors?.tipo ? (
                                <FormHelperText>{ts('coffeeTypeRequired')}</FormHelperText>
                              ) : null}
                            </FormControl>
                          </TableCell>

                          <TableCell sx={{ minWidth: 200 }}>
                            <FormControl
                              size="small"
                              fullWidth
                              error={Boolean(errors?.lote)}
                              disabled={!linea.presentacionCafeId || !linea.tipoCafeId}
                            >
                              <InputLabel id={`lote-${linea.id}`}>{tf('lotCode')}</InputLabel>
                              <Select
                                labelId={`lote-${linea.id}`}
                                label={tf('lotCode')}
                                value={linea.loteCafeId}
                                onChange={(event: SelectChangeEvent) =>
                                  updateLine(linea.id, { loteCafeId: event.target.value })
                                }
                              >
                                {lotesForLine(linea.presentacionCafeId, linea.tipoCafeId).map((lote) => (
                                  <MenuItem key={lote.loteCafeId} value={String(lote.loteCafeId)}>
                                    {lote.codigo} ({lote.cantidad})
                                  </MenuItem>
                                ))}
                              </Select>
                              {linea.loteCafeId && linea.presentacionCafeId && linea.tipoCafeId ? (
                                (() => {
                                  const stockKey = `${linea.loteCafeId}-${linea.presentacionCafeId}-${linea.tipoCafeId}`;
                                  const disponible = stockByLotSku.get(stockKey) ?? 0;
                                  const solicitado = requestedByLot.get(stockKey) ?? 0;
                                  const restante = disponible - solicitado;
                                  const cantidad = Number(linea.cantidad.trim());
                                  const excede = Number.isInteger(cantidad) && cantidad > 0 && solicitado > disponible;
                                  const cantidadMostrada = Number.isInteger(cantidad) && cantidad > 0 ? cantidad : 0;
                                  return (
                                    <Box sx={{ mt: 0.75, display: 'grid', gridTemplateColumns: 'repeat(3, minmax(0, 1fr))', gap: 1, width: '100%' }}>
                                      <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', gap: 0.25, px: 1, py: 0.5, borderRadius: 1, bgcolor: 'action.hover', minHeight: 44 }}>
                                        <Typography variant="caption" color="text.secondary">Disponibles</Typography>
                                        <Typography variant="caption" fontWeight={600}>{disponible}</Typography>
                                      </Box>
                                      <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', gap: 0.25, px: 1, py: 0.5, borderRadius: 1, bgcolor: 'action.hover', minHeight: 44 }}>
                                        <Typography variant="caption" color="text.secondary">Cantidad</Typography>
                                        <Typography variant="caption" fontWeight={600}>{cantidadMostrada}</Typography>
                                      </Box>
                                      <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', gap: 0.25, px: 1, py: 0.5, borderRadius: 1, bgcolor: excede ? 'error.light' : 'success.light', color: excede ? 'error.contrastText' : 'success.contrastText', minHeight: 44 }}>
                                        <Typography variant="caption" sx={{ opacity: 0.9 }}>Restantes</Typography>
                                        <Typography variant="caption" fontWeight={700}>{restante}</Typography>
                                      </Box>
                                    </Box>
                                  );
                                })()
                              ) : null}
                              {errors?.lote ? (
                                <FormHelperText>{ts('lotRequired')}</FormHelperText>
                              ) : null}
                            </FormControl>
                          </TableCell>

                          <TableCell sx={{ minWidth: 120 }}>
                            <TextField
                              size="small"
                              type="number"
                              label={ts('quantityLabel')}
                              value={linea.cantidad}
                              onChange={(event) =>
                                updateLine(linea.id, { cantidad: event.target.value })
                              }
                              error={Boolean(errors?.cantidad)}
                              helperText={
                                errors?.cantidad === 'required'
                                  ? ts('quantityRequired')
                                  : errors?.cantidad === 'invalid'
                                    ? ts('quantityInvalid')
                                    : errors?.cantidad === 'insufficient'
                                      ? ts('quantityInsufficient')
                                      : undefined
                              }
                              fullWidth
                              slotProps={{ htmlInput: { min: 1, step: 1 } }}
                            />
                          </TableCell>

                          <TableCell sx={{ minWidth: 180 }}>
                            <FormControl
                              size="small"
                              fullWidth
                              error={Boolean(errors?.precio)}
                              disabled={!linea.presentacionCafeId}
                            >
                              <InputLabel id={`precio-${linea.id}`}>{ts('priceLabel')}</InputLabel>
                              <Select
                                labelId={`precio-${linea.id}`}
                                label={ts('priceLabel')}
                                value={linea.precioVentaId}
                                onChange={(event: SelectChangeEvent) =>
                                  updateLine(linea.id, { precioVentaId: event.target.value })
                                }
                              >
                                {preciosForPresentacion(linea.presentacionCafeId).map((precio) => (
                                  <MenuItem
                                    key={precio.precioVentaId}
                                    value={String(precio.precioVentaId)}
                                  >
                                    {precio.nombre} -{' '}
                                    {new Intl.NumberFormat(locale, {
                                      style: 'currency',
                                      currency: 'COP',
                                      maximumFractionDigits: 0,
                                    }).format(precio.precio)}
                                  </MenuItem>
                                ))}
                              </Select>
                              {errors?.precio ? (
                                <FormHelperText>
                                  {errors.precio === 'required'
                                    ? ts('priceRequired')
                                    : errors.precio === 'inactive'
                                      ? ts('priceInactive')
                                      : ts('priceMismatch')}
                                </FormHelperText>
                              ) : null}
                            </FormControl>
                          </TableCell>

                          <TableCell align="right">
                            {selectedPrecio
                              ? new Intl.NumberFormat(locale, {
                                  style: 'currency',
                                  currency: 'COP',
                                  maximumFractionDigits: 0,
                                }).format(selectedPrecio.precio)
                              : '-'}
                          </TableCell>

                          <TableCell align="right">
                            {selectedPrecio && Number.isFinite(cantidad)
                              ? new Intl.NumberFormat(locale, {
                                  style: 'currency',
                                  currency: 'COP',
                                  maximumFractionDigits: 0,
                                }).format(subtotal)
                              : '-'}
                          </TableCell>

                          <TableCell align="right">
                            <Button
                              size="small"
                              color="error"
                              variant="outlined"
                              onClick={() => removeLine(linea.id)}
                              disabled={lineas.length <= 1}
                            >
                              <DeleteOutlineIcon fontSize="small" />
                            </Button>
                          </TableCell>
                        </TableRow>
                      );
                    })}
                  </TableBody>
                </Table>
              </TableContainer>

              <Stack direction="row" justifyContent="space-between" alignItems="center">
                <Button
                  size="small"
                  variant="outlined"
                  startIcon={<AddIcon />}
                  onClick={addLine}
                  disabled={loading}
                >
                  {ts('addProduct')}
                </Button>

                <Typography variant="h6">
                  {ts('totalLabel')}: {' '}
                  {new Intl.NumberFormat(locale, {
                    style: 'currency',
                    currency: 'COP',
                    maximumFractionDigits: 0,
                  }).format(total)}
                </Typography>
              </Stack>
            </Stack>
          )}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={closeDialog} disabled={loading}>
          {tc('cancel')}
        </Button>
        <Button variant="contained" onClick={() => void submit()} disabled={loading || saving || hasErrors}>
          {saving ? ts('savingVenta') : ts('registerSale')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}