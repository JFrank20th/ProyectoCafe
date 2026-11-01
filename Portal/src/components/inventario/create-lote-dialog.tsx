'use client';

import { useEffect, useMemo, useState } from 'react';

import AddIcon from '@mui/icons-material/Add';
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutline';
import Alert from '@mui/material/Alert';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Card from '@mui/material/Card';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogTitle from '@mui/material/DialogTitle';
import FormControl from '@mui/material/FormControl';
import FormHelperText from '@mui/material/FormHelperText';
import IconButton from '@mui/material/IconButton';
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
  formatGrams,
  formatKg,
  formatNumber,
  isCatalogoActivo,
  type PresentacionCafe,
  type TipoCafe,
  type VariedadCafe,
  type TipoTuesteCafe,
  type TipoProcesoCafe,
} from '@/lib/inventario/cafe';
import {
  buildCreateLotePayload,
  calcularFechaVencimiento,
  LOTE_CODE_MAX_LENGTH,
  OBSERVACION_MAX_LENGTH,
  resolveIngresoLineas,
  resolveInventoryError,
  todayIsoUtc,
  validateCreateLoteInput,
  type CreateLoteOperationResult,
  type IngresoLineaFieldError,
  type IngresoLineaInput,
  type IngresoLineaResult,
  type LoteFieldError,
  type LoteFormValues,
} from '@/lib/inventario/operations';

let nextLineId = 1;

function createEmptyLine(): IngresoLineaInput {
  const id = `ingreso-${nextLineId}`;
  nextLineId += 1;

  return {
    id,
    presentacionCafeId: '',
    tipoCafeId: '',
    cantidad: '',
  };
}

export type CreateLoteDialogProps = {
  open: boolean;
  onClose: () => void;
  onCreated: (result: CreateLoteOperationResult) => void;
  canCreateMovimiento: boolean;
  canLoadCatalogos: boolean;
  presentaciones: readonly PresentacionCafe[];
  tiposCafe: readonly TipoCafe[];
  variedades: readonly VariedadCafe[];
  tiposTueste: readonly TipoTuesteCafe[];
  tiposProceso: readonly TipoProcesoCafe[];
  catalogsLoading: boolean;
  catalogsError: string | null;
};

