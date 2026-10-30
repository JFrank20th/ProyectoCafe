'use client';

import { useEffect, useMemo, useState } from 'react';

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
import TextField from '@mui/material/TextField';
import Typography from '@mui/material/Typography';

import { useLocale, useTranslations } from '@/hooks/useTranslations';
import { apiFetch } from '@/lib/api/client';
import {
  formatGrams,
  formatKg,
  formatNumber,
  isCatalogoActivo,
  tipoMovimientoKey,
  type ExistenciaCafe,
  type LoteCafeResumen,
  type MovimientoCafe,
  type PresentacionCafe,
  type TipoCafe,
  type TipoMovimientoLoteCafe,
} from '@/lib/inventario/cafe';
import {
  buildMovimientoPayload,
  movementRequiresBalance,
  OBSERVACION_MAX_LENGTH,
  REFERENCIA_MAX_LENGTH,
  resolveInventoryError,
  validateMovimientoInput,
  type IngresoLineaFieldError,
  type MovimientoFieldErrors,
  type MovimientoFormValues,
} from '@/lib/inventario/operations';

const TIPOS_MOVIMIENTO: readonly TipoMovimientoLoteCafe[] = [1, 2, 3, 4];

export type MovimientoDialogProps = {
  open: boolean;
  onClose: () => void;
  onSaved: (movement: MovimientoCafe) => void;
  lote: LoteCafeResumen;
  existencias: readonly ExistenciaCafe[];
  presentaciones: readonly PresentacionCafe[];
  tiposCafe: readonly TipoCafe[];
  canLoadCatalogos: boolean;
  canViewExistencias: boolean;
  existenciasLoading: boolean;
  existenciasError: string | null;
  initialTipo?: TipoMovimientoLoteCafe;
  catalogsLoading: boolean;
  catalogsError: string | null;
};

