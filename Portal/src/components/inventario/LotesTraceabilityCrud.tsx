'use client';

import { useEffect, useState } from 'react';

import Alert from '@mui/material/Alert';
import Button from '@mui/material/Button';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import CardHeader from '@mui/material/CardHeader';
import Chip from '@mui/material/Chip';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogTitle from '@mui/material/DialogTitle';
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

type Producto = {
  productoId: number;
  nombre: string;
  estado: boolean;
};

type Lote = {
  loteId: number;
  productoId: number;
  productoNombre: string;
  unidadMedida: string;
  codigoLote: string;
  cantidadInicial: number;
  cantidadDisponible: number;
  estado: string;
  fechaCreacion: string;
};

type LoteTraceability = {
  loteId: number;
  productoId: number;
  productoNombre: string;
  unidadMedida: string;
  codigoLote: string;
  cantidadInicial: number;
  cantidadDisponible: number;
  estado: string;
  fechaCreacion: string;
  origen?: {
    tipo: string;
    referenciaId?: number | null;
    fecha?: string | null;
    titulo: string;
    descripcion: string;
    parcelaId?: number | null;
    parcelaNombre?: string | null;
    fincaNombre?: string | null;
    loteOrigenId?: number | null;
    codigoLoteOrigen?: string | null;
  } | null;
  movimientos: Array<{
    movimientoId: number;
    tipo: string;
    ventaId?: number | null;
    clienteNombre?: string | null;
    ventaEstado?: boolean | null;
    fecha: string;
    cantidad: number;
    observacion?: string | null;
  }>;
  transformacionesRelacionadas: Array<{
    transformacionCafeId: number;
    tipoTransformacion: string;
    fechaTransformacion?: string | null;
    loteSalidaId: number;
    codigoLoteSalida: string;
    productoSalidaNombre: string;
    cantidadEntrada: number;
    cantidadSalida: number;
    estado: string;
  }>;
  empaquesRelacionados: Array<{
    empaqueCafeId: number;
    rol: string;
    fechaEmpaque?: string | null;
    loteOrigenId: number;
    codigoLoteOrigen: string;
    productoOrigenNombre: string;
    loteSalidaId: number;
    codigoLoteSalida: string;
    productoSalidaNombre: string;
    presentacionNombre: string;
    presentacionCantidad: number;
    presentacionUnidadMedida: string;
    cantidad: number;
    pesoTotalGramos: number;
    cantidadOrigenAsignada: number;
    costoTotalAsignado: number;
    estado: string;
  }>;
  costosRelacionados: Array<{
    costoId: number;
    nombre: string;
    tipoCosto: string;
    fuenteModulo?: string | null;
    referenciaId?: number | null;
    monto: number;
    fecha?: string | null;
    estado: boolean;
  }>;
  operacionesAgricolasRelacionadas: Array<{
    operacionAgricolaId: number;
    tipoOperacion: string;
    fechaOperacion?: string | null;
    insumoNombre?: string | null;
    cantidadInsumo?: number | null;
    costoTotal: number;
    estado: string;
  }>;
};

function toInputDate(value?: string | null) {
  if (!value) return '';
  return value.slice(0, 10);
}

function mapOriginTypeLabel(value: string, tproduction: (key: string) => string) {
  switch (value) {
    case 'CosechaCafe':
      return tproduction('originHarvest');
    case 'RecepcionCafe':
      return tproduction('originReception');
    case 'TransformacionCafe':
      return tproduction('originTransformation');
    case 'EmpaqueCafe':
      return tproduction('originPackaging');
    default:
      return tproduction('originManual');
  }
}

function mapCostTypeLabel(value: string, tproduction: (key: string) => string) {
  switch (value) {
    case 'OperacionAgricola':
      return tproduction('costTypeOperacionAgricola');
    case 'CosechaCafe':
      return tproduction('costTypeCosechaCafe');
    case 'TransformacionCafe':
      return tproduction('costTypeTransformacionCafe');
    case 'EmpaqueCafe':
      return tproduction('costTypeEmpaqueCafe');
    default:
      return tproduction('costTypeManual');
  }
}

function mapOperationTypeLabel(value: string, tproduction: (key: string) => string) {
  switch (value) {
    case 'AplicacionInsumo':
      return tproduction('operationTypeAplicacionInsumo');
    case 'Fertilizacion':
      return tproduction('operationTypeFertilizacion');
    case 'Poda':
      return tproduction('operationTypePoda');
    case 'Riego':
      return tproduction('operationTypeRiego');
    case 'Jornal':
      return tproduction('operationTypeJornal');
    default:
      return tproduction('operationTypeMantenimiento');
  }
}