export function CreateLoteDialog({
  open,
  onClose,
  onCreated,
  canCreateMovimiento,
  canLoadCatalogos,
  presentaciones,
  tiposCafe,
  variedades,
  tiposTueste,
  tiposProceso,
  catalogsLoading,
  catalogsError,
}: CreateLoteDialogProps) {
  const tc = useTranslations('common');
  const tf = useTranslations('fields');
  const ti = useTranslations('inventory');
  const locale = useLocale();

  const [codigoLote, setCodigoLote] = useState('');
  const [fechaProduccion, setFechaProduccion] = useState('');
  // Fecha de envasado se conserva internamente por compatibilidad, pero no se captura en la UI.
  const fechaEnvasado = '';
  const fechaVencimiento = calcularFechaVencimiento(fechaProduccion);
  const [observacion, setObservacion] = useState('');
  const [variedadCafeId, setVariedadCafeId] = useState('');
  const [tipoTuesteCafeId, setTipoTuesteCafeId] = useState('');
  const [tipoProcesoCafeId, setTipoProcesoCafeId] = useState('');
  const [lineas, setLineas] = useState<IngresoLineaInput[]>([]);
  const [submitted, setSubmitted] = useState(false);
  const [saving, setSaving] = useState(false);
  const [dialogError, setDialogError] = useState<string | null>(null);

  const todayIso = todayIsoUtc();

  const activePresentaciones = useMemo(
    () => presentaciones.filter((presentacion) => isCatalogoActivo(presentacion.estado)),
    [presentaciones],
  );
  const activeTiposCafe = useMemo(
    () => tiposCafe.filter((tipoCafe) => isCatalogoActivo(tipoCafe.estado)),
    [tiposCafe],
  );
  const activeVariedades = useMemo(
    () => variedades.filter((variedad) => isCatalogoActivo(variedad.estado)),
    [variedades],
  );
  const activeTiposTueste = useMemo(
    () => tiposTueste.filter((tipoTueste) => isCatalogoActivo(tipoTueste.estado)),
    [tiposTueste],
  );
  const activeTiposProceso = useMemo(
    () => tiposProceso.filter((tipoProceso) => isCatalogoActivo(tipoProceso.estado)),
    [tiposProceso],
  );
  const hasInactiveCatalogs =
    activePresentaciones.length !== presentaciones.length ||
    activeTiposCafe.length !== tiposCafe.length;
  const catalogsReady =
    canLoadCatalogos &&
    !catalogsLoading &&
    !catalogsError &&
    activePresentaciones.length > 0 &&
    activeTiposCafe.length > 0;
  const canUseLines = canCreateMovimiento && catalogsReady;

  const values: LoteFormValues = {
    codigoLote,
    fechaProduccion,
    fechaEnvasado,
    fechaVencimiento,
    observacion,
    variedadCafeId,
    tipoTuesteCafeId,
    tipoProcesoCafeId,
  };
  const lotErrors = submitted ? validateCreateLoteInput(values, todayIso) : {};
  const lineResolution = useMemo(
    () => resolveIngresoLineas(canUseLines ? lineas : [], presentaciones, tiposCafe),
    [canUseLines, lineas, presentaciones, tiposCafe],
  );

  useEffect(() => {
    if (!open) return;

    setCodigoLote('');
    setFechaProduccion(todayIsoUtc());
    // setFechaEnvasado('');
    // fechaVencimiento se calcula automáticamente.
    setObservacion('');
    setVariedadCafeId('');
    setTipoTuesteCafeId('');
    setTipoProcesoCafeId('');
    setLineas(canCreateMovimiento && canLoadCatalogos ? [createEmptyLine()] : []);
    setSubmitted(false);
    setSaving(false);
    setDialogError(null);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open]);

  function closeDialog() {
    if (saving) return;
    onClose();
  }

  function updateLine(id: string, patch: Partial<IngresoLineaInput>) {
    setLineas((current) =>
      current.map((linea) => (linea.id === id ? { ...linea, ...patch } : linea)),
    );
  }

  function addLine() {
    if (!catalogsReady) return;
    setLineas((current) => [...current, createEmptyLine()]);
  }

  function removeLine(id: string) {
    setLineas((current) => current.filter((linea) => linea.id !== id));
  }

  function lotHelperText(field: keyof LoteFormValues, error?: LoteFieldError): string | undefined {
    if (!error) return undefined;

    switch (error) {
      case 'required':
        return field === 'codigoLote'
          ? ti('lotCodeRequired')
          : ti('productionDateRequired');
      case 'maxLength':
        return field === 'observacion'
          ? ti('observationTooLong', { max: OBSERVACION_MAX_LENGTH })
          : ti('lotCodeTooLong', { max: LOTE_CODE_MAX_LENGTH });
      case 'futureDate':
        return ti('productionDateFuture');
      case 'beforeProduction':
        return field === 'fechaEnvasado'
          ? ti('packagingBeforeProduction')
          : ti('expirationBeforeProduction');
      case 'beforePackaging':
        return ti('expirationBeforePackaging');
      default:
        return ti('invalidDate');
    }
  }

  function lineHelperText(error?: IngresoLineaFieldError): string | undefined {
    if (!error) return undefined;

    switch (error) {
      case 'presentationRequired':
        return ti('presentationRequired');
      case 'coffeeTypeRequired':
        return ti('coffeeTypeRequired');
      case 'catalogUnavailable':
        return ti('catalogUnavailable');
      case 'qtyRequired':
        return ti('quantityRequired');
      case 'qtyTooLarge':
        return ti('quantityTooLarge');
      default:
        return ti('quantityInvalid');
    }
  }

  async function submit() {
    if (saving) return;

    setSubmitted(true);
    setDialogError(null);

    const currentLotErrors = validateCreateLoteInput(values, todayIso);
    const currentLineResolution = resolveIngresoLineas(
      canUseLines ? lineas : [],
      presentaciones,
      tiposCafe,
    );

    if (
      Object.keys(currentLotErrors).length > 0 ||
      Object.keys(currentLineResolution.errors).length > 0
    ) {
      setDialogError(ti('validationErrorSummary'));
      return;
    }

    setSaving(true);

    const payload = buildCreateLotePayload(values);
    const conflictMessage = ti('conflictError');
    const forbiddenMessage = ti('forbiddenError');
    const notFoundMessage = ti('notFoundError');

    try {
      const created = await apiFetch<{ loteCafeId: number }>('/api/v1/inventario/lotes', {
        method: 'POST',
        body: JSON.stringify(payload),
      });

      const lineResults: IngresoLineaResult[] = [];

      if (canUseLines) {
        for (const line of currentLineResolution.lines) {
          try {
            await apiFetch<unknown>(
              `/api/v1/inventario/lotes/${created.loteCafeId}/movimientos`,
              {
                method: 'POST',
                body: JSON.stringify({
                  tipoMovimiento: 1,
                  presentacionCafeId: line.presentacion.presentacionCafeId,
                  tipoCafeId: line.tipoCafe.tipoCafeId,
                  cantidadPaquetes: line.cantidadPaquetes,
                  referencia: null,
                  observacion: payload.observacion,
                }),
              },
            );

            lineResults.push({
              id: line.id,
              presentacionNombre: line.presentacion.nombre,
              tipoCafeNombre: line.tipoCafe.nombre,
              cantidadPaquetes: line.cantidadPaquetes,
              success: true,
            });
          } catch (error) {
            lineResults.push({
              id: line.id,
              presentacionNombre: line.presentacion.nombre,
              tipoCafeNombre: line.tipoCafe.nombre,
              cantidadPaquetes: line.cantidadPaquetes,
              success: false,
              error: resolveInventoryError(error, ti('lineErrorFallback'), {
                conflict: conflictMessage,
                forbidden: forbiddenMessage,
                notFound: notFoundMessage,
              }),
            });
          }
        }
      }

      onCreated({
        loteCafeId: created.loteCafeId,
        codigoLote: payload.codigoLote,
        lineResults,
      });
      onClose();
    } catch (error) {
      setDialogError(
        resolveInventoryError(error, ti('errorCreatingLote'), {
          conflict: conflictMessage,
          forbidden: forbiddenMessage,
          notFound: notFoundMessage,
        }),
      );
    } finally {
      setSaving(false);
    }
  }

  return (
    <Dialog open={open} onClose={closeDialog} fullWidth maxWidth="md">
      <DialogTitle>{ti('newLoteTitle')}</DialogTitle>
      <DialogContent dividers>
        <Stack spacing={2} sx={{ pt: 1 }}>
          {dialogError ? <Alert severity="error">{dialogError}</Alert> : null}

          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
            <TextField
              label={tf('lotCode')}
              value={codigoLote}
              onChange={(event) => setCodigoLote(event.target.value)}
              error={Boolean(lotErrors.codigoLote)}
              helperText={lotHelperText('codigoLote', lotErrors.codigoLote)}
              fullWidth
              autoFocus
              slotProps={{ htmlInput: { maxLength: LOTE_CODE_MAX_LENGTH } }}
            />

            <TextField
              label={tf('productionDate')}
              type="date"
              value={fechaProduccion}
              onChange={(event) => setFechaProduccion(event.target.value)}
              error={Boolean(lotErrors.fechaProduccion)}
              helperText={lotHelperText('fechaProduccion', lotErrors.fechaProduccion)}
              fullWidth
              slotProps={{
                inputLabel: { shrink: true },
                htmlInput: { max: todayIso },
              }}
            />
          </Stack>

          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
            <TextField
              label={tf('expirationDate')}
              type="date"
              value={fechaVencimiento}
              fullWidth
              slotProps={{ inputLabel: { shrink: true } }}
              inputProps={{ readOnly: true }}
            />
          </Stack>

          <TextField
            label={tf('observation')}
            value={observacion}
            onChange={(event) => setObservacion(event.target.value)}
            error={Boolean(lotErrors.observacion)}
            helperText={
              lotHelperText('observacion', lotErrors.observacion) ??
              `${observacion.length}/${OBSERVACION_MAX_LENGTH}`
            }
            fullWidth
            multiline
            minRows={2}
            slotProps={{ htmlInput: { maxLength: OBSERVACION_MAX_LENGTH } }}
          />

          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
            <FormControl size="small" fullWidth>
              <InputLabel id="variedad-lote-label">{ti('variedadLabel')}</InputLabel>
              <Select
                labelId="variedad-lote-label"
                label={ti('variedadLabel')}
                value={variedadCafeId}
                onChange={(event: SelectChangeEvent) => setVariedadCafeId(event.target.value)}
              >
                <MenuItem value="">{tc('noneOption')}</MenuItem>
                {activeVariedades.map((variedad) => (
                  <MenuItem key={variedad.variedadCafeId} value={String(variedad.variedadCafeId)}>
                    {variedad.nombre}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>

            <FormControl size="small" fullWidth>
              <InputLabel id="tipo-tueste-lote-label">{ti('tipoTuesteLabel')}</InputLabel>
              <Select
                labelId="tipo-tueste-lote-label"
                label={ti('tipoTuesteLabel')}
                value={tipoTuesteCafeId}
                onChange={(event: SelectChangeEvent) => setTipoTuesteCafeId(event.target.value)}
              >
                <MenuItem value="">{tc('noneOption')}</MenuItem>
                {activeTiposTueste.map((tipoTueste) => (
                  <MenuItem key={tipoTueste.tipoTuesteCafeId} value={String(tipoTueste.tipoTuesteCafeId)}>
                    {tipoTueste.nombre}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>

            <FormControl size="small" fullWidth>
              <InputLabel id="tipo-proceso-lote-label">{ti('tipoProcesoLabel')}</InputLabel>
              <Select
                labelId="tipo-proceso-lote-label"
                label={ti('tipoProcesoLabel')}
                value={tipoProcesoCafeId}
                onChange={(event: SelectChangeEvent) => setTipoProcesoCafeId(event.target.value)}
              >
                <MenuItem value="">{tc('noneOption')}</MenuItem>
                {activeTiposProceso.map((tipoProceso) => (
                  <MenuItem key={tipoProceso.tipoProcesoCafeId} value={String(tipoProceso.tipoProcesoCafeId)}>
                    {tipoProceso.nombre}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
          </Stack>

          <Typography variant="subtitle2">{ti('entryLinesTitle')}</Typography>

          {!canCreateMovimiento ? (
            <Alert severity="info">{ti('noEntryPermission')}</Alert>
          ) : !canLoadCatalogos ? (
            <Alert severity="info">{ti('noCatalogPermission')}</Alert>
          ) : catalogsLoading ? (
            <Typography variant="body2" color="text.secondary">
              {tc('loading')}
            </Typography>
          ) : catalogsError ? (
            <Alert severity="error">{catalogsError}</Alert>
          ) : activePresentaciones.length === 0 || activeTiposCafe.length === 0 ? (
            <Alert severity="warning">{ti('noActiveCatalogs')}</Alert>
          ) : (
            <Stack spacing={2}>
              {hasInactiveCatalogs ? (
                <Alert severity="warning">{ti('inactiveCatalogsWarning')}</Alert>
              ) : null}

              {lineas.length === 0 ? (
                <Alert severity="info">{ti('noEntryLines')}</Alert>
              ) : (
                <Card variant="outlined">
                  <TableContainer>
                    <Table size="small" sx={{ minWidth: 920 }}>
                      <TableHead>
                        <TableRow>
                          <TableCell>{tf('presentation')}</TableCell>
                          <TableCell>{tf('coffeeType')}</TableCell>
                          <TableCell>{tf('quantity')}</TableCell>
                          <TableCell align="right">{tf('unitWeight')}</TableCell>
                          <TableCell align="right">{tf('netWeight')}</TableCell>
                          <TableCell align="right">{tf('actions')}</TableCell>
                        </TableRow>
                      </TableHead>
                      <TableBody>
                        {lineas.map((linea) => {
                          const lineErrors = submitted
                            ? lineResolution.errors[linea.id]
                            : undefined;
                          const resolvedLine = lineResolution.lines.find(
                            (line) => line.id === linea.id,
                          );
                          const selectedPresentacion = activePresentaciones.find(
                            (presentacion) =>
                              presentacion.presentacionCafeId ===
                              Number(linea.presentacionCafeId),
                          );

                          return (
                            <TableRow key={linea.id}>
                              <TableCell sx={{ minWidth: 220 }}>
                                <FormControl
                                  size="small"
                                  fullWidth
                                  error={Boolean(lineErrors?.presentacionCafeId)}
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
                                      })
                                    }
                                  >
                                    {activePresentaciones.map((presentacion) => (
                                      <MenuItem
                                        key={presentacion.presentacionCafeId}
                                        value={String(presentacion.presentacionCafeId)}
                                      >
                                        {presentacion.nombre} (
                                        {formatGrams(
                                          locale,
                                          presentacion.pesoUnitarioGramos,
                                        )}
                                        )
                                      </MenuItem>
                                    ))}
                                  </Select>
                                  {lineErrors?.presentacionCafeId ? (
                                    <FormHelperText>
                                      {lineHelperText(lineErrors.presentacionCafeId)}
                                    </FormHelperText>
                                  ) : null}
                                </FormControl>
                              </TableCell>

                              <TableCell sx={{ minWidth: 200 }}>
                                <FormControl
                                  size="small"
                                  fullWidth
                                  error={Boolean(lineErrors?.tipoCafeId)}
                                >
                                  <InputLabel id={`tipo-cafe-${linea.id}`}>
                                    {tf('coffeeType')}
                                  </InputLabel>
                                  <Select
                                    labelId={`tipo-cafe-${linea.id}`}
                                    label={tf('coffeeType')}
                                    value={linea.tipoCafeId}
                                    onChange={(event: SelectChangeEvent) =>
                                      updateLine(linea.id, {
                                        tipoCafeId: event.target.value,
                                      })
                                    }
                                  >
                                    {activeTiposCafe.map((tipoCafe) => (
                                      <MenuItem
                                        key={tipoCafe.tipoCafeId}
                                        value={String(tipoCafe.tipoCafeId)}
                                      >
                                        {tipoCafe.nombre}
                                      </MenuItem>
                                    ))}
                                  </Select>
                                  {lineErrors?.tipoCafeId ? (
                                    <FormHelperText>
                                      {lineHelperText(lineErrors.tipoCafeId)}
                                    </FormHelperText>
                                  ) : null}
                                </FormControl>
                              </TableCell>

                              <TableCell sx={{ minWidth: 150 }}>
                                <TextField
                                  size="small"
                                  type="number"
                                  label={tf('quantity')}
                                  value={linea.cantidad}
                                  onChange={(event) =>
                                    updateLine(linea.id, { cantidad: event.target.value })
                                  }
                                  error={Boolean(lineErrors?.cantidad)}
                                  helperText={lineHelperText(lineErrors?.cantidad)}
                                  fullWidth
                                  slotProps={{ htmlInput: { min: 1, step: 1 } }}
                                />
                              </TableCell>

                              <TableCell align="right">
                                {selectedPresentacion
                                  ? formatGrams(locale, selectedPresentacion.pesoUnitarioGramos)
                                  : '—'}
                              </TableCell>

                              <TableCell align="right">
                                {resolvedLine ? formatKg(locale, resolvedLine.pesoNetoKg) : '—'}
                              </TableCell>

                              <TableCell align="right">
                                <IconButton
                                  size="small"
                                  aria-label={ti('removeLine')}
                                  onClick={() => removeLine(linea.id)}
                                >
                                  <DeleteOutlineIcon fontSize="small" />
                                </IconButton>
                              </TableCell>
                            </TableRow>
                          );
                        })}
                      </TableBody>
                    </Table>
                  </TableContainer>
                </Card>
              )}

              <Box>
                <Button
                  size="small"
                  variant="outlined"
                  startIcon={<AddIcon />}
                  onClick={addLine}
                  disabled={!catalogsReady || saving}
                >
                  {ti('addLine')}
                </Button>
              </Box>

              <Alert severity="info" icon={false}>
                <Stack direction={{ xs: 'column', sm: 'row' }} spacing={3}>
                  <Box>
                    <Typography variant="caption" color="text.secondary">
                      {ti('totalPackagesPreview')}
                    </Typography>
                    <Typography variant="h6">
                      {formatNumber(locale, lineResolution.totalPaquetes)}
                    </Typography>
                  </Box>
                  <Box>
                    <Typography variant="caption" color="text.secondary">
                      {ti('totalNetWeightPreview')}
                    </Typography>
                    <Typography variant="h6">
                      {formatKg(locale, lineResolution.totalPesoNetoKg)}
                    </Typography>
                  </Box>
                </Stack>
              </Alert>
            </Stack>
          )}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={closeDialog} disabled={saving}>
          {tc('cancel')}
        </Button>
        <Button
          onClick={() => void submit()}
          variant="contained"
          disabled={saving || (canCreateMovimiento && canLoadCatalogos && catalogsLoading)}
        >
          {saving ? ti('creatingLote') : ti('createLoteAction')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