export function MovimientoDialog({
  open,
  onClose,
  onSaved,
  lote,
  existencias,
  presentaciones,
  tiposCafe,
  canLoadCatalogos,
  canViewExistencias,
  existenciasLoading,
  existenciasError,
  initialTipo = 1,
  catalogsLoading,
  catalogsError,
}: MovimientoDialogProps) {
  const tc = useTranslations('common');
  const tf = useTranslations('fields');
  const ti = useTranslations('inventory');
  const locale = useLocale();

  const [tipoMovimiento, setTipoMovimiento] = useState<TipoMovimientoLoteCafe>(1);
  const [presentacionCafeId, setPresentacionCafeId] = useState('');
  const [tipoCafeId, setTipoCafeId] = useState('');
  const [cantidad, setCantidad] = useState('');
  const [referencia, setReferencia] = useState('');
  const [observacion, setObservacion] = useState('');
  const [submitted, setSubmitted] = useState(false);
  const [saving, setSaving] = useState(false);
  const [dialogError, setDialogError] = useState<string | null>(null);

  const activePresentaciones = useMemo(
    () => presentaciones.filter((presentacion) => isCatalogoActivo(presentacion.estado)),
    [presentaciones],
  );
  const activeTiposCafe = useMemo(
    () => tiposCafe.filter((tipoCafe) => isCatalogoActivo(tipoCafe.estado)),
    [tiposCafe],
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
  const balanceUnavailable =
    !canViewExistencias || existenciasLoading || Boolean(existenciasError);

  const values = useMemo<MovimientoFormValues>(
    () => ({
      tipoMovimiento,
      presentacionCafeId,
      tipoCafeId,
      cantidad,
      referencia,
      observacion,
    }),
    [cantidad, observacion, presentacionCafeId, referencia, tipoCafeId, tipoMovimiento],
  );
  const validation = useMemo(
    () =>
      validateMovimientoInput(values, {
        presentaciones,
        tiposCafe,
        existencias,
        loteEstado: lote.estado,
      }),
    [existencias, lote.estado, presentaciones, tiposCafe, values],
  );
  const errors: MovimientoFieldErrors = submitted ? validation.errors : {};
  const lotBlocked = lote.estado === 3;

  useEffect(() => {
    if (!open) return;

    const safeInitialTipo =
      !canViewExistencias && movementRequiresBalance(initialTipo) ? 1 : initialTipo;

    setTipoMovimiento(safeInitialTipo);
    setPresentacionCafeId('');
    setTipoCafeId('');
    setCantidad('');
    setReferencia('');
    setObservacion('');
    setSubmitted(false);
    setSaving(false);
    setDialogError(null);
  }, [canViewExistencias, initialTipo, open]);

  function closeDialog() {
    if (saving) return;
    onClose();
  }

  function catalogErrorText(error?: IngresoLineaFieldError): string | undefined {
    if (!error) return undefined;

    switch (error) {
      case 'presentationRequired':
        return ti('presentationRequired');
      case 'coffeeTypeRequired':
        return ti('coffeeTypeRequired');
      case 'catalogUnavailable':
        return ti('catalogUnavailable');
      default:
        return undefined;
    }
  }

  function quantityErrorText(error?: IngresoLineaFieldError | 'insufficient'): string | undefined {
    if (!error) return undefined;

    switch (error) {
      case 'qtyRequired':
        return ti('quantityRequired');
      case 'qtyTooLarge':
        return ti('quantityTooLarge');
      case 'insufficient':
        return ti('insufficientBalance');
      default:
        return ti('quantityInvalid');
    }
  }

  function observationErrorText(
    error?: MovimientoFieldErrors['observacion'],
  ): string | undefined {
    if (!error) return undefined;

    return error === 'required'
      ? ti('adjustmentObservationRequired')
      : ti('observationTooLong', { max: OBSERVACION_MAX_LENGTH });
  }

  async function submit() {
    if (saving || lotBlocked) return;

    setSubmitted(true);
    setDialogError(null);

    const current = validateMovimientoInput(values, {
      presentaciones,
      tiposCafe,
      existencias,
      loteEstado: lote.estado,
    });

    if (Object.keys(current.errors).length > 0) {
      setDialogError(ti('validationErrorSummary'));
      return;
    }

    if (!current.presentacion || !current.tipoCafe || current.cantidadPaquetes === null) {
      setDialogError(ti('validationErrorSummary'));
      return;
    }

    if (current.requiresBalance && !canViewExistencias) {
      setDialogError(ti('noBalancePermission'));
      return;
    }

    if (current.requiresBalance && balanceUnavailable) {
      setDialogError(
        existenciasError ?? ti('balanceUnavailable'),
      );
      return;
    }

    setSaving(true);

    const conflictMessage = ti('conflictError');
    const forbiddenMessage = ti('forbiddenError');
    const notFoundMessage = ti('notFoundError');

    try {
      const saved = await apiFetch<MovimientoCafe>(
        `/api/v1/inventario/lotes/${lote.loteCafeId}/movimientos`,
        {
          method: 'POST',
          body: JSON.stringify(
            buildMovimientoPayload(
              values,
              current.presentacion.presentacionCafeId,
              current.tipoCafe.tipoCafeId,
              current.cantidadPaquetes,
            ),
          ),
        },
      );

      onSaved(saved);
      onClose();
    } catch (error) {
      setDialogError(
        resolveInventoryError(error, ti('errorSavingMovement'), {
          conflict: conflictMessage,
          forbidden: forbiddenMessage,
          notFound: notFoundMessage,
        }),
      );
    } finally {
      setSaving(false);
    }
  }

  const submitDisabled =
    saving ||
    lotBlocked ||
    !catalogsReady ||
    !canLoadCatalogos ||
    (validation.requiresBalance && balanceUnavailable);

  return (
    <Dialog open={open} onClose={closeDialog} fullWidth maxWidth="sm">
      <DialogTitle>
        {ti('movementDialogTitle', { code: lote.codigoLote })}
      </DialogTitle>
      <DialogContent dividers>
        <Stack spacing={2} sx={{ pt: 1 }}>
          {dialogError ? <Alert severity="error">{dialogError}</Alert> : null}

          {lotBlocked ? (
            <Alert severity="error">{ti('lotBlockedWarning')}</Alert>
          ) : null}

          {!canViewExistencias ? (
            <Alert severity="info">{ti('noBalancePermission')}</Alert>
          ) : existenciasLoading ? (
            <Alert severity="info">{ti('balanceLoading')}</Alert>
          ) : existenciasError ? (
            <Alert severity="error">{existenciasError}</Alert>
          ) : null}

          <FormControl fullWidth>
            <InputLabel id="movimiento-tipo-label">{tf('movementType')}</InputLabel>
            <Select
              labelId="movimiento-tipo-label"
              label={tf('movementType')}
              value={String(tipoMovimiento)}
              onChange={(event: SelectChangeEvent) => {
                setTipoMovimiento(Number(event.target.value) as TipoMovimientoLoteCafe);
              }}
            >
              {TIPOS_MOVIMIENTO.map((tipo) => (
                <MenuItem
                  key={tipo}
                  value={String(tipo)}
                  disabled={balanceUnavailable && movementRequiresBalance(tipo)}
                >
                  {ti(tipoMovimientoKey(tipo))}
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          {!canLoadCatalogos ? (
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
            <>
              {hasInactiveCatalogs ? (
                <Alert severity="warning">{ti('inactiveCatalogsWarning')}</Alert>
              ) : null}

              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
                <FormControl
                  fullWidth
                  error={Boolean(errors.presentacionCafeId)}
                >
                  <InputLabel id="movimiento-presentacion-label">
                    {tf('presentation')}
                  </InputLabel>
                  <Select
                    labelId="movimiento-presentacion-label"
                    label={tf('presentation')}
                    value={presentacionCafeId}
                    onChange={(event: SelectChangeEvent) =>
                      setPresentacionCafeId(event.target.value)
                    }
                  >
                    {activePresentaciones.map((presentacion) => (
                      <MenuItem
                        key={presentacion.presentacionCafeId}
                        value={String(presentacion.presentacionCafeId)}
                      >
                        {presentacion.nombre} (
                        {formatGrams(locale, presentacion.pesoUnitarioGramos)})
                      </MenuItem>
                    ))}
                  </Select>
                  {errors.presentacionCafeId ? (
                    <FormHelperText>
                      {catalogErrorText(errors.presentacionCafeId)}
                    </FormHelperText>
                  ) : null}
                </FormControl>

                <FormControl fullWidth error={Boolean(errors.tipoCafeId)}>
                  <InputLabel id="movimiento-tipo-cafe-label">
                    {tf('coffeeType')}
                  </InputLabel>
                  <Select
                    labelId="movimiento-tipo-cafe-label"
                    label={tf('coffeeType')}
                    value={tipoCafeId}
                    onChange={(event: SelectChangeEvent) =>
                      setTipoCafeId(event.target.value)
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
                  {errors.tipoCafeId ? (
                    <FormHelperText>
                      {catalogErrorText(errors.tipoCafeId)}
                    </FormHelperText>
                  ) : null}
                </FormControl>
              </Stack>

              <TextField
                type="number"
                label={tf('quantity')}
                value={cantidad}
                onChange={(event) => setCantidad(event.target.value)}
                error={Boolean(errors.cantidad)}
                helperText={quantityErrorText(errors.cantidad)}
                fullWidth
                slotProps={{ htmlInput: { min: 1, step: 1 } }}
              />

              {canViewExistencias &&
              validation.presentacion &&
              validation.tipoCafe ? (
                <Alert
                  severity={errors.cantidad === 'insufficient' ? 'error' : 'info'}
                  icon={false}
                >
                  <Typography variant="body2">
                    {ti('currentBalance', {
                      count: formatNumber(locale, validation.saldoActual),
                    })}
                  </Typography>
                  {validation.requiresBalance ? (
                    <Typography variant="caption">{ti('balanceRequiredNote')}</Typography>
                  ) : null}
                </Alert>
              ) : validation.requiresBalance ? (
                <Alert severity="info">{ti('selectComboForBalance')}</Alert>
              ) : null}

              {validation.pesoNetoKg !== null ? (
                <Alert severity="info" icon={false}>
                  {ti('estimatedNetWeight', {
                    weight: formatKg(locale, validation.pesoNetoKg),
                  })}
                </Alert>
              ) : null}
            </>
          )}

          <TextField
            label={tf('reference')}
            value={referencia}
            onChange={(event) => setReferencia(event.target.value)}
            error={Boolean(errors.referencia)}
            helperText={
              errors.referencia
                ? ti('referenceTooLong', { max: REFERENCIA_MAX_LENGTH })
                : `${referencia.length}/${REFERENCIA_MAX_LENGTH}`
            }
            fullWidth
            slotProps={{ htmlInput: { maxLength: REFERENCIA_MAX_LENGTH } }}
          />

          <TextField
            label={tf('observation')}
            value={observacion}
            onChange={(event) => setObservacion(event.target.value)}
            required={validation.requiresObservation}
            error={Boolean(errors.observacion)}
            helperText={
              observationErrorText(errors.observacion) ??
              `${observacion.length}/${OBSERVACION_MAX_LENGTH}`
            }
            fullWidth
            multiline
            minRows={2}
            slotProps={{ htmlInput: { maxLength: OBSERVACION_MAX_LENGTH } }}
          />

          {validation.requiresObservation ? (
            <Box>
              <Typography variant="caption" color="text.secondary">
                {ti('adjustmentObservationHelp')}
              </Typography>
            </Box>
          ) : null}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={closeDialog} disabled={saving}>
          {tc('cancel')}
        </Button>
        <Button onClick={() => void submit()} variant="contained" disabled={submitDisabled}>
          {saving ? ti('savingMovement') : ti('registerMovementAction')}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