function mapTransformationTypeLabel(value: string, tproduction: (key: string) => string) {
  switch (value) {
    case 'Despulpado':
      return tproduction('typeDespulpado');
    case 'Secado':
      return tproduction('typeSecado');
    case 'Trillado':
      return tproduction('typeTrillado');
    case 'Tostion':
      return tproduction('typeTostion');
    default:
      return value;
  }
}

function mapPackagingRoleLabel(value: string, tproduction: (key: string) => string) {
  switch (value) {
    case 'Salida':
      return tproduction('packagingRoleOutput');
    default:
      return tproduction('packagingRoleSource');
  }
}

function formatPresentationLabel(nombre: string, cantidad: number, unidadMedida: string) {
  return `${nombre} · ${cantidad} ${unidadMedida}`;
}

export function LotesTraceabilityCrud() {
  const nav = useTranslations('navigation');
  const tc = useTranslations('common');
  const tf = useTranslations('fields');
  const tp = useTranslations('pages');
  const tcrud = useTranslations('crud');
  const tproduction = useTranslations('production');

  const { has } = useAuth();
  const canView = has('Inventario.Lotes', 'Ver');

  const [items, setItems] = useState<Lote[]>([]);
  const [productos, setProductos] = useState<Producto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [productoFiltro, setProductoFiltro] = useState('');

  const [detailOpen, setDetailOpen] = useState(false);
  const [detailLoading, setDetailLoading] = useState(false);
  const [detailError, setDetailError] = useState<string | null>(null);
  const [detail, setDetail] = useState<LoteTraceability | null>(null);

  async function reload() {
    setLoading(true);
    setError(null);
    try {
      const [lotesRes, productosRes] = await Promise.allSettled([
        apiFetch<Lote[]>('/api/v1/lotes'),
        apiFetch<Producto[]>('/api/v1/productos'),
      ]);

      if (lotesRes.status === 'fulfilled') {
        setItems(lotesRes.value);
      } else {
        setItems([]);
        setError(lotesRes.reason instanceof Error ? lotesRes.reason.message : tcrud('errorLoadingData'));
      }

      setProductos(productosRes.status === 'fulfilled' ? productosRes.value.filter((item) => item.estado) : []);
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

  async function loadTraceability(loteId: number) {
    setDetailLoading(true);
    setDetailError(null);
    try {
      const data = await apiFetch<LoteTraceability>(`/api/v1/lotes/${loteId}/trazabilidad`);
      setDetail(data);
      setDetailOpen(true);
    } catch (e) {
      setDetailError(e instanceof Error ? e.message : tproduction('errorLoadingTraceability'));
      setDetailOpen(true);
    } finally {
      setDetailLoading(false);
    }
  }

  function closeDetail() {
    if (detailLoading) return;
    setDetailOpen(false);
    setDetail(null);
    setDetailError(null);
  }

  const filteredItems = productoFiltro
    ? items.filter((item) => item.productoId === Number(productoFiltro))
    : items;

  if (!canView) {
    return (
      <Stack spacing={1}>
        <Typography variant="h4">{nav('lotes')}</Typography>
        <Typography variant="body2" color="text.secondary">{tp('noPermission')}</Typography>
      </Stack>
    );
  }

  return (
    <Stack spacing={2}>
      <Stack direction="row" alignItems="center" justifyContent="space-between" spacing={2}>
        <Typography variant="h4">{nav('lotes')}</Typography>
        <Stack direction="row" spacing={1}>
          <TextField select size="small" label={tf('product')} value={productoFiltro} onChange={(e) => setProductoFiltro(e.target.value)} sx={{ minWidth: 220 }}>
            <MenuItem value="">{tproduction('allProducts')}</MenuItem>
            {productos.map((item) => (
              <MenuItem key={item.productoId} value={String(item.productoId)}>{item.nombre}</MenuItem>
            ))}
          </TextField>
          <Button variant="outlined" onClick={() => void reload()} disabled={loading}>{tc('refresh')}</Button>
        </Stack>
      </Stack>

      {error ? <Alert severity="error">{error}</Alert> : null}

      <Card variant="outlined">
        <CardHeader title={tc('listTitle')} />
        <CardContent sx={{ pt: 0 }}>
          {loading ? (
            <Typography variant="body2" color="text.secondary">{tc('loading')}</Typography>
          ) : (
            <TableContainer>
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>{tf('id')}</TableCell>
                    <TableCell>{tf('createdAt')}</TableCell>
                    <TableCell>{tf('product')}</TableCell>
                    <TableCell>{tf('batchCode')}</TableCell>
                    <TableCell>{tf('initialQuantity')}</TableCell>
                    <TableCell>{tf('availableQuantity')}</TableCell>
                    <TableCell>{tf('status')}</TableCell>
                    <TableCell align="right">{tf('actions')}</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {filteredItems.map((item) => (
                    <TableRow key={item.loteId} hover>
                      <TableCell>{item.loteId}</TableCell>
                      <TableCell>{toInputDate(item.fechaCreacion)}</TableCell>
                      <TableCell>{item.productoNombre}</TableCell>
                      <TableCell>{item.codigoLote}</TableCell>
                      <TableCell>{item.cantidadInicial} {item.unidadMedida}</TableCell>
                      <TableCell>{item.cantidadDisponible} {item.unidadMedida}</TableCell>
                      <TableCell><Chip size="small" color={item.estado === 'Activo' ? 'success' : 'default'} label={item.estado} /></TableCell>
                      <TableCell align="right">
                        <Button size="small" variant="outlined" onClick={() => void loadTraceability(item.loteId)}>{tc('viewDetails')}</Button>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          )}
        </CardContent>
      </Card>

      <Dialog open={detailOpen} onClose={closeDetail} fullWidth maxWidth="lg">
        <DialogTitle>{tproduction('traceabilityTitle')}</DialogTitle>
        <DialogContent dividers>
          <Stack spacing={2} sx={{ pt: 1 }}>
            {detailError ? <Alert severity="error">{detailError}</Alert> : null}
            {detailLoading ? <Typography variant="body2" color="text.secondary">{tc('loading')}</Typography> : null}
            {detail ? (
              <>
                <Card variant="outlined">
                  <CardContent>
                    <Stack spacing={1}>
                      <Typography variant="h6">{detail.codigoLote}</Typography>
                      <Typography variant="body2" color="text.secondary">{detail.productoNombre} · {detail.cantidadDisponible} / {detail.cantidadInicial} {detail.unidadMedida}</Typography>
                      <Chip size="small" color={detail.estado === 'Activo' ? 'success' : 'default'} label={detail.estado} sx={{ width: 'fit-content' }} />
                    </Stack>
                  </CardContent>
                </Card>

                <Card variant="outlined">
                  <CardHeader title={tf('origin')} />
                  <CardContent sx={{ pt: 0 }}>
                    <Stack spacing={1}>
                      <Typography variant="body2"><strong>{mapOriginTypeLabel(detail.origen?.tipo ?? 'Manual', tproduction)}</strong></Typography>
                      <Typography variant="body2">{detail.origen?.titulo ?? '—'}</Typography>
                      <Typography variant="body2" color="text.secondary">{detail.origen?.descripcion ?? '—'}</Typography>
                      {detail.origen?.fecha ? <Typography variant="caption" color="text.secondary">{toInputDate(detail.origen.fecha)}</Typography> : null}
                      {detail.origen?.loteOrigenId ? (
                        <Button size="small" variant="outlined" sx={{ width: 'fit-content' }} onClick={() => void loadTraceability(detail.origen?.loteOrigenId ?? 0)}>
                          {tc('viewDetails')} {detail.origen.codigoLoteOrigen ? `· ${detail.origen.codigoLoteOrigen}` : ''}
                        </Button>
                      ) : null}
                    </Stack>
                  </CardContent>
                </Card>

                <Card variant="outlined">
                  <CardHeader title={tproduction('movementsTitle')} />
                  <CardContent sx={{ pt: 0 }}>
                    {detail.movimientos.length === 0 ? (
                      <Typography variant="body2" color="text.secondary">{tproduction('noMovements')}</Typography>
                    ) : (
                      <TableContainer>
                        <Table size="small">
                          <TableHead>
                            <TableRow>
                              <TableCell>{tf('id')}</TableCell>
                              <TableCell>{tf('date')}</TableCell>
                              <TableCell>{tf('origin')}</TableCell>
                              <TableCell>{tf('quantity')}</TableCell>
                              <TableCell>{tf('notes')}</TableCell>
                            </TableRow>
                          </TableHead>
                          <TableBody>
                            {detail.movimientos.map((item) => (
                              <TableRow key={item.movimientoId} hover>
                                <TableCell>{item.movimientoId}</TableCell>
                                <TableCell>{toInputDate(item.fecha)}</TableCell>
                                <TableCell>
                                  <Stack spacing={0.25}>
                                    <Typography variant="body2">{item.tipo}</Typography>
                                    {item.ventaId ? (
                                      <Typography variant="caption" color="text.secondary">
                                        {nav('ventas')} #{item.ventaId}{item.clienteNombre ? ` · ${item.clienteNombre}` : ''}
                                      </Typography>
                                    ) : null}
                                  </Stack>
                                </TableCell>
                                <TableCell>{item.cantidad}</TableCell>
                                <TableCell>{item.observacion ?? '—'}</TableCell>
                              </TableRow>
                            ))}
                          </TableBody>
                        </Table>
                      </TableContainer>
                    )}
                  </CardContent>
                </Card>

                <Card variant="outlined">
                  <CardHeader title={tproduction('relatedTransformationsTitle')} />
                  <CardContent sx={{ pt: 0 }}>
                    {detail.transformacionesRelacionadas.length === 0 ? (
                      <Typography variant="body2" color="text.secondary">{tproduction('noRelatedTransformations')}</Typography>
                    ) : (
                      <TableContainer>
                        <Table size="small">
                          <TableHead>
                            <TableRow>
                              <TableCell>{tf('id')}</TableCell>
                              <TableCell>{tf('transformationType')}</TableCell>
                              <TableCell>{tf('date')}</TableCell>
                              <TableCell>{tf('batchCode')}</TableCell>
                              <TableCell>{tf('outputProduct')}</TableCell>
                              <TableCell>{tf('outputQuantity')}</TableCell>
                              <TableCell align="right">{tf('actions')}</TableCell>
                            </TableRow>
                          </TableHead>
                          <TableBody>
                            {detail.transformacionesRelacionadas.map((item) => (
                              <TableRow key={item.transformacionCafeId} hover>
                                <TableCell>{item.transformacionCafeId}</TableCell>
                                <TableCell>{mapTransformationTypeLabel(item.tipoTransformacion, tproduction)}</TableCell>
                                <TableCell>{toInputDate(item.fechaTransformacion) || '—'}</TableCell>
                                <TableCell>{item.codigoLoteSalida}</TableCell>
                                <TableCell>{item.productoSalidaNombre}</TableCell>
                                <TableCell>{item.cantidadSalida}</TableCell>
                                <TableCell align="right">
                                  <Button size="small" variant="outlined" onClick={() => void loadTraceability(item.loteSalidaId)}>{tc('viewDetails')}</Button>
                                </TableCell>
                              </TableRow>
                            ))}
                          </TableBody>
                        </Table>
                      </TableContainer>
                    )}
                  </CardContent>
                </Card>

                <Card variant="outlined">
                  <CardHeader title={tproduction('relatedPackagingTitle')} />
                  <CardContent sx={{ pt: 0 }}>
                    {detail.empaquesRelacionados.length === 0 ? (
                      <Typography variant="body2" color="text.secondary">{tproduction('noRelatedPackaging')}</Typography>
                    ) : (
                      <TableContainer>
                        <Table size="small">
                          <TableHead>
                            <TableRow>
                              <TableCell>{tf('id')}</TableCell>
                              <TableCell>{tf('date')}</TableCell>
                              <TableCell>{tf('relation')}</TableCell>
                              <TableCell>{tf('sourceBatch')}</TableCell>
                              <TableCell>{tf('batchCode')}</TableCell>
                              <TableCell>{tf('outputProduct')}</TableCell>
                              <TableCell>{tf('presentation')}</TableCell>
                              <TableCell>{tf('quantity')}</TableCell>
                              <TableCell>{tf('totalCost')}</TableCell>
                              <TableCell>{tf('status')}</TableCell>
                              <TableCell align="right">{tf('actions')}</TableCell>
                            </TableRow>
                          </TableHead>
                          <TableBody>
                            {detail.empaquesRelacionados.map((item) => {
                              const relatedLoteId = item.rol === 'Origen' ? item.loteSalidaId : item.loteOrigenId;
                              const relatedCodigo = item.rol === 'Origen' ? item.codigoLoteSalida : item.codigoLoteOrigen;

                              return (
                                <TableRow key={`${item.empaqueCafeId}-${item.loteSalidaId}-${item.rol}`} hover>
                                  <TableCell>{item.empaqueCafeId}</TableCell>
                                  <TableCell>{toInputDate(item.fechaEmpaque) || '—'}</TableCell>
                                  <TableCell>
                                    <Chip
                                      size="small"
                                      color={item.rol === 'Origen' ? 'secondary' : 'info'}
                                      label={mapPackagingRoleLabel(item.rol, tproduction)}
                                    />
                                  </TableCell>
                                  <TableCell>{item.codigoLoteOrigen}</TableCell>
                                  <TableCell>{item.codigoLoteSalida}</TableCell>
                                  <TableCell>{item.productoSalidaNombre}</TableCell>
                                  <TableCell>{formatPresentationLabel(item.presentacionNombre, item.presentacionCantidad, item.presentacionUnidadMedida)}</TableCell>
                                  <TableCell>{item.cantidad}</TableCell>
                                  <TableCell>{item.costoTotalAsignado}</TableCell>
                                  <TableCell>
                                    <Chip size="small" color={item.estado === 'Registrado' ? 'success' : 'default'} label={item.estado} />
                                  </TableCell>
                                  <TableCell align="right">
                                    <Button size="small" variant="outlined" onClick={() => void loadTraceability(relatedLoteId)}>
                                      {tc('viewDetails')} {relatedCodigo ? `· ${relatedCodigo}` : ''}
                                    </Button>
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

                <Card variant="outlined">
                  <CardHeader title={tproduction('relatedCostsTitle')} />
                  <CardContent sx={{ pt: 0 }}>
                    {detail.costosRelacionados.length === 0 ? (
                      <Typography variant="body2" color="text.secondary">{tproduction('noRelatedCosts')}</Typography>
                    ) : (
                      <TableContainer>
                        <Table size="small">
                          <TableHead>
                            <TableRow>
                              <TableCell>{tf('id')}</TableCell>
                              <TableCell>{tf('name')}</TableCell>
                              <TableCell>{tf('costType')}</TableCell>
                              <TableCell>{tf('amount')}</TableCell>
                              <TableCell>{tf('date')}</TableCell>
                              <TableCell>{tf('status')}</TableCell>
                            </TableRow>
                          </TableHead>
                          <TableBody>
                            {detail.costosRelacionados.map((item) => (
                              <TableRow key={item.costoId} hover>
                                <TableCell>{item.costoId}</TableCell>
                                <TableCell>{item.nombre}</TableCell>
                                <TableCell>{mapCostTypeLabel(item.tipoCosto, tproduction)}</TableCell>
                                <TableCell>{item.monto}</TableCell>
                                <TableCell>{toInputDate(item.fecha) || '—'}</TableCell>
                                <TableCell><Chip size="small" color={item.estado ? 'success' : 'default'} label={item.estado ? tf('active') : tf('inactive')} /></TableCell>
                              </TableRow>
                            ))}
                          </TableBody>
                        </Table>
                      </TableContainer>
                    )}
                  </CardContent>
                </Card>

                <Card variant="outlined">
                  <CardHeader title={tproduction('relatedOperationsTitle')} />
                  <CardContent sx={{ pt: 0 }}>
                    {detail.operacionesAgricolasRelacionadas.length === 0 ? (
                      <Typography variant="body2" color="text.secondary">{tproduction('noRelatedOperations')}</Typography>
                    ) : (
                      <TableContainer>
                        <Table size="small">
                          <TableHead>
                            <TableRow>
                              <TableCell>{tf('id')}</TableCell>
                              <TableCell>{tf('date')}</TableCell>
                              <TableCell>{tf('operationType')}</TableCell>
                              <TableCell>{tf('supply')}</TableCell>
                              <TableCell>{tf('supplyQuantity')}</TableCell>
                              <TableCell>{tf('totalCost')}</TableCell>
                              <TableCell>{tf('status')}</TableCell>
                            </TableRow>
                          </TableHead>
                          <TableBody>
                            {detail.operacionesAgricolasRelacionadas.map((item) => (
                              <TableRow key={item.operacionAgricolaId} hover>
                                <TableCell>{item.operacionAgricolaId}</TableCell>
                                <TableCell>{toInputDate(item.fechaOperacion) || '—'}</TableCell>
                                <TableCell>{mapOperationTypeLabel(item.tipoOperacion, tproduction)}</TableCell>
                                <TableCell>{item.insumoNombre ?? '—'}</TableCell>
                                <TableCell>{item.cantidadInsumo ?? '—'}</TableCell>
                                <TableCell>{item.costoTotal}</TableCell>
                                <TableCell><Chip size="small" color={item.estado === 'Registrada' ? 'success' : 'default'} label={item.estado} /></TableCell>
                              </TableRow>
                            ))}
                          </TableBody>
                        </Table>
                      </TableContainer>
                    )}
                  </CardContent>
                </Card>
              </>
            ) : null}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={closeDetail} disabled={detailLoading}>{tc('close')}</Button>
        </DialogActions>
      </Dialog>
    </Stack>
  );
}